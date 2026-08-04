using UnityEngine;

public enum DefenseType
{
    None,           // 쳐맞음
    FailedGuard,    // 가드 유지 중 (관통 데미지)
    NormalGuard,    // 일반 가드 성공 (데미지 0)
    PerfectParry    // 완벽한 패링 
}

public struct DamageEvent
{
    public GameObject attacker;
    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public AttackData attackData;

    public float currentDamage;
    public float currentPostureDamage;
    public float currentKnockbackForce;

    public DefenseType defenseResult;

    public bool isCancelled;

    public DamageEvent(GameObject attacker, AttackData attackData, Vector3 hitPoint, Vector3 hitDirection)
    {
        this.attacker = attacker;
        this.attackData = attackData;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;

        this.currentDamage = attackData.damage;
        this.currentPostureDamage = attackData.postureDamage;
        this.currentKnockbackForce = attackData.knockbackPower;

        this.defenseResult = DefenseType.None;
        this.isCancelled = false;
    }
}