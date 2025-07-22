using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetTransform", story: "Set [Transform] of [Target]", category: "Action", id: "d297bb89339848a3fe4eb9ecf5c8fa52")]
public partial class SetTransformAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> Transform;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {
        Transform.Value = Target.Value.transform.position;
        return Status.Success;
    }

}

