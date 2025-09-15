using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsDead", story: "IsDead", category: "Conditions", id: "598ed49c68d0243a7399f791f12ce81f")]
public partial class IsDeadCondition : Condition
{
    EnemyStats stats;

    public override bool IsTrue()
    {
        if (stats == null)
        {
            Debug.LogError("Enemy의 stats 컴포넌트 없음.");
            return false;
        }

        return stats.dead;
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
