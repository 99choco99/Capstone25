using UnityEngine;


public enum AttackPhase { WindUp,Active, Recovery}

public class PlayerAttackState : PlayerState
{
    private AttackData currentAttackData;
    private AttackPhase currentPhase;
    private float stateTimer;
    public override bool UseRootMotion => true;

    public PlayerAttackState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.UpdateLocomotion(0, 0);

        currentAttackData = stateMachine.RequestedAttackData;
        stateMachine.RequestedAttackData = null;

        ExecuteAttackData();
    }

    public override void Update()
    {

        // 타이머 업데이트 및 타겟팅
        UpdateCoreLogic();
        // 시간에 따른 페이즈 전환
        UpdatePhaseTransition();
        // 현재 페이즈에 따른 유저 입력 처리
        HandleCombatInput();


    }

    private void UpdateCoreLogic()
    {
        stateTimer += Time.deltaTime;

        if (currentPhase == AttackPhase.WindUp && player.IsLockOn)
        {
            Vector3 dirToTarget = (player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position).normalized;
            player.Motor.RotateToDirection(dirToTarget);
        }
    }

    private void UpdatePhaseTransition()
    {
        float nTime = stateTimer / currentAttackData.DurationInSeconds;

        if (currentPhase == AttackPhase.WindUp && nTime >= currentAttackData.ActiveStartTime)
        {
            currentPhase = AttackPhase.Active;
            player.Combat.OnAnimationPlayerAttackStart();
        }
        else if (currentPhase == AttackPhase.Active && nTime >= currentAttackData.RecoveryStartTime)
        {
            currentPhase = AttackPhase.Recovery;
            player.Combat.OnAnimationPlayerAttackEnd();
        }
        else if (currentPhase == AttackPhase.Recovery && nTime >= 1.0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    private void HandleCombatInput()
    {
        ActionCommand cmd = player.InputBuffer.PeekValidCommand();

        switch (currentPhase)
        {
            case AttackPhase.WindUp:
                if (player.InputHandler.GuardInput)
                {
                    player.InputBuffer.ConsumeCurrentCommand();
                    stateMachine.TransitionTo(stateMachine.PlayerGuardState);
                }
                else if (cmd == ActionCommand.Dodge)
                {
                    player.InputBuffer.ConsumeCurrentCommand();
                    stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
                }
                break;

            case AttackPhase.Active:
                // 액티브 구간 입력 무시
                break;

            case AttackPhase.Recovery:
                if (cmd == ActionCommand.Attack)
                {
                    if (player.TargetingSystem.IsCurrentTargetExecutable())
                    {
                        player.InputBuffer.ConsumeCurrentCommand();
                        stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
                    }
                    else if (currentAttackData.NextComboAttack != null)
                    {
                        player.InputBuffer.ConsumeCurrentCommand();
                        currentAttackData = currentAttackData.NextComboAttack;
                        ExecuteAttackData();
                    }
                }
                else if (cmd == ActionCommand.Dodge)
                {
                    player.InputBuffer.ConsumeCurrentCommand();
                    stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
                }
                else if (player.InputHandler.GuardInput)
                {
                    player.InputBuffer.ConsumeCurrentCommand();
                    stateMachine.TransitionTo(stateMachine.PlayerGuardState);
                }
                break;
        }
    }

    private void ExecuteAttackData()
    {
        if (currentAttackData != null)
        {
            stateTimer = 0f;
            currentPhase = AttackPhase.WindUp;
            player.AnimatorController.PlayAction(currentAttackData.AnimationHash);
            player.Combat.SetCurrentAttackData(currentAttackData);
        }
        else
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {
        currentAttackData = null;
        currentPhase = AttackPhase.WindUp;
        player.Combat.ForceResetAttackState();
    }
}
