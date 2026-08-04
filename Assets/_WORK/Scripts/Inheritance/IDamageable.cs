using UnityEngine;

public interface IDamageable
{
    /// <summary>아군 공격을 걸러내기 위한 팀 표식</summary>
    Faction TargetFaction { get; }

    bool IsDead { get; }

    /// <summary>
    /// 모든 공격을 계산하는 피해 함수
    /// 반환된 DamageResult를 통해 공격 수락 여부와 방어 결과를 확인
    /// </summary>
    DamageResult ReceiveDamage(in DamageRequest request);

    /// <summary>
    /// 체간 피해를 입히는 함수
    /// </summary>
    void ReceivePostureDamage(float postureDamage);
}
