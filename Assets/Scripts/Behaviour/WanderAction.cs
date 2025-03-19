using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.UI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wander", story: "[Self] Navigation To WanderPosition", category: "Action", id: "83a8d5556c148c3b5ead070a813b1da6")]
public partial class WanderAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private UnityEngine.AI.NavMeshAgent agent;


    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

