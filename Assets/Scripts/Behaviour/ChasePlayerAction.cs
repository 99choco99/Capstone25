using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase Player", story: "[Agent]가 [Target]을 향해 뛰어간다", category: "Action/Enemy", id: "chase_player")]
public partial class ChasePlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private EnemyMotor motor;

    protected override Status OnStart()
    {
        if (Agent.Value == null || !Agent.Value.TryGetComponent(out motor)) return Status.Failure;
        if (Target.Value == null) return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null) return Status.Failure;

        motor.Chase(Target.Value.transform.position);
        return Status.Running;
    }
}