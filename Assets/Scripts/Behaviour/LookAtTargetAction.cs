using System;
using TMPro;
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
        if (enemy.Senses.CurrentTarget == null)
        {
            return Status.Failure;
        }
        Vector3 targetPosition = enemy.Senses.CurrentTarget.position;
        Vector3 lookAtPosition = new Vector3(targetPosition.x, enemy.transform.position.y, targetPosition.z);
        enemy.Motor.LookAtTarget(lookAtPosition);

        Vector3 directionToTarget = (lookAtPosition - enemy.transform.position).normalized;

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

