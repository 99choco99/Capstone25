using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TargetingSystem : MonoBehaviour
{
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask targetLayer;

    public event Action<IDamageable> OnChangedTarget;
    public event Action OnTargetDeselected;

    public IDamageable CurrentTarget { get; private set; }

    private List<IDamageable> targetInRange = new List<IDamageable>();
    private int targetIndex = -1;
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
            if(targetIndex >= targetInRange.Count) { SetTarget(null); return; }
            SetTarget(targetInRange[targetIndex]);
        }
        else
        {
            FindAndSelectTargetInRange();
        }
    }


    //타겟을 찾고 선택하기
    void FindAndSelectTargetInRange()
    {
        var colliders = Physics.OverlapSphere(transform.position, detectionRange,targetLayer);

        foreach (var collider in colliders)
        {
            targetInRange.Add(collider.GetComponent<IDamageable>());
        }
        if (targetInRange.Count > 0)
        {
            targetIndex = 0;
            SetTarget(targetInRange[0]);
        }
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
            OnChangedTarget?.Invoke(target);
        }
        else
        {
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
