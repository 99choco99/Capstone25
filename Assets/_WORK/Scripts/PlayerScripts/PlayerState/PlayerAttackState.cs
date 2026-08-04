using UnityEngine;
public enum AttackPhase { WindUp,Active, Recovery}

public class PlayerAttackState : PlayerState
{
    // 적에게 패링당했을 때 반동 시간
    private const float DeflectReboundDuration = 0.35f;

    private AttackData currentAttackData;
    private AttackPhase currentPhase;
    private float stateTimer;
    private float reboundTimer;
    private bool isRebounding;
    private bool hasCommittedAttack;
    private bool hasBufferedGuard;

    public override bool UseRootMotion => !isRebounding;

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
        if (isRebounding)
        {
            BufferGuardCommand();

            reboundTimer += Time.deltaTime;
            player.Motor.SetMovement(Vector3.zero);

            if (reboundTimer >= DeflectReboundDuration)
            {
                stateMachine.TransitionTo(hasBufferedGuard ? stateMachine.PlayerGuardState : stateMachine.PlayerGroundedState);
            }

            return;
        }

        //공격 취소 시도 검사
        if (CancelAttack()) return;


        stateTimer += Time.deltaTime;
        float normalizedTime = stateTimer / currentAttackData.DurationInSeconds;

        if (normalizedTime < currentAttackData.CommitTime) return;

        AttackCommit(normalizedTime);       // 공격을 확정
        UpdatePhase(normalizedTime);        // 시간에 따른 페이즈 전환
        if (stateMachine.CurrentState != this) return;
        HandleCombatInput(normalizedTime);  // 현재 페이즈에 따른 유저 입력 처리
    }

    /// <summary>
    /// 공격 확정 전 공격 취소
    /// </summary>
    private bool CancelAttack()
    {
        ActionCommand cmd = player.InputBuffer.PeekValidCommand();
        if (currentPhase != AttackPhase.WindUp || hasCommittedAttack || (cmd != ActionCommand.Guard && cmd != ActionCommand.Dodge))
        {
            return false;
        }

        player.InputBuffer.ConsumeCurrentCommand();
        stateMachine.TransitionTo(cmd == ActionCommand.Guard ? stateMachine.PlayerGuardState : stateMachine.PlayerDodgeState);
        return true;
    }


    /// <summary>
    /// Attack을 확정짓는 순간. Enemy에게 이벤트 발송, 더이상 공격 취소 불가
    /// </summary>
    private void AttackCommit(float normalizedTime)
    {
        if (hasCommittedAttack || currentPhase != AttackPhase.WindUp)
        {
            return;
        }

        hasCommittedAttack = true;

        float remainingUntilActive = Mathf.Max(0f, currentAttackData.DurationInSeconds * (currentAttackData.ActiveStartTime - normalizedTime));

        player.Combat.CommitCurrentAttack(Time.time + remainingUntilActive);
    }

    /// <summary>
    /// 시간에 따라 공격 페이즈 변경
    /// </summary>
    private void UpdatePhase(float nTime)
    {
        if (currentPhase == AttackPhase.WindUp && nTime >= currentAttackData.ActiveStartTime)
        {
            currentPhase = AttackPhase.Active;
            player.Combat.PlayerAttackStart();

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFXAtPoint(SfxKeys.Attack, player.Combat.CurrentWeapon.transform.position);
        }
        else if (currentPhase == AttackPhase.Active && nTime >= currentAttackData.RecoveryStartTime)
        {
            currentPhase = AttackPhase.Recovery;
            player.Combat.PlayerAttackEnd();
        }
        else if (currentPhase == AttackPhase.Recovery && nTime >= 1.0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    /// <summary>
    /// attack중 인풋 저장 
    /// </summary>
    private void HandleCombatInput(float nTime)
    {
        BufferGuardCommand();

        if (currentPhase != AttackPhase.Recovery) { return; }

        if (hasBufferedGuard)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }

        if(nTime < currentAttackData.ComboStartTime) { return; }

        ActionCommand cmd = player.InputBuffer.PeekValidCommand();

        if (cmd == ActionCommand.Attack)
        {
            if (player.TargetingSystem.GetDeathblowPlan(out DeathblowPlan opportunity))
            {
                player.InputBuffer.ConsumeCurrentCommand();
                stateMachine.RequestedDeathblowPlan = opportunity;
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
    }

    /// <summary>
    /// 가드 입력 버퍼에 저장
    /// </summary>
    private void BufferGuardCommand()
    {
        if (player.InputBuffer.PeekValidCommand() != ActionCommand.Guard) return;

        player.InputBuffer.ConsumeCurrentCommand();
        hasBufferedGuard = true;
    }

    /// <summary>
    /// attackData 사용, 실질적인 공격 시작
    /// </summary>
    private void ExecuteAttackData()
    {
        if (currentAttackData != null)
        {
            stateTimer = 0f;
            currentPhase = AttackPhase.WindUp;
            hasCommittedAttack = false;
            hasBufferedGuard = false;
            player.AnimatorController.PlayAttack(currentAttackData.AnimationHash, currentAttackData.AnimationSpeed);
            player.Combat.SetCurrentAttackData(currentAttackData);
        }
        else
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    /// <summary>
    /// 공격이 상대에게 닿아 결과가 확정됐을 때
    /// </summary>
    public override void HandleAttackAccepted(in DamageResult result)
    {
        if (result.DefenseType != DefenseType.Parry || currentAttackData.DeflectResponse == DeflectResponse.Continue)
        {
            return;
        }

        isRebounding = true;
        reboundTimer = 0f;
        currentPhase = AttackPhase.Recovery;

        //판정 닫기
        player.Combat.ForceResetAttackState();
        player.Motor.SetMovement(Vector3.zero);

        //공격자 넉백
        KnockbackSpec knockback = KnockBackPolicy.AttackerKnockBack(result);
        player.Motor.StartKnockback(-result.HitDirection, knockback);

        player.AnimatorController.PlayReaction(AnimHash.AttackRebound, 0.05f);
    }


    /// <summary>
    /// WindUp 동안 공격 방향을 조절
    /// </summary>
    public void RotateDuringWindUp()
    {
        if (isRebounding || currentPhase != AttackPhase.WindUp)
            return;

        Vector3 desiredDirection = Vector3.zero;
        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            desiredDirection = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
        }
        else
        {
            desiredDirection = player.GetDesiredMoveDirection();
        }

        desiredDirection.y = 0f;
        if (desiredDirection.sqrMagnitude > 0.0001f)
            player.Motor.RotateToDirection(desiredDirection);
    }


    public override void Exit()
    {
        currentAttackData = null;
        currentPhase = AttackPhase.WindUp;
        isRebounding = false;
        reboundTimer = 0f;
        hasCommittedAttack = false;
        hasBufferedGuard = false;
        player.Combat.ForceResetAttackState();
    }
}
