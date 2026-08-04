using UnityEngine;

public class EnemyAttackData : AttackData
{
    public override bool CanGuard => base.CanGuard;

    [Header("유틸리티")]
    [Tooltip("발동되기 위한 최소 거리")]
    public float minDistance = 0f;
    [Tooltip("발동되기 위한 최대 거리")]
    public float maxDistance = 3f;
    [Range(0f, 100f), Tooltip("발동 확률 가중치 (높을수록 우선순위 상승)")]
    public float weight = 50f;

    [Header("AttackData Cooldown")]
    public float minAttackCooldown = 1.0f; // 공격 후 최소 대기 시간
    public float maxAttackCooldown = 2.0f; // 공격 후 최대 대기 시간
}
