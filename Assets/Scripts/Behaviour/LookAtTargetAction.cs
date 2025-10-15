using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "LookAtTarget", story: "LookAtTarget", category: "Action", id: "08b1ecd2151c5fb4a4124e30b69e0e44")]
public partial class LookAtTargetAction : Action
{
    private Enemy enemy;

    public float arrivalAngle = 5.0f;


    protected override Status OnStart()
    {
        if (enemy == null)
        {
            enemy = GameObject.GetComponent<Enemy>();
        }

        if (enemy == null || enemy.Motor == null || enemy.Senses == null)
        {
            Debug.LogError("LookAtTargetAction: Enemy 또는 필수 컴포넌트를 찾을 수 없습니다!");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (enemy.Senses.Target == null)
        {
            return Status.Failure;
        }

        enemy.Motor.LookAtTarget(enemy.Senses.Target.position);

        Vector3 directionToTarget = (enemy.Senses.Target.position - enemy.transform.position).normalized;
        directionToTarget.y = 0;

        float angle = Vector3.Angle(enemy.transform.forward, directionToTarget);

        if (angle <= arrivalAngle)
        {
            return Status.Success;
        }

        return Status.Running;
    }


    protected override void OnEnd()
    {
    }
}

