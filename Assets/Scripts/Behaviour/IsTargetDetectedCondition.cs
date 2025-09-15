using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Target Detected", story: "Is Target Detected", category: "Conditions", id: "b5e95abf92cf451a64e58871e2a65b50")]
public partial class IsTargetDetectedCondition : Condition
{
    EnemySense senses;

    public override bool IsTrue()
    {
        if (senses == null)
        {
            Debug.LogError("Enemy의 Sesnse 컴포넌트 없음.");
            return false;
        }

        return senses.IsTargetDetected;
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
