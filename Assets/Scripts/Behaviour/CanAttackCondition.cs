using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Attack", story: "Can Attack", category: "Conditions", id: "4a52e41999e5f23be78357119f7de09a")]
public partial class CanAttackCondition : Condition
{
    EnemyCombat combat;
    public override bool IsTrue()
    {
        if(combat == null)
        {
            Debug.Log("combat 컴포넌트가 없음");
            return false;
        }
        return combat.canAttack ;
    }

    public override void OnStart()
    {
        if(combat == null && GameObject != null)
        {
            combat = GameObject.GetComponent<EnemyCombat>();
        }
    }

    public override void OnEnd()
    {
    }
}
