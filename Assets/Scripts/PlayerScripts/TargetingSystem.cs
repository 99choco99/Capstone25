
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask ObstacleLayer;

    [SerializeField] private float cameraHalfFov;
    [SerializeField] private float detectionRange = 5f;
    List<IDamageable> validTargets = new List<IDamageable>();

    [Header("타겟 전환 쿨다운")]
    [SerializeField] private float targetSwitchCooldown = 2f; // 2초 쿨다운
    private float lastSwitchTime = 0f; // 마지막으로 타겟을 바꾼 시간

    public event Action<IDamageable> OnChangedTarget;
    public event Action OnTargetDeselected;

    public IDamageable CurrentTarget { get; private set; }
    public Collider targetCollider;
    private IDamageable nearestTarget;
    private IDamageable LeftTarget;
    private IDamageable RightTarget;
    private Player player;


    private void Awake()
    {
        player = GetComponent<Player>();

        cameraHalfFov = player.MainCamera.fieldOfView;
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
                nearestTarget = validTargets[0];
                SetTarget(nearestTarget);
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
    }

    private void HandleTargetSwitching()
    {
        if (Time.time < targetSwitchCooldown + lastSwitchTime)
        {
            return;
        }

        float lookInputX = player.InputHandler.LookInput.x;

        if (Mathf.Abs(lookInputX) > 0.5f)
        {
            validTargets = GetAllValidTargets();
            UpdateLeftRightTargets(validTargets);

            if (lookInputX > 0.8f && RightTarget != null)
            {
                SetTarget(RightTarget);
            }
            else if (lookInputX < -0.8f && LeftTarget != null)
            {
                SetTarget(LeftTarget);
            }
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

                Vector3 directionToTarget = collider.transform.position - player.MainCamera.transform.position;

                if (Vector3.Angle(player.MainCamera.transform.forward, directionToTarget) > cameraHalfFov) continue;


                if (Physics.Linecast(player.MainCamera.transform.position, collider.bounds.center, ObstacleLayer)) continue;

                validTargets.Add(target);
            }
        }
        return validTargets;
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

            if(angle > cameraHalfFov) { return; }
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
            targetCollider = CurrentTarget.gameObject.GetComponent<Collider>();
            OnChangedTarget?.Invoke(target);
            lastSwitchTime = Time.time;
        }
        else
        {
            DeselectTarget();
        }
    }

    void DeselectTarget()
    {
        CurrentTarget = null;
        nearestTarget = null;
        targetCollider = null;
        LeftTarget = null;
        RightTarget = null;
        player.isLockOn = false;
        OnTargetDeselected?.Invoke();
    }



    public bool IsCurrentTargetExecutable()
    {
        if (CurrentTarget == null || CurrentTarget.dead) return false;

        float distance = Vector3.Distance(transform.position, CurrentTarget.transform.position);
        if(distance > 2f) { return false; }

        //적의 방어가 무너졌을 때
        if (CurrentTarget.gameObject.TryGetComponent<EnemyStats>(out var enemyStats))
        {
            if (enemyStats.IsPostureBroken) return true;
        }

        //적이 발견하지 못했을 때
        if(CurrentTarget.gameObject.TryGetComponent<EnemySense>(out var enemySense))
        {
            if (!enemySense.IsTargetDetected)
            {
                return true;
            }
        }

        return false;
    }



}
