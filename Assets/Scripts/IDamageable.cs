using UnityEngine;

public interface IDamageable
{
    public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal); // ���� ���ط�, ���ݴ��� ��ġ, ���ݴ��� ǥ���� ����
}
