using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is My Posture High", story: "Is My Posture High?", category: "Conditions", id: "7377506209d7e7c4d73d89fb6412561e")]
public partial class IsMyPostureHighCondition : Condition
{
    EnemyStats stats;
    public override bool IsTrue()
    {
        if (stats == null)
        {
            Debug.LogError("Enemy의 stats 컴포넌트 없음.");
            return false;
        }
        return stats.currentPosture >= (stats.maxPosture * 0.7f);
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
