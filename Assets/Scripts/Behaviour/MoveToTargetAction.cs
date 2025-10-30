using System;
using Unity.Behavior;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move To Target", story: "Move to Target", category: "Action", id: "d87a71806fac973e6218bca78b6e2078")]
public partial class MoveToTargetAction : Action
{
    Enemy enemy;
    NavMeshAgent navAgent;
    protected override Status OnStart()
    {
        if (enemy == null && GameObject != null)
        {
            enemy = GameObject.GetComponent<Enemy>();
            navAgent = GameObject.GetComponent<NavMeshAgent>();
        }
        if (enemy == null) 
        {
            return Status.Failure;
        }

        enemy.BehaviourTree.GetVariable<float>("AttackRange", out var distance);
        navAgent.stoppingDistance = distance.Value * 0.9f;

        if (enemy.Senses.CurrentTarget != null) {
            enemy.Motor.MoveTo(enemy.Senses.CurrentTarget.transform.position);
        }

        return Status.Running;


        }


    protected override Status OnUpdate()
    {
        if (enemy.Senses.CurrentTarget == null)
        {
            enemy.Motor.Stop();
            return Status.Failure;
        }

        enemy.Motor.MoveTo(enemy.Senses.CurrentTarget.transform.position);

        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            enemy.Motor.Stop();
            return Status.Success;
        }

        // 아직 이동 중
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (enemy != null && enemy.Motor != null)
        {
            enemy.Motor.Stop();
        }
    }
}

