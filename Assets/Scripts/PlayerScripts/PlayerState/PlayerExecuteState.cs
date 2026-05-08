using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private IDamageable target;
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        player.Stats.isInvincible = true;
        player.Motor.canMove = false;
        player.Motor.canRotate = false;
        player.InputHandler.enabled = false;

        target = player.TargetingSystem.CurrentTarget;

        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }
        player.Motor.StopMovement();
        Legacy_PlayerCamera.Instance.cameraZPosition = -4f;
        player.Combat.AttemptDeathblow(target.gameObject.GetComponent<Enemy>());
    }

    public override void Exit()
    {
        Legacy_PlayerCamera.Instance.ResetCameraZPostion();
        player.Stats.isInvincible = false;
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        player.InputHandler.enabled = true;
    }
}
