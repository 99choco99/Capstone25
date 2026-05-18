using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private Player player;
    private Transform camTransform;
    private Collider[] hitColliders = new Collider[20];
    List<ITargetable> validTargets = new List<ITargetable>();

    [Header("유효 타겟 레이어 설정")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("타겟 범위")]
    [SerializeField] private float cameraHalfFov;
    [SerializeField] private float detectionRange = 5f;

    [Header("타겟 전환 설정")]
    [SerializeField] private float targetSwitchCooldown = 0.25f; // 2초 쿨다운
    [SerializeField] private float searchAngle = 5f;
    private float lastSwitchTime = 0f;                           // 마지막으로 타겟을 바꾼 시간

    [Header("타겟 우선순위 가중치 설정")]
    [SerializeField] private float distanceWeight = 1.0f; // 거리에 대한 가중치
    [SerializeField] private float angleWeight = 0.2f;    // 화면 중앙에 가까울수록 유리함

    public event Action<ITargetable> OnChangedTarget;
    public event Action OnTargetDeselected;

    public ITargetable CurrentTarget { get; private set; }

    private void Awake()
    {
        camTransform = UnityEngine.Camera.main.transform;
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
            ValidateCurrentTarget();
            HandleTargetUpdates();
        }
    }


    //타겟 설정
    private void ToggleTarget()
    {
        if (CurrentTarget != null)
        {
            DeselectTarget();
        }
        else
        {
            ITargetable target = FindBestTarget(0f);
            if(target != null) { SetTarget(target); }
        }
    }

    //너무 멀어졌을 때 타겟팅 취소
    private void ValidateCurrentTarget()
    {
        if (CurrentTarget.IsTargetableDead ||
            Vector3.Distance(transform.position, CurrentTarget.TargetTransform.position) > detectionRange)
        {
            DeselectTarget();
            return;
        }
    }


    private void HandleTargetUpdates()
    {
        if (Time.time < targetSwitchCooldown + lastSwitchTime) { return; }

        float lookInputX = player.InputHandler.LookInput.x;

        if (Mathf.Abs(lookInputX) > 0.5f)
        {
            float searchDirection = Mathf.Sign(lookInputX);
            ITargetable nextTarget = FindBestTarget(searchDirection);
            if(nextTarget != null) {SetTarget(nextTarget); }
        }
    }

    private ITargetable FindBestTarget(float searchDirection)
    {
        GetAllValidTargets();

        ITargetable bestTarget = null;
        float bestScore = Mathf.Infinity;

        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;

        foreach (var target in validTargets)
        {
            if (target == CurrentTarget) continue;

            Vector3 directionToTarget = target.TargetTransform.position - camTransform.position;
            directionToTarget.y = 0;

            float distance = directionToTarget.magnitude;
            float angle = Vector3.SignedAngle(camForward, directionToTarget, Vector3.up);

            if (searchDirection > 0 && angle < searchAngle) continue;
            else if (searchDirection < 0 && angle > searchAngle) continue;

            float score = distance * distanceWeight + Mathf.Abs(angle) * angleWeight;

            if (score < bestScore)
            {
                bestScore = score; 
                bestTarget = target;
            }
        }

        return bestTarget;
    }


    private void GetAllValidTargets()
    {
        validTargets.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, hitColliders, targetLayer);

        for(int i = 0; i < hitCount; i++)
        {
            if (hitColliders[i].TryGetComponent<ITargetable>(out var target))
            {
                if (target.IsTargetableDead) continue;

                Vector3 directionToTarget = target.TargetTransform.position - camTransform.position;

                if (Vector3.Angle(camTransform.forward, directionToTarget) > cameraHalfFov) continue;
                if (Physics.Linecast(camTransform.position, target.TargetTransform.position, obstacleLayer)) continue;

                validTargets.Add(target);
            }
        }
    }

    void SetTarget(ITargetable target)
    {
        CurrentTarget = target;
        player.IsLockOn = true;
        OnChangedTarget?.Invoke(target);
        lastSwitchTime = Time.time;
    }

    public void DeselectTarget()
    {
        CurrentTarget = null;
        player.IsLockOn = false;
        OnTargetDeselected?.Invoke();
    }



    public bool IsCurrentTargetExecutable()
    {
        if (CurrentTarget == null || CurrentTarget.IsTargetableDead) return false;

        if (CurrentTarget is Enemy enemy)
        {
            //return enemy.IsExecutable;
        }

        return false;
    }
}
