using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action; // System.Action과의 충돌을 피하기 위한 별칭
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait Animation", story: "Wait [Animation] of [Animator]", category: "Action/Delay", id: "2a681c331eae3e5fbe5810091b62149f")]
public partial class WaitAnimationAction : Action
{
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<string> Animation;

    // 선택 사항: 애니메이션이 이미 감지되었는지 추적하여 한 번의 실행에서 여러 번 성공하는 것을 방지합니다.
    private bool animationDetected = false;

    protected override Status OnStart()
    {
        // 노드가 시작될 때 감지 플래그를 재설정합니다.
        animationDetected = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Animator.Value == null || string.IsNullOrEmpty(Animation.Value))
        {
            Debug.LogWarning("WaitAnimationAction: Animator 또는 Animation 이름이 설정되지 않았습니다.");
            return Status.Failure; // 또는 원하는 동작에 따라 Running 반환
        }

        // 현재 상태가 원하는 애니메이션인지 확인합니다.
        if (Animator.Value.GetCurrentAnimatorStateInfo(0).IsName(Animation.Value))
        {
            if (!animationDetected) // 시작당 한 번만 성공
            {
                animationDetected = true;
                return Status.Success;
            }
        }

        // 또한 Animator가 현재 원하는 애니메이션으로 전환 중인지 확인합니다.
        // 이는 애니메이션이 활성화되려고 할 때 이를 포착하는 데 중요합니다.
        if (Animator.Value.IsInTransition(0))
        {
            AnimatorStateInfo nextStateInfo = Animator.Value.GetNextAnimatorStateInfo(0);
            if (nextStateInfo.IsName(Animation.Value))
            {
                if (!animationDetected) // 시작당 한 번만 성공
                {
                    animationDetected = true;
                    return Status.Success;
                }
            }
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        // 이 예시에서는 특별한 정리 작업이 필요하지 않습니다.
    }
}