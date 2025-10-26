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
    EnemyAnimationManager animManager;

    protected override Status OnStart()
    {
        combat = GameObject.GetComponent<EnemyCombat>();
        animManager = GameObject.GetComponent<EnemyAnimationManager>();


        if (animManager.IsPerformAction)
        {
            return Status.Failure;
        }

        combat.DecideDefenseAction();

        if (animManager.IsPerformAction)
        {
            return Status.Running;
        }
        else
        {
            return Status.Failure;
        }
    }

    protected override Status OnUpdate()
    {
        if (animManager.IsPerformAction)
        {
            return Status.Running;
        }
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

