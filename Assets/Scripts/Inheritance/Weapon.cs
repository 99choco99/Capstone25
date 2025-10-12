using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;

    private IWeaponOwner owner;
    private Collider weaponCollider;
    private List<IDamageable> hitTargets = new List<IDamageable>();

    private void Awake()
    {
        owner = GetComponentInParent<IWeaponOwner>();
        weaponCollider = GetComponent<Collider>();

        if (owner == null)
        {
            Debug.LogError("이 무기의 주인(IWeaponOwner)을 찾을 수 없습니다", gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((layerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (other.TryGetComponent<IDamageable>(out var target))
            {
                if (!hitTargets.Contains(target))
                {
                    hitTargets.Add(target);
                    owner.OnWeaponHit(target, other, this); // 데미지 처리
                }
            }
        }
    }

    public void EnableWeaponCollider()
    {
        hitTargets.Clear();
        weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        hitTargets.Clear();
        weaponCollider.enabled = false;
    }
}
