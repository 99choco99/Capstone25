using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsPlayerAttackThreatening", story: "IsPlayerAttackThreatening", category: "Conditions", id: "c97ede5ec2750a96833c95edf4544311")]
public partial class IsPlayerAttackThreateningCondition : Condition
{
    private EnemySense senses;

    public override bool IsTrue()
    {

        if (senses == null)
        {
            return false;
        }
        return senses.IsPlayerAttackThreatening;
    }

    public override void OnStart()
    {
        senses = GameObject.GetComponent<Enemy>()?.Senses;
    }

    public override void OnEnd()
    {
    }
}
