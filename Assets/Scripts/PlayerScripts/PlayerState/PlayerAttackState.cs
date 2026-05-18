using UnityEngine;


public enum AttackPhase { WindUp,Active, Recovery}

public class PlayerAttackState : State
{
    private AttackType pendingAttackType;
    private AttackPhase currentPhase;
    public override bool UseRootMotion => true;

    public PlayerAttackState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.StopMovement();
        player.AnimatorController.UpdateLocomotion(0, 0);

        pendingAttackType = stateMachine.RequestedAttack;

        stateMachine.RequestedAttack = AttackType.None;

        ExecutePendingAttack();
    }

    public override void Update()
    {
        if (currentPhase == AttackPhase.WindUp)
        {
            player.Motor.HandleRotation();
        }
        else if(currentPhase == AttackPhase.Recovery)
        {
            if(player.InputHandler.MoveInput != Vector3.zero)
            {
                stateMachine.TransitionTo(player.StateMachine.PlayerGroundedState);
                return;
            }
        }

        if (player.InputHandler.AttackInput && player.TargetingSystem.IsCurrentTargetExecutable())
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
            return;
        }
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            pendingAttackType = AttackType.Normal;
            return;
        }
    }

    public override void Exit()
    {
        pendingAttackType = AttackType.None;
        player.Combat.ResetCombo();
    }

    public void OnAttackActiveStart()
    {
        if (currentPhase != AttackPhase.WindUp) { return; }
        currentPhase = AttackPhase.Active;
    }

    public void OnCheckNextAttack()
    {
        if(currentPhase != AttackPhase.Active) { return; }

        if (pendingAttackType != AttackType.None)
        {
            ExecutePendingAttack();
        }
        else
        {
            currentPhase = AttackPhase.Recovery;
        }

    }

    public override void OnAnimationEnd()
    {
        if (currentPhase != AttackPhase.Recovery) { return; }
        ReturnToLocomotion();
    }

    private void ExecutePendingAttack()
    {
        bool attackStarted = false;

        switch (pendingAttackType)
        {
            case AttackType.Normal: attackStarted = player.Combat.StartNormalAttack(); break;
            case AttackType.Heavy: attackStarted = player.Combat.StartHeavyAttack(); break;
            case AttackType.SprintAttack: attackStarted = player.Combat.StartSprintAttack(); break;
        }

        if (attackStarted)
        {
            currentPhase = AttackPhase.WindUp;
        }
        else
        {
            ReturnToLocomotion();
        }

        pendingAttackType = AttackType.None;
    }

    private void ReturnToLocomotion()
    {
        stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
    }
}
