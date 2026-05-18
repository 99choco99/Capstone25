using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private float hitCheckRadius = 0.2f; // 검사할 반경
    [SerializeField] private Transform[] hitPoints;    // 검사 중심점

    private IWeaponOwner owner;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private bool isAttackActive = false;

    private Vector3[] previousPoints;

    private RaycastHit[] hitResults = new RaycastHit[32];
    private Collider[] overlapResults = new Collider[32];

    private void Awake()
    {
        owner = GetComponentInParent<IWeaponOwner>();
        if(hitPoints == null || hitPoints.Length == 0)
        {
            Debug.LogError("아직 무기에 hitPoint 설정이 되지 않았습니다.");
        }
        else
        {
            previousPoints = new Vector3[hitPoints.Length];
        }
    }
    private void Update()
    {
        if (owner == null || !isAttackActive)
        {
            return;
        }
        PerformHitCheck();
    }
    public void EnableWeaponCollider()
    {
        hitTargets.Clear();
        isAttackActive = true;

        for (int i = 0; i < hitPoints.Length; i++)
        {
            previousPoints[i] = hitPoints[i].position;
        }
    }

    public void DisableWeaponCollider()
    {
        hitTargets.Clear();
        isAttackActive = false;
    }
    public void PerformHitCheck()
    {
        for (int i = 0; i < hitPoints.Length; i++)
        {
            Vector3 currentPoint = hitPoints[i].position;
            Vector3 previousPoint = previousPoints[i];

            Vector3 direction = currentPoint - previousPoint;
            float distance = direction.magnitude;

            if(distance > 0.001f)
            {
                int hitCount = 
                    Physics.SphereCastNonAlloc
                    (previousPoint, hitCheckRadius, direction.normalized, hitResults, distance, targetLayerMask);
                for(int h =0 ; h < hitCount; h++) { ProcessHit(hitResults[h].collider);}
                
            }
            else
            {
                int hitCount = Physics.OverlapSphereNonAlloc(previousPoint, hitCheckRadius, overlapResults, targetLayerMask);
                for(int h= 0; h < hitCount; h++) { ProcessHit (overlapResults[h]); }
            }
        }
    }


    private void ProcessHit(Collider hitCollider)
    {
        if(hitCollider.TryGetComponent<IDamageable>(out var target))
        {
            if(owner.OwnerFaction != target.TargetFaction)
            {
                hitTargets.Add(target);
                owner.OnWeaponHit(target, hitCollider, this);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (hitPoints == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < hitPoints.Length; i++)
        {
            if (hitPoints[i] != null)
            {
                Gizmos.DrawWireSphere(hitPoints[i].position, hitCheckRadius);

                if (Application.isPlaying && isAttackActive && previousPoints != null && previousPoints.Length > i)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(previousPoints[i], hitPoints[i].position);
                    Gizmos.color = Color.red;
                }
            }
        }
    }
}
