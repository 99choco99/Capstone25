using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perform HeavyAttack", story: "Perform HeavyAttack", category: "Action", id: "75c4a20ad56e9e0022fc02141e67b4e2")]
public partial class PerformHeavyAttackAction : Action
{
    EnemyCombat combat;
    protected override Status OnStart()
    {
        if (combat == null && GameObject != null)
        {
            combat = GameObject.GetComponent<EnemyCombat>();
        }
        if (combat == null)
        {
            Debug.LogError("combat 컴포넌트 없음");
            return Status.Failure;
        }

        combat.PerformHeavyAttack();

        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

