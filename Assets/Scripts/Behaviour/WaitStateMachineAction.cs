using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait StateMachine", story: "Wait ExitStateMachine of [gameObject]", category: "Action/Delay", id: "bd8613fdc0d43eeca2b565f59a8c14a1")]
public partial class WaitStateMachineAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> gameObject;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {

    }
}

