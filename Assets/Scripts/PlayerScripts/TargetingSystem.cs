using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] public LayerMask ObstacleLayer;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float maximumViewAngle = 50f;
    private List<IDamageable> TargetInRange = new List<IDamageable>();



    public event Action<IDamageable> OnChangedTarget;
    public event Action OnTargetDeselected;

    public IDamageable CurrentTarget { get; private set; }
    private IDamageable nearestTarget;
    private IDamageable LeftTarget;
    private IDamageable RightTarget;
    private Player player;


    private void Awake()
    {
        player = GetComponent<Player>();   
    }


    private void Update()
    {
        if (player.InputHandler.TargetInput)
        {
            player.InputHandler.UseTargetInput();
            SelectNextTarget();
        }

        if (CurrentTarget != null)
        {
            if(player.InputHandler.LookInput.x > 0.5f)
            {
                if (RightTarget != null) SetTarget(RightTarget);
            }
            else if(player.InputHandler.LookInput.x < -0.5f)
            {
                if (LeftTarget != null) SetTarget(LeftTarget);
            }


            if (CurrentTarget.dead || Vector3.Distance(transform.position, CurrentTarget.gameObject.transform.position) > detectionRange)
            {
                DeselectTarget();
            }
        }
    }


    //현재타겟을 다음 타겟으로 설정
    public void SelectNextTarget()
    {
        if (CurrentTarget != null)
        {
            SetTarget(null);
        }
        else
        {
            FindAndSelectTargetInRange();
            SetTarget(nearestTarget);
        }
    }


    //타겟을 찾고 선택하기
    void FindAndSelectTargetInRange()
    {
        TargetInRange.Clear();

        float shortestDistance = Mathf.Infinity;
        float shortestDistanceOfRightTarget = Mathf.Infinity;
        float shortestdistanceOfLeftTarget = -Mathf.Infinity;


        var colliders = Physics.OverlapSphere(transform.position, detectionRange,targetLayer);

        foreach (var collider in colliders)
        {
            var target = collider.GetComponent<IDamageable>();

            if(target != null)
            {
                Vector3 targetDirection = collider.transform.position - transform.position;
                float distanceFromTarget = Vector3.Distance(transform.position, collider.transform.position);
                float viewableAngle = Vector3.Angle(targetDirection, player.MainCamera.transform.forward);

                if (target.dead) { continue; }

                if(distanceFromTarget > detectionRange) { continue; }

                if(viewableAngle < maximumViewAngle)
                {
                    if(Physics.Linecast(targetTransform.position, target.transform.position, out var hit, ObstacleLayer))
                    {
                        continue;
                    }
                    else
                    {
                        TargetInRange.Add(target);
                    }
                }

            }
        }


        for (int i = 0; i < TargetInRange.Count; i++)
        {
            if (TargetInRange[i] != null)
            {
                float distanceFromTarget = Vector3.Distance(transform.position, TargetInRange[i].transform.position);
                Vector3 directionFromTarget = TargetInRange[i].transform.position - transform.position;

                if(distanceFromTarget < shortestDistance)
                {
                    shortestDistance = distanceFromTarget;
                    nearestTarget = TargetInRange[i];
                }

                Vector3 relativeEnemyPosition = transform.InverseTransformPoint(TargetInRange[i].transform.position);
                var distanceFromLeftTarget = relativeEnemyPosition.x;
                var distanceFromRightTarget = relativeEnemyPosition.x;

                if (CurrentTarget == TargetInRange[i])
                    continue;


                if(distanceFromLeftTarget <= 0f && distanceFromRightTarget > shortestdistanceOfLeftTarget)
                {
                    shortestdistanceOfLeftTarget = distanceFromLeftTarget;
                    LeftTarget = TargetInRange[i];
                }else if(distanceFromRightTarget >= 0f && distanceFromRightTarget < shortestDistanceOfRightTarget)
                {
                    shortestDistanceOfRightTarget = distanceFromRightTarget;
                    RightTarget = TargetInRange[i];
                }
            }
            else
            {
                ClearTargetInRange();
            }
        }


    }

    private void ClearTargetInRange()
    {
        nearestTarget = null;
        LeftTarget = null;
        RightTarget = null;
        TargetInRange.Clear();
    }


    //타겟 해제
    void DeselectTarget()
    {
        SetTarget(null);
    }

    //타겟 설정
    void SetTarget(IDamageable target)
    {
        CurrentTarget = target;
        if (target != null)
        {
            player.isLockOn = true;
            OnChangedTarget?.Invoke(target);
        }
        else
        {
            player.isLockOn = false;
            OnTargetDeselected?.Invoke();
        }
    }


    public bool IsCurrentTargetExecutable()
    {
        if (CurrentTarget == null || CurrentTarget.dead) return false;

        //적의 방어가 무너졌을 때
        if (CurrentTarget.gameObject.TryGetComponent<EnemyStats>(out var enemyStats))
        {
            if (enemyStats.IsPostureBroken) return true;
        }

        //적이 발견하지 못했을 때
        if(CurrentTarget.gameObject.TryGetComponent<EnemySense>(out var enemySense))
        {
            float angleToEnemyBack = Vector3.Angle(player.transform.forward, -CurrentTarget.transform.forward);
            if (!enemySense.IsTargetDetected && angleToEnemyBack < 45f) // 등 뒤 90도 범위
            {
                return true;
            }
        }

        return false;
    }
}
