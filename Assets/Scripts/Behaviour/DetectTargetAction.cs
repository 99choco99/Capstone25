using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DetectTarget", story: "[Target] In [ChaseDistance] of [Self]", category: "Action", id: "9f2056f8589d38b2a453f495934c35e9")]
public partial class DetectTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> ChaseDistance;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    private readonly int layerMask = 1 << 6;

    protected override Status OnUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(Self.Value.transform.position, ChaseDistance.Value, layerMask);
        if(hits.Length > 0)
        {
            Target.Value = hits[0].gameObject;
        }
        else
        {
            Target.Value = null;
        }
        return Status.Success;
    }

}

