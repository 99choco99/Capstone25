using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Posture Broken", story: "Is Posture Broken", category: "Conditions", id: "e98c01151066ad2848de16f4d1099b2b")]
public partial class IsPostureBrokenCondition : Condition
{
    EnemyStats stats;

    public override bool IsTrue()
    {
        if (stats == null)
        {
            Debug.LogError("Enemy의 stats 컴포넌트 없음.");
            return false;
        }

        return stats.IsPostureBroken;
    }

    public override void OnStart()
    {
        if (stats == null && GameObject != null)
        {
            stats = GameObject.GetComponent<Enemy>()?.Stats;
        }

    }

    public override void OnEnd()
    {
    }
}
