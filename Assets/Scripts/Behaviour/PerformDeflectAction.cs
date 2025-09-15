using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perform Deflect", story: "Perform Deflect", category: "Action", id: "c2c886a55e1d77ca305ed3d5c7203518")]
public partial class PerformDeflectAction : Action
{
    EnemyMotor motor;

    protected override Status OnStart()
    {
        if(motor == null && GameObject != null)
        {
            motor = GameObject.GetComponent<Enemy>()?.Motor;
        }
        if (motor == null)
        {
            return Status.Failure;
        }
        motor.PerformDeflect();
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

