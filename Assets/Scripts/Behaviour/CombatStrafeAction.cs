using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Combat Strafe", story: "[Agent]가 [X] 방향과 [Z] 방향으로 걷는다", category: "Action/Enemy", id: "combat_strafe")]
public partial class CombatStrafeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> X;
    [SerializeReference] public BlackboardVariable<float> Z;

    private EnemyMotor motor;

    protected override Status OnStart()
    {
        if (Agent.Value == null || !Agent.Value.TryGetComponent(out motor))
            return Status.Failure;

        // 노드 진입 시 전투 이동 모드로 전환
        motor.SetMovementMode(EnemyMotor.MovementMode.CombatStrafe);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // 매 프레임 조이스틱 값 주입
        motor.SetCombatInput(X.Value, Z.Value);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // 노드가 끝나면 조이스틱에서 손을 뗌
        if (motor != null) motor.SetCombatInput(0, 0);
    }
}