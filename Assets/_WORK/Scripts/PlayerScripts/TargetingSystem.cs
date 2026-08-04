using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
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
    [SerializeField] private float targetSwitchCooldown = 0.25f; // 0.25초 쿨다운
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
        if (CurrentTarget != null)
        {
            ValidateCurrentTarget();
        }
    }


    //타겟 설정
    public void ToggleTarget()
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

    //타겟 변경
    public void SwitchTargetUpdates(float searchDirection)
    {
        if (CurrentTarget == null) return;
        if (Time.time < targetSwitchCooldown + lastSwitchTime) { return; }

        if (Mathf.Abs(searchDirection) > 0.5f)
        {
            float dir = Mathf.Sign(searchDirection);
            ITargetable nextTarget = FindBestTarget(dir);
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

    //타겟으로 가능한 모든 대상 가져오기
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

    //타겟으로 설정
    void SetTarget(ITargetable target)
    {
        CurrentTarget = target;
        OnChangedTarget?.Invoke(target);
        lastSwitchTime = Time.time;
    }

    //타겟 설정 해제
    public void DeselectTarget()
    {
        CurrentTarget = null;
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
