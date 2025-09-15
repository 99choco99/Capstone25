using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Create Space Distance", story: "Create Space [Distance]", category: "Action", id: "b8b28b8862153f9b5d6eacc931b7b0e0")]
public partial class CreateSpaceDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Distance;
    EnemyMotor motor;
    protected override Status OnStart()
    {
        if (motor == null && GameObject != null)
        {
            motor = GameObject.GetComponent<Enemy>()?.Motor;
        }
        if (motor == null)
        {
            return Status.Failure;
        }
        motor.Retreat(Distance.Value);
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

