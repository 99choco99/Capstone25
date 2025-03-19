using UnityEngine;

public interface IDamageable
{
    public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal); // 입은 피해량, 공격당한 위치, 공격당한 표면의 방향
}
