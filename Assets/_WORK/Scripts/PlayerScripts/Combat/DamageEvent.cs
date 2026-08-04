using UnityEngine;

public struct DamageEvent
{
    public GameObject attacker;
    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public AttackData attackData;

    public float currentDamage;
    public float currentPostureDamage;
    public float currentKnockbackForce;

    public bool wasGuarded;
    public bool wasParried;
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

        this.wasGuarded = false;
        this.wasParried = false;
        this.isCancelled = false;
    }
}