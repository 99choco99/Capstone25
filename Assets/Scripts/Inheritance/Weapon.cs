using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private float hitCheckRadius = 0.5f; // 검사할 반경
    [SerializeField] private Transform hitCheckPoint;    // 검사 중심점

    private IWeaponOwner owner;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private bool isAttackActive = false;

    private void Awake()
    {
        owner = GetComponentInParent<IWeaponOwner>();
        if (hitCheckPoint == null) { hitCheckPoint = transform; } // 중심점 없으면 무기 자체 위치 사용
        if (owner == null)
        {
            Debug.LogError("이 무기의 주인(IWeaponOwner)을 찾을 수 없습니다", gameObject);
        }
    }
    private void Update()
    {
        if (owner == null)
        {
            isAttackActive = false;
            this.enabled = false;
            return;
        }
        PerformHitCheck();
    }
    public void PerformHitCheck()
    {
        if (!isAttackActive) return;

        Collider[] overlappedColliders = Physics.OverlapSphere(hitCheckPoint.position, hitCheckRadius, targetLayerMask);

        foreach (Collider col in overlappedColliders)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
            {
                if (!hitTargets.Contains(target))
                {
                    owner.OnWeaponHit(target, col, this); // 데미지 처리
                }
            }
        }
    }

    public void EnableWeaponCollider()
    {
        hitTargets.Clear();
        isAttackActive = true;
    }

    public void DisableWeaponCollider()
    {
        hitTargets.Clear();
        isAttackActive = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitCheckPoint.position, hitCheckRadius);
        }
    }
}
