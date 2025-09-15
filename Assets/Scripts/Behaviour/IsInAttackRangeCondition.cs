using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsInAttackRange", story: "Is In [AttackRange]", category: "Conditions", id: "b0b777b68dd52ca6807198b9be68dfd7")]
public partial class IsInAttackRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<float> AttackRange;

    EnemySense senses;
    public override bool IsTrue()
    {
        if (senses == null)
        {
            Debug.LogError("Enemy의 Sesnse 컴포넌트 없음.");
            return false;
        }
        return senses.IsTargetInAttackRange(AttackRange);
    }

    public override void OnStart()
    {
        if(senses == null && GameObject != null)
        {
            senses = GameObject.GetComponent<Enemy>()?.Senses;
        }
    }

    public override void OnEnd()
    {
    }
}
