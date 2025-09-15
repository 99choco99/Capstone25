using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Distance high than OptimalRange", story: "Is Distance > [OptimalRange]", category: "Conditions", id: "cb131a6615f09c7438c22dc10819a2e7")]
public partial class IsDistanceHighThanOptimalRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<float> OptimalRange;
    EnemySense senses;
    public override bool IsTrue()
    {
        if (senses == null)
        {
            Debug.LogError("Enemy의 Sesnse 컴포넌트 없음.");
            return false;
        }
        return senses.DistanceToTarget > OptimalRange.Value;
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
