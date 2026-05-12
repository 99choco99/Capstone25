using UnityEngine;


public enum AttackType {Normal,Heavy }

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    public AttackType type;

    [Header("데미지 스탯")]
    public float damage;
    public float postureDamage;
    public float knockbackPower;
    public float knockbackDuration;

    [Header("유틸리티")]
    [Tooltip("발동되기 위한 최소 거리")]
    public float minDistance = 0f;
    [Tooltip("발동되기 위한 최대 거리")]
    public float maxDistance = 3f;
    [Range(0f, 100f), Tooltip("발동 확률 가중치 (높을수록 우선순위 상승)")]
    public float weight = 50f;

    [Header("Attack Cooldown")]
    public float minAttackCooldown = 1.0f; // 공격 후 최소 대기 시간
    public float maxAttackCooldown = 2.0f; // 공격 후 최대 대기 시간
}
