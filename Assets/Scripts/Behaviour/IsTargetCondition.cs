using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsTarget", story: "Is [Target]", category: "Conditions", id: "e7700724b7f24960d1abd22c9dcfd686")]
public partial class IsTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        if(Target.Value == null)
        {
            return false;
        }
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
