using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsPlayerAttacking", story: "Is PlayerAttacking", category: "Conditions", id: "78e554c48cd5f42a65ebfcebc199191a")]
public partial class IsPlayerAttackingCondition : Condition
{
    EnemySense senses;

    public override bool IsTrue()
    {
        if (senses == null)
        {
            Debug.LogError("Enemy의 Sesnse 컴포넌트 없음.");
            return false;
        }

        return senses.IsPlayerAttacking;
    }

    public override void OnStart()
    {
        if (senses == null && GameObject != null)
        {
            senses = GameObject.GetComponent<Enemy>()?.Senses;
        }

    }

    public override void OnEnd()
    {
    }
}
