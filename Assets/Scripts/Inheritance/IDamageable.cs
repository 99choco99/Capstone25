using UnityEngine;

public interface IDamageable
{
    public void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitNormal); // 입은 피해량, 공격당한 위치, 공격당한 표면의 방향
}
