using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StopAction", story: "StopAction", category: "Action", id: "72457e132a3d2824f29fbaf1eb4b1cef")]
public partial class StopAction : Action
{
    EnemyMotor motor;
    protected override Status OnStart()
    {
        if(motor == null && GameObject != null)
        {
            motor = GameObject.GetComponent<EnemyMotor>();
        }
        if(motor == null)
        {
            Debug.LogError("motor 가 존재하지 않음.");
        }

        motor.Stop();
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

