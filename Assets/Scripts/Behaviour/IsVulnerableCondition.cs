using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsVulnerable", story: "Is Vulnerable?", category: "Variable Conditions", id: "e260773de354f73b771ba39c68729455")]
public partial class IsVulnerableCondition : Condition
{
    private EnemySense senses;
    public override bool IsTrue()
    {
        if(senses == null)
        {
            Debug.LogError("Enemy의 Sesnse 컴포넌트 없음.");
            return false;
        }

        return senses.IsPlayerVulnerable;
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
