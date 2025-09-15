using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perform NormalAttack", story: "Perform NormalAttack", category: "Action", id: "f2a7a86ff31f440231d686a1eda9484b")]
public partial class PerformNormalAttackAction : Action
{
    EnemyCombat combat;
    private bool isAttackFinished;
    protected override Status OnStart()
    {
        if(combat == null && GameObject != null)
        {
            combat = GameObject.GetComponent<EnemyCombat>();
        }
        if(combat == null)
        {
            Debug.LogError("combat 컴포넌트 없음");
            return Status.Failure;
        }

        isAttackFinished = false;
        combat.OnAttackEnd += HandleAttackEnd;
        combat.PerformAttack();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (isAttackFinished)
        {
            return Status.Success;
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (combat != null)
        {
            combat.OnAttackEnd -= HandleAttackEnd;
        }
    }
    private void HandleAttackEnd()
    {
        isAttackFinished = true;
    }
}

