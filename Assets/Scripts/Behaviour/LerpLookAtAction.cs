using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Lerp LookAt", story: "[GameObject] looks at [target] by [value]", category: "Action", id: "efeb0ca8aabf804b96acfd1391b0d341")]
public partial class LerpLookAtAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> gameObject;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Value;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(Target == null) { return Status.Failure; }
        // 타겟의 위치를 향하는 방향 계산
        Vector3 direction = Target.Value.transform.position - gameObject.Value.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Lerp를 통해 회전 적용
        gameObject.Value.transform.rotation = Quaternion.Slerp(gameObject.Value.transform.rotation, targetRotation, Value * Time.deltaTime);

        // 회전 완료 체크 (20도 이내 차이)
        if (Quaternion.Angle(gameObject.Value.transform.rotation, targetRotation) < 20.0f)
        {
            return Status.Success; // 회전 완료
        }

        return Status.Running; // 아직 회전 중
    }

    protected override void OnEnd()
    {
    }
}

