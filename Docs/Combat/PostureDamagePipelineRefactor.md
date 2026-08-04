# 패링 반환 체간 파이프라인 분리

- 작업일: 2026-07-29
- 범위: `DamageRequest`, `LivingEntity`, Player/Enemy 상태 전환, 전투 SFX, `AttackData`
- 목표: 패링으로 공격자에게 돌아가는 체간 피해를 일반 피격 파이프라인과 분리한다.

## 1. 변경 이유

기존에는 수비자가 패링에 성공하면 공격자에게 `DeflectDamage`라는 두 번째
`DamageRequest`를 전송했다.

```text
적의 직접 공격
→ 플레이어.ReceiveDamage
→ Parry 확정
├─ 플레이어 체간 피해 적용
└─ 적.ReceiveDamage(DeflectPosture)
```

이 구조는 `ReceiveDamage` 안에 있던 체간 제한, 회복 지연, UI 이벤트와 붕괴
판정을 재사용한다는 장점이 있었다. 그러나 패링 반환 체간은 실제 무기 공격이
아닌데도 일반 피해처럼 처리됐기 때문에 다음 예외가 필요했다.

- `DamageCause.DeflectPosture`
- `DamageRequest.DeflectDamage`
- 두 번째 요청을 다시 방어하지 않게 만드는 `CanGuard = false`
- State에서 보조 요청의 피격 애니메이션을 무시하는 조건
- SFX와 카메라 충격에서 보조 요청을 무시하는 조건

체간 수치 하나를 적용하기 위해 가짜 공격을 만들고, 그 부작용을 여러
시스템에서 제거하는 구조였으므로 책임을 분리했다.

## 2. 변경 후 흐름

```text
적의 직접 공격
→ 플레이어.ReceiveDamage
→ Parry 확정
├─ 플레이어: ApplyPostureDamage(원본 체간 × 0.5, 붕괴 불가)
└─ 적: ReceivePostureDamage(원본 체간 × 1.0, 붕괴 가능)
```

`ReceiveDamage`와 `ReceivePostureDamage`는 서로 다른 공개 진입점이지만,
실제 체간 수치 변경은 `LivingEntity.ApplyPostureDamage` 하나가 담당한다.

```text
LivingEntity.ApplyPostureDamage
├─ CurrentPosture 누적 및 최대치 제한
├─ 체간 회복 지연 초기화
├─ OnPostureChanged 통지
├─ 완벽 패링의 붕괴 금지 적용
└─ 최초 체간 붕괴 판정
```

따라서 체간 계산은 중복하지 않으면서도 일반 공격의 체력 계산, 방어 판정,
피격 애니메이션과 전투 피드백을 두 번 실행하지 않는다.

## 3. 체간 규칙

### 직접 피격과 일반 가드

`DamageCalculator`가 계산한 체간 피해를 수비자에게 적용한다.
체간이 최대치에 도달하면 붕괴할 수 있다.

### 완벽 패링한 수비자

수비자는 `postureDamage × ParryPostureRatio`만큼 체간을 받는다.
현재 비율은 `0.5`다.

수치가 최대 체간에 도달해도 그 패링 자체로는 붕괴하지 않는다.
그다음 일반 가드 또는 피격에서 양수 체간 피해를 받으면 붕괴한다.

### 패링당한 공격자

공격자는 원본 요청의
`RequestedPayload.PostureDamage × DeflectPostureRatio`만큼 체간을 받는다.
현재 비율은 `1.0`이다.

여기서 `DamageCalculator.Calculate`가 패링용으로 줄인 수비자의 체간값을
사용하면 반환 체간까지 50%가 되므로, 반드시 원본 `RequestedPayload`를 사용한다.

## 4. 체간 붕괴 알림

직접 공격은 기존처럼 `DamageResult.PostureBroken`으로 상태 머신에 결과를
전달한다. 패링 반환 체간은 `DamageResult`를 만들지 않으므로
`LivingEntity.OnPostureBroken` 이벤트로 상태 전환을 알린다.

