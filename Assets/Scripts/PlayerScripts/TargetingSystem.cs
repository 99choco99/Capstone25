
using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] public LayerMask ObstacleLayer;
    [SerializeField] private Transform raycastOrigin; // 플레이어 시점 (카메라 또는 머리 위치)
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float maximumViewAngle = 50f;
    List<IDamageable> validTargets = new List<IDamageable>();

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
            ToggleTarget();
        }

        if (CurrentTarget != null)
        {
            HandleTargetUpdates();
            HandleTargetSwitching();
        }
    }


    //현재타겟을 다음 타겟으로 설정
    private void ToggleTarget()
    {
        if (CurrentTarget != null)
        {
            DeselectTarget();
        }
        else
        {
            validTargets = GetAllValidTargets();
            if (validTargets.Count > 0)
            {
                IDamageable nearest = FindNearestTarget(validTargets);
                SetTarget(nearest);
            }
        }
    }

    private void HandleTargetUpdates()
    {
        if (CurrentTarget.dead || Vector3.Distance(transform.position, CurrentTarget.transform.position) > detectionRange)
        {
            DeselectTarget();
            return;
        }

        validTargets = GetAllValidTargets();
        UpdateLeftRightTargets(validTargets);
    }

    private void HandleTargetSwitching()
    {
        float lookInputX = player.InputHandler.LookInput.x;

        if (lookInputX > 0.5f && RightTarget != null)
        {
            SetTarget(RightTarget);
        }
        else if (lookInputX < -0.5f && LeftTarget != null)
        {
            SetTarget(LeftTarget);
        }
    }


    private List<IDamageable> GetAllValidTargets()
    {
        validTargets.Clear();


        var colliders = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<IDamageable>(out var target))
            {
                if (target.dead) continue;

                Vector3 directionToTarget = collider.transform.position - raycastOrigin.position;
                if (Vector3.Angle(player.MainCamera.transform.forward, directionToTarget) > maximumViewAngle) continue;

                if (Physics.Linecast(raycastOrigin.position, collider.bounds.center, ObstacleLayer)) continue;

                validTargets.Add(target);
            }
        }
        return validTargets;
    }


    private IDamageable FindNearestTarget(List<IDamageable> targets)
    {
        IDamageable nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var target in targets)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = target;
            }
        }
        return nearest;
    }


    private void UpdateLeftRightTargets(List<IDamageable> targets)
    {
        LeftTarget = null;
        RightTarget = null;
        float closestRightAngle = 180f;
        float closestLeftAngle = -180f;

        Vector3 cameraForward = player.MainCamera.transform.forward;

        foreach (var target in targets)
        {
            if (target == CurrentTarget) continue;

            Vector3 directionToTarget = target.transform.position - transform.position;
            float angle = Vector3.SignedAngle(cameraForward, directionToTarget, Vector3.up);

            if (angle > 0 && angle < closestRightAngle) // 오른쪽에 있는 타겟들 중 가장 중앙에 가까운 타겟
            {
                closestRightAngle = angle;
                RightTarget = target;
            }
            else if (angle < 0 && angle > closestLeftAngle) // 왼쪽에 있는 타겟들 중 가장 중앙에 가까운 타겟
            {
                closestLeftAngle = angle;
                LeftTarget = target;
            }
        }
    }

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
            DeselectTarget();
        }
    }

    void DeselectTarget()
    {
        CurrentTarget = null;
        LeftTarget = null;
        RightTarget = null;
        player.isLockOn = false;
        OnTargetDeselected?.Invoke();
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
