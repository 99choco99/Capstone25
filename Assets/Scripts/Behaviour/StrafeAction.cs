using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.AppUI.UI;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Strafe", story: "Strafe In [Distance] For [Duration]", category: "Action", id: "808cdee79382b266c49a16a8f9b2f2c9")]
public partial class StrafeAction : Action
{
    EnemyMotor motor;
    EnemySense sense;
    [SerializeReference] public BlackboardVariable<float> Distance;
    [SerializeReference] public BlackboardVariable<float> Duration;

    protected override Status OnStart()
    {

        if (motor == null && GameObject != null)
        {
            motor = GameObject.GetComponent<Enemy>()?.Motor;
            sense = GameObject.GetComponent<EnemySense>();
        }
        if (motor == null || sense == null || sense.Target == null)
        {
            return Status.Failure;
        }

        motor.StartStrafe(Duration, sense.Target, Distance);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (motor != null)
        {
            motor.StopStrafe();
        }
    }
}

