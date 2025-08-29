using System.Collections;
using UnityEngine;

public class PlayerWeapon : Weapon
{
    [SerializeField] LayerMask layerMask;
    PlayerSetting player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerSetting>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((1 << other.gameObject.layer) == layerMask)
        {
            if (!other.TryGetComponent<Enemy>(out var target)) { Debug.Log("Enemy Component를 찾을 수 없음");  return; }
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDirection = (other.transform.position - player.transform.position).normalized;
            hitDirection.y = 0;
            target.OnDamage(player.currentAttack, player.currentAnimationIndex, hitDirection);
        }
    }


}
