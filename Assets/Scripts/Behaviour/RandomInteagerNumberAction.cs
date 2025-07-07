using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Random Inteager Number", story: "Set Random [Number] between [A] and [B]", category: "Action", id: "ba8baef7b3ae57e7ce0b62affa6efcb2")]
public partial class RandomInteagerNumberAction : Action
{
    [SerializeReference] public BlackboardVariable<int> Number;
    [SerializeReference] public BlackboardVariable<int> A;
    [SerializeReference] public BlackboardVariable<int> B;

    protected override Status OnStart()
    {
        Number.Value = UnityEngine.Random.Range(A.Value, B.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(Number.Value < 0 || Number.Value >= B.Value) { return Status.Failure; }
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

