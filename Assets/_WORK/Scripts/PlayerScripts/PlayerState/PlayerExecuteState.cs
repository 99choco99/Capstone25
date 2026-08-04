using UnityEngine;
using UnityEngine.Playables;

public class PlayerExecuteState : PlayerState
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private ITargetable target;
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        player.SetInvincible(true);
        player.Combat.ForceResetAttackState();
        player.Motor.SetMovement(Vector3.zero);

        target = player.TargetingSystem.CurrentTarget;
        player.TargetingSystem.DeselectTarget();

        if (target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        if (player.TargetingSystem.CurrentTarget is Enemy enemy)
        {
            player.Execution.OnExecuteEnd += OnCutsceneEnded;
            if (!player.Execution.AttemptDeathblow(enemy))
            {
                player.Execution.OnExecuteEnd -= OnCutsceneEnded;
                stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            }
        }
        else stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
    }

    private void OnCutsceneEnded()
    {
        player.Execution.OnExecuteEnd -= OnCutsceneEnded;
        stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
    }

    public override void Exit()
    {
        player.Stats.IsInvincible = false;
    }

}