- Player: `PlayerStunState`로 전환
- Enemy: `EnemyGroggyState`로 전환

직접 공격으로 이미 해당 상태에 전환된 경우 이벤트 핸들러는 중복 전환하지
않는다. `LivingEntity`는 붕괴 여부를 기억하므로 `ResetPosture`로 복구되기
전까지 추가 피해가 들어와도 붕괴 이벤트를 반복하지 않는다.

## 5. 제거하거나 단순화한 항목

제거:

- `DamageCause`
- `DamageRequest.DeflectDamage`
- `DamageRequest.DeflectPostureDamage`
- `AttackData.deflectPostureDamage`
- State와 SFX의 `DeflectPosture` 예외 조건

유지:

- `DamageRequest.CanGuard`
- `DamageResult.PostureBroken`
- 패링에 성공한 수비자가 받는 `ParryPostureRatio`
- 패링당한 공격자가 받는 `DeflectPostureRatio`

`CanGuard`는 더 이상 보조 요청의 무한 반사를 막는 용도가 아니다.
일반 공격은 가드 가능하고 특수 공격은 가드 불가능하다는 직접 공격 규칙만
표현한다.

## 6. AttackData 변경

기존 AttackData 16개 중 15개는 `postureDamage`와
`deflectPostureDamage`가 동일했다. 유일한 예외인 `NormalAttack5`는 프로젝트
참조가 없고 애니메이션 이름도 비어 있는 미사용 에셋이었다.

현재 전투에서 두 값을 독립적으로 튜닝하지 않았으므로
`deflectPostureDamage`를 제거하고 `postureDamage` 하나를 기준값으로 사용한다.

나중에 공격마다 반환 체간을 다르게 설계해야 한다면 전역 비율을 무작정
늘리기보다, 실제 예외가 필요한 공격에만 별도 배율을 추가한다.

## 7. 의도적으로 달라진 경계 정책

- 회피 무적은 실제 무기 공격의 피격만 막는다. 패링 반환 체간은 무기 공격이
  아니므로 `ReceivePostureDamage`에서 회피 무적을 검사하지 않는다.
- `MaxPosture`가 0 이하인 대상은 체간 시스템을 사용하지 않는 것으로 보고
  체간 피해와 붕괴를 적용하지 않는다.

현재 상태 머신에서는 공격 중인 캐릭터가 동시에 회피 상태일 수 없고,
Player와 모든 EnemyData의 최대 체간은 100이므로 기존 플레이에는 차이가 없다.

## 8. 회귀 테스트

- [ ] 일반 피격이 체력과 체간을 한 번만 적용한다.
- [ ] 일반 가드는 체력 피해를 막고 체간 피해를 적용한다.
- [ ] 완벽 패링은 수비자에게 50% 체간을 주지만 붕괴시키지 않는다.
- [ ] 최대 체간에서 다음 일반 가드 또는 피격을 받으면 붕괴한다.
- [ ] 패링당한 공격자의 체간은 원본 공격 체간의 100%만큼 증가한다.
- [ ] 패링 반환 체간으로 적이 붕괴하면 `EnemyGroggyState`에 들어간다.
- [ ] 패링 반환 체간으로 플레이어가 붕괴하면 `PlayerStunState`에 들어간다.
- [ ] 패링 한 번에 SFX, VFX, 히트 스톱과 카메라 충격이 한 번만 발생한다.
- [ ] 특수 공격은 `CanGuard == false`이며 일반 가드와 패링을 통과한다.
- [ ] 체간 회복 후 다시 붕괴할 수 있다.

## 9. 검증 결과

`Assembly-CSharp.csproj` 빌드 결과:

- 컴파일 오류: 0
- 이번 변경과 무관한 기존 경고: 12

Unity Play Mode에서는 위 회귀 테스트를 실제 애니메이션과 함께 확인해야 한다.
