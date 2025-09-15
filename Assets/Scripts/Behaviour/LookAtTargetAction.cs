using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "LookAtTarget", story: "LookAtTarget", category: "Action", id: "08b1ecd2151c5fb4a4124e30b69e0e44")]
public partial class LookAtTargetAction : Action
{

    private EnemyMotor motor;
    private EnemySense senses;
    private Transform selfTransform;

    public float arrivalAngle = 5.0f;

    protected override Status OnStart()
    {
        motor = GameObject.GetComponent<Enemy>()?.Motor;
        senses = GameObject.GetComponent<Enemy>()?.Senses;
        selfTransform = GameObject.transform;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (motor == null || senses == null || senses.Target == null)
        {
            return Status.Failure;
        }

        motor.LookAtTarget(senses.Target.position);

        Vector3 directionToTarget = (senses.Target.position - selfTransform.position).normalized;
        directionToTarget.y = 0;

        float angle = Vector3.Angle(selfTransform.forward, directionToTarget);

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

