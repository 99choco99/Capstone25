using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perform Attack", story: "[Agent]가 지능적으로 공격한다", category: "Action/Enemy", id: "perform_attack")]
public partial class PerformAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    private EnemyCombat combat;

    protected override Status OnStart()
    {
        if (Agent.Value == null || !Agent.Value.TryGetComponent(out combat))
            return Status.Failure;

        if (combat.canAttack)
        {
            combat.PerformAttack();
            return Status.Success; // 공격 명령 성공!
        }

        return Status.Failure; //쿨타임이라 공격 실패
    }
}