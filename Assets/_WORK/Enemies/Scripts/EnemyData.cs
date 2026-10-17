using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int id;

    [Header("생존 수치")]
    public float hp;
    public float posture;

    public int exp;

    [Header("보스 마지막 인살")]
    [Tooltip("마지막 목숨의 붕괴 시 자동 인살과 결정타 입력 연출을 사용합니다.")]
    public bool automaticFinalDeathblow;

    [Header("플레이어 레벨에 따른 성장")]
    public LevelScaling levelScaling = new();

    [Header("거리 판단")]
    [Tooltip("교전 거리")]
    [Min(0.1f)] public float combatRange = 3f;
    [Tooltip("이 배수만큼 멀어지면 추적으로 전환. 전투 경계선에서 떨림 방지")]
    [Min(1f)] public float combatExitRangeMultiplier = 1.2f;
    [InspectorName("다시 접근하기 전 거리 여유"), Min(0f)] public float distanceTolerance = 0.2f;
    [Tooltip("공격을 고른 뒤 접근할 때 사용할 최소 중심 거리. 공격 전진·접촉 간격과 몸 충돌 반경을 제한하지 않습니다.")]
    [InspectorName("최소 접근 거리"), Min(0f)] public float minimumApproachDistance = 1.35f;
    [InspectorName("거리 조정 정체 제한 시간"), Min(0f)] public float approachRetryDelay = 1.2f;
    [Tooltip("패턴 예약부터 공격 시작까지의 전체 제한 시간. 거리 조정에 진전이 있어도 초기화하지 않습니다.")]
    [InspectorName("공격 준비 전체 제한 시간"), Min(0.1f)] public float attackPreparationTimeout = 2.5f;
    [InspectorName("준비 실패 패턴 재선택 대기"), Min(0.1f)] public float failedPreparationRetryDelay = 2f;
    [Tooltip("현재 거리보다 더 먼 시작 간격이 필요한 공격을 예약할 때 허용할 최대 거리 차이입니다.")]
    [InspectorName("공격 준비 최대 후퇴 간격"), Min(0f)] public float maxPreparationRetreatDistance = 1.5f;

    [Tooltip("이 간격 안에서 교전 중이면 공격으로 압박합니다. 후퇴/관찰은 상대가 실제로 다가올 때 중단합니다.")]
    [InspectorName("근접 압박 거리"), Min(0.1f)] public float closePressureDistance = 2.3f;
    [Tooltip("이 간격부터 새 공격을 고를 때 돌진, 접근 공격을 우선합니다. 재사용 대기와 실제 시작 사거리는 유지합니다.")]
    [InspectorName("원거리 진입 거리"), Min(0.1f)] public float distantAttackDistance = 3.6f;

    [Header("행동 대기")]
    [Min(0f)] public float minActionDelay = 0.5f;
    [Min(0f)] public float maxActionDelay = 1.2f;

    [Header("공방 결과별 행동")]
    public EnemyExchangeSettings exchange = new();

    [Tooltip("공방 상태별 역할 가중치와 접촉 반응 시간. 미지정이면 기존 개별 가중치를 사용합니다.")]
    public EnemyAttackPolicy attackPolicy;
    [Tooltip("정책 기준 거리를 현재 무기와 몸의 중심 간격으로 환산하는 배율입니다.")]
    [Min(0.1f)] public float policyDistanceScale = 1f;

    [Header("공격 패턴")]
    public List<EnemyAttackData> pattern = new();
    [InspectorName("같은 패턴 반복 선택 완화")] public bool avoidRepeatedPatterns = true;
    [InspectorName("직전 패턴 선택 가중치 배율"), Range(0.01f, 1f)] public float repeatedPatternWeight = 0.35f;
    [Tooltip("원본 판단을 모션으로 연결할 때 적용합니다. 다른 실행 가능한 공격이 있을 때만 같은 첫 모션의 연속 사용을 제한합니다. 연격 내부 동작은 별도로 세지 않습니다.")]
    [InspectorName("같은 모션 연속 실행 상한"), Min(1)] public int maximumConsecutivePatternUses = 2;
    [InspectorName("기억할 최근 패턴 수"), Min(0)] public int recentPatternMemory = 4;
    [Tooltip("최근 실제로 시작한 같은 특징의 패턴마다 가중치에 곱할 값입니다. 준비하다 취소한 공격은 포함하지 않습니다.")]
    [InspectorName("최근 패턴 반복 가중치"), Range(0.1f, 1f)] public float recentPatternWeight = 0.65f;
    [InspectorName("후퇴 동작")] public EnemyAttackData retreatAttack;
    [Tooltip("공격 준비에 필요한 간격과 NavMesh에서 실제 확보할 수 있는 후퇴 거리가 이 값 이상일 때만 도약합니다. 짧은 거리 조정은 뒤로 걷습니다.")]
    [InspectorName("최소 후퇴 도약 거리"), Min(0.1f)] public float minimumRetreatJumpDistance = 1.25f;

    [Header("방어")]
    [Tooltip("첫 패링 확률")]
    [Range(0f, 1f)] public float parryChance;
    [Tooltip("일반 가드 확률")]
    [Range(0f, 1f)] public float blockChance = 1f;
    [Tooltip("무방비 상태에서 플레이어의 공격 확정을 감지한 뒤 방어 판단에 필요한 시간입니다. 이미 올린 가드의 연타 대응에는 적용하지 않습니다.")]
    [InspectorName("첫 방어 반응 시간"), Min(0f)] public float defenseReactionTime = 0.14f;
    [Tooltip("연속으로 가드한 횟수마다 패링 확률에 더합니다. 가드 압박 기억 시간과 최종 방어 확률을 따릅니다.")]
    [InspectorName("연속 가드당 패링 확률 추가"), Range(0f, 1f)] public float guardPressureParryIncrement = 0.5f;
    [Tooltip("예상 타격 보다 빠르게 가드를 올리는 시간")]
    [Min(0f)] public float guardLeadTime = 0.11f;
    [Tooltip("공격을 막을 수 있는 전체 각도")]
    [Range(0f, 180f)] public float guardAngle = 90f;

    [Header("공격 후 빈틈과 피격 복귀")]
    [Tooltip("헛공격 또는 막힌 큰 공격·긴 연격의 마지막 회수 시작부터 상대에게 대응 기회를 주는 최소 시간입니다. 플레이어의 남은 회피·방어 회복과 첫 공격의 타격 구간까지 포함하며, 그동안 간격을 유지해 움직입니다.")]
    [UnityEngine.Serialization.FormerlySerializedAs("punishWindowDuration")]
    [InspectorName("회수 시작 후 최소 대응 시간 (초)"), Min(0f)] public float defenseRecoveryDuration = 0.4f;
    [Tooltip("정면의 일반 공격에 이 횟수만큼 피격 반응을 보인 뒤에는 추가 경직 갱신을 멈추고 가드를 올립니다. 피해는 그대로 받습니다.")]
    [InspectorName("연속 피격 반응 횟수"), Min(1)] public int maximumHitReactions = 2;
    [Tooltip("피격에서 복귀할 때 가드 동작을 먼저 보여주는 시간입니다. 이 시간이 지나야 일반 가드 판정이 켜집니다.")]
    [InspectorName("피격 후 가드 준비 (초)"), Min(0f)] public float hitGuardRecoveryDuration = 0.12f;

    [Header("위협 판정")]
    [Min(0.1f)] public float reactionRange = 2.5f;
    [Range(0f, 180f)] public float threatAngle = 180f;

    [Header("반격")]
    [Tooltip("패링 후 반격 확률")]
    [Range(0f, 1f)] public float counterChance = 0.45f;
    [Tooltip("패링 접촉 후 반격 전 최소 반응 시간")]
    [Min(0f)] public float counterDelay = 0.16f;
    [InspectorName("패링 후 회수할 체간 비율"), Range(0f, 1f)] public float counterRecoveryPostureRatio = 0.7f;
    [Tooltip("공격을 시작하지 못한 채 이 횟수만큼 패링하면 체간 회복보다 직접 반격을 우선합니다.")]
    [InspectorName("공격 전 최대 수동 패링 횟수"), Min(1)] public int maximumPassiveParries = 2;

    [Header("헛공격의 빈틈 대응")]
    [InspectorName("헛공격 대응 확률"), Range(0f, 1f)] public float whiffPunishChance = 0.9f;
    [Tooltip("플레이어의 헛공격 회수가 시작된 뒤 이 시간부터 판단합니다. 적 공격의 준비 동작은 유지합니다.")]
    [InspectorName("헛공격 대응 반응 시간"), Min(0f)] public float whiffReactionTime = 0.12f;
    [Tooltip("예상 첫 타격까지의 시간에 더하는 여유입니다. 플레이어의 남은 빈틈 안에 타격할 수 있는 공격만 고르며, 헛공격 대응 반응 시간과 별개입니다.")]
    [InspectorName("헛공격 대응 타격 여유 (초)"), Min(0f)] public float whiffStartupMargin = 0.04f;
    [InspectorName("헛공격 대응 재사용 대기"), Min(0f)] public float whiffPunishCooldown = 1.2f;

    [Header("패링 후 후속 패턴")]
    [Tooltip("빠른 반격 대신 정상 공격 준비를 거치는 후속 패턴을 고를 확률입니다. 후속 후보가 없으면 기존 반격을 사용합니다.")]
    [InspectorName("후속 패턴 선택 확률"), Range(0f, 1f)] public float counterFollowupChance = 0.35f;
    [InspectorName("플레이어 가드 시 후속 확률 추가"), Range(0f, 1f)] public float guardedCounterFollowupBonus = 0.12f;
    [InspectorName("연속 공격 압박 시 후속 확률 추가"), Range(0f, 1f)] public float pressureCounterFollowupBonus = 0.05f;
    [InspectorName("플레이어 공격 기억 시간"), Min(0.1f)] public float playerPressureMemoryDuration = 2.5f;
    [InspectorName("연속 공격 압박 기준 횟수"), Min(1)] public int playerPressureAttackCount = 2;

    [Header("플레이어 행동별 패턴 가중치")]
    [InspectorName("가드 상대 위협 가중치"), Min(0.1f)] public float guardedPerilousWeight = 1.8f;
    [InspectorName("가드 상대 엇박자 가중치"), Min(0.1f)] public float guardedDelayedWeight = 1.6f;
    [InspectorName("공격 압박 상대 후퇴 가중치"), Min(0.1f)] public float pressureRetreatWeight = 2f;
    [InspectorName("멀어진 상대 돌진 가중치"), Min(0.1f)] public float distantRushWeight = 1.7f;

    [Header("회전")]
    [Min(0f)] public float combatRotationSpeed = 15f;
    [Min(0f)] public float chaseAngularSpeed = 400f;

    [Header("이동 속도")]
    [Min(0f)] public float chaseSpeed = 3f;
    [Min(0f)] public float strafeSpeed = 0.5f;
    [Tooltip("상대를 보며 직선으로 간격을 벌릴 때의 속도. 0이면 strafe 속도를 사용합니다.")]
    [InspectorName("간격 확보 속도"), Min(0f)] public float spacingSpeed;
    public float EffectiveSpacingSpeed => spacingSpeed > 0f ? spacingSpeed : strafeSpeed;
    [Tooltip("돌진 및 후퇴 후 재진입의 거리 조정 속도. 0이면 기존 추격 속도를 사용합니다.")]
    [InspectorName("빠른 재진입 접근 속도"), Min(0f)] public float burstApproachSpeed;
    [InspectorName("빠른 재진입 가속도"), Min(0f)] public float burstApproachAcceleration = 24f;
    [Min(0f)] public float arrivalTolerance = 0.1f;

    [Header("이동 애니메이션 기준 속도")]
    [Tooltip("NavMesh 최고 속도와 독립된 걷기 모션 기준 속도. 실제 이동 속도로 걷기/달리기를 혼합합니다.")]
    [Min(0.01f)] public float locomotionWalkSpeed = 2.54f;
    [Min(0.02f)] public float locomotionRunSpeed = 3.75f;


    [Header("교전 위치 보정")]
    [Tooltip("공격 사거리 실측 자료가 없을 때 사용할 대치 중심 간격. 실측 자료가 있으면 양쪽 기본 공격의 위협권에서 계산합니다.")]
    [InspectorName("관찰 거리"), Min(0.1f)] public float observationDistance = 4.4f;
    [Tooltip("대치 거리에서 이 여유보다 가까워지면 물러납니다. 거리 경계의 작은 변화로 후퇴/관찰을 반복하지 않도록 합니다.")]
    [InspectorName("관찰 거리 여유"), Min(0f)] public float observationDistanceTolerance = 0.45f;
    [Tooltip("한 번의 횡이동 목표 거리. 몸통 폭보다 작으면 몸통 폭을 사용하며, 각도 상한과 이동 가능한 공간에 따라 실제 거리는 줄어듭니다.")]
    [InspectorName("횡이동 한 번 거리"), Min(0.1f)] public float strafeStepDistance = 1.2f;
    [Tooltip("한 번의 짧은 옆걸음에서 허용할 최대 각도")]
    [Range(5f, 60f)] public float strafeArcAngle = 24f;
    [Tooltip("관찰 최소 시간. 한 번의 횡이동 거리는 별도로 설정합니다.")]
    [Range(0f, 10f)] public float minStrafeTime = 1.5f;
    [Tooltip("관찰 최대 시간. 간격 확보 제한 시간은 필요한 거리/속도에 따라 더 길어질 수 있습니다.")]
    [Range(0f, 10f)] public float maxStrafeTime = 3.5f;
    [Tooltip("옆걸음을 마친 뒤 다음 옆걸음까지 상대를 주시하며 쉬는 시간")]
    [InspectorName("횡이동 사이 정지 시간"), Min(0f)] public float strafePauseTime = 0.25f;
    [Tooltip("상대가 실제로 이동했을 때 기존 목적지를 보정하는 최소 간격")]
    [Min(0.05f)] public float strafeRefreshTime = 0.25f;
    [Min(0f)] public float strafeSampleDistance = 1.5f;
    [Min(0f)] public float strafeRetryTime = 0.5f;

    [System.Serializable]
    public sealed class LevelScaling
    {
        public bool Enabled;
        [Min(2)] public int MaximumLevel = 29;
        [Min(0.01f)] public float InitialHealthMultiplier = 0.95f;
        [Min(0.01f)] public float InitialPostureMultiplier = 1f;
        [Min(0.01f)] public float InitialDamageMultiplier = 1f;
        [Min(0.01f)] public float MaximumHealthMultiplier = 1.1f;
        [Min(0.01f)] public float MaximumPostureMultiplier = 1.1f;
        [Min(0.01f)] public float MaximumDamageMultiplier = 1.1f;

        public float GetMultiplier(int playerLevel, float initial, float maximum)
        {
            return Enabled
                ? Mathf.Lerp(
                initial,
                maximum,
                Mathf.InverseLerp(1f, Mathf.Max(2, MaximumLevel), playerLevel))
                : 1f;
        }
    }

    private void OnValidate()
    {
        if (parryChance + blockChance >= 1f)
        {
            blockChance = 1f - parryChance;
        }
    }
}
