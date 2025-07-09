using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get Distance", story: "Get Distance Between [A] And [B]", category: "Action", id: "787c23ea977542775c2c8c497745f071")]
public partial class GetDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> A;
    [SerializeReference] public BlackboardVariable<GameObject> B;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Vector3.Distance(A.Value.transform.position, B.Value.transform.position);
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

