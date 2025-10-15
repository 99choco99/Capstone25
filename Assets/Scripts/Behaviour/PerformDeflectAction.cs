using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PerformDeflect", story: "PerformDeflect", category: "Action", id: "be06278213da2c6c7c6c00930156d062")]
public partial class PerformDeflectAction : Action
{
    EnemyCombat combat;
    protected override Status OnStart()
    {
        combat = GameObject.GetComponent<EnemyCombat>();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        combat.DecideDefenseAction();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

