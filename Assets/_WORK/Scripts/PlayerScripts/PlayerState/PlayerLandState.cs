using UnityEngine;

public class PlayerLandState : PlayerState
{
    //¹Ì¿Ï¼º
    public override bool UseRootMotion => false;

    private float landRecoveryTimer;
    private readonly float hardLandVelocityThreshold = -15f;
    private readonly float hardLandRecoveryTime = 0.5f;
    private bool isHardLanding;

    public PlayerLandState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        float impactVelocity = player.Motor.CurrentVerticalVelocity;
        if (impactVelocity <= hardLandVelocityThreshold)
        {
            isHardLanding = true;
            landRecoveryTimer = hardLandRecoveryTime;

            player.AnimatorController.PlayAction(AnimHash.HardLand);
            player.Motor.SetMovement(Vector3.zero);
        }
        else
        {
            isHardLanding = false;
            landRecoveryTimer = 0f;

            player.AnimatorController.PlayAction(AnimHash.SoftLand);
        }
    }

    public override void Update()
    {
        if (!isHardLanding)
        {
            Vector3 moveDir = player.GetDesiredMoveDirection();
            Vector3 calculatedInputMove = moveDir * player.Motor.MoveSpeed;
            player.Motor.SetMovement(calculatedInputMove);

            if (moveDir != Vector3.zero) player.Motor.RotateToDirection(moveDir);
        }
        landRecoveryTimer -= Time.deltaTime;

        if (landRecoveryTimer <= 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {

    }
}