using UnityEngine;


public enum AttackType {Normal,Heavy }

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    public AttackType type;

    public float damage;
    public float knockbackPower;
    public float knockbackDuration;

    [Header("Attack Cooldown")]
    public float minAttackCooldown = 1.0f; // 공격 후 최소 대기 시간
    public float maxAttackCooldown = 2.0f; // 공격 후 최대 대기 시간
}
