using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public void SelectNextTarget()
    {
        if (CurrentTarget != null)
        {
            targetIndex = (targetIndex + 1) % targetInRange.Count;
            SetTarget(targetInRange[targetIndex]);
        }
        else
        {
            FindAndSelectTargetInRange();
        }
    }

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

    void DeselectTarget()
    {
        SetTarget(null);
    }

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

}
