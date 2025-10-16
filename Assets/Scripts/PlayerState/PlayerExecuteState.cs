using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private IDamageable target;
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        target = player.TargetingSystem.CurrentTarget;
        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }
        player.Motor.StopMovement();
        PlayerCamera.Instance.cameraZPosition = -4f;
        player.Combat.AttemptDeathblow(target.gameObject.GetComponent<Enemy>());
    }

    public override void Exit()
    {
        PlayerCamera.Instance.ResetCameraZPostion();
    }
}
