using UnityEngine;

public class PlayerAttackState : State
{
    private bool isComboInputQueued;

    public override bool UseRootMotion => true;

    public PlayerAttackState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        isComboInputQueued = false;
        player.Motor.StopMovement();
        player.Combat.StartAttack();
        player.Combat.OnAttackEnd += HandleAttackEnd;
    }

    public override void Update()
    {
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            isComboInputQueued = true;
        }
    }

    public override void Exit()
    {
        player.Combat.OnAttackEnd -= HandleAttackEnd;
    }

    private void HandleAttackEnd()
    {
        if (isComboInputQueued)
        {
            isComboInputQueued = false;
            player.Combat.StartAttack();
        }
        else
        {
            if (player.InputHandler.MoveInput == Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            }
        }
    }
}
