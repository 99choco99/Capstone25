using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WaitAnimationTime", story: "Wait [Animation] Time for [Animator]", category: "Action/Delay", id: "53ecbee3821bf2f956c65ed9876fa632")]
public partial class WaitAnimationTimeAction : Action
{
    [SerializeReference] public BlackboardVariable<string> Animation;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    float AnimationTime;

    protected override Status OnStart()
    {
        if (Animator.Value.GetCurrentAnimatorStateInfo(0).IsName(Animation))
        {
            AnimationTime = Animator.Value.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }
        else
        {
            return Status.Failure;
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(AnimationTime >= 1)
        {
            return Status.Success;
        }
        else
        {
            AnimationTime += Time.deltaTime;
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

