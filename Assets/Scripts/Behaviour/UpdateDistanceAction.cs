using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UpdateDistance", story: "Update [Distance] From [Self] To [Target]", category: "Action", id: "3ca683c4ad6030fe51380e039714b9d6")]
public partial class UpdateDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Distance;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;


    protected override Status OnUpdate()
    {
        if(Target.Value != null)
        {
            Distance.Value = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);
        }
        return Status.Success;
    }

    protected override void OnEnd()
    {

    }
}

