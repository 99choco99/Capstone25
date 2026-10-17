using UnityEngine;

public class PlayerGuardState : PlayerState
{
    private float GuardLockTime => CombatSettings.Current.GuardInputLockTime; // 이 동안 제자리에 멈춘다

    private readonly PlayerGuardDefense defense = new();
    private uint lastGuardPressVersion;
    private uint inputResetVersion;

    private float HitReactionTimer;
    private float hitRecoveryTimer;
    private float guardReactionTimer;
    private bool isGuardHitReaction;
    private bool isNormalGuardReaction;
    private bool isPlayingGuardEntry;

    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public bool IsRecoveringFromHit => hitRecoveryTimer > 0f;
    public GuardDefensePhase DefensePhase => defense.Phase;

    private bool HasParryWindow => player.Combat.CurrentDefenseType == DefenseType.Parry;
    private bool HasGuardDefense => defense.HasGuardDefense(Time.time, player.InputHandler.GuardInput);

    public override PostureRecoveryMode PostureRecoveryMode => IsRecoveringFromHit || isNormalGuardReaction && isGuardHitReaction
        ? PostureRecoveryMode.HitReaction
        : HasGuardDefense ? PostureRecoveryMode.GuardBoosted : PostureRecoveryMode.Normal;

    public void SetHitRecovery(float remainingDuration)
    {
        hitRecoveryTimer = remainingDuration;
        isGuardHitReaction = false;
        PlayGuardEntry();
    }

    public override void HandleDamage(DamageResult result)
    {
        player.InputBuffer.CancelHeldAttack();
        switch (result.DefenseType)
        {
            case DefenseType.Parry:
                int parryAnim = result.Request.UseHeavyDefenseFeedback
                    ? AnimHash.ParryHeavy : AnimHash.Parry;
                PlayGuardReaction(parryAnim, result);
                return;
            case DefenseType.NormalGuard:
                // 공격 강도에 맞춰 작은 가드 반동과 크게 버티는 동작을 구분합니다.
                int guardAnim = AnimationController.GetGuardReactionHash(result);
                PlayGuardReaction(guardAnim, result);
                return;
            default:
                base.HandleDamage(result);
                return;
        }
    }

    public override void Enter()
    {
        HitReactionTimer = 0f;
        hitRecoveryTimer = 0f;
        guardReactionTimer = 0f;
        isNormalGuardReaction = false;
        isPlayingGuardEntry = false;
        isGuardHitReaction = player.AnimatorController.IsPlayingReaction;
        inputResetVersion = player.InputHandler.InputResetVersion;
        float inputAge = Time.time - player.InputHandler.LastGuardPressedGameTime;
        bool freshInput = player.InputHandler.GuardPressVersion != lastGuardPressVersion
            && inputAge >= 0f && inputAge <= CombatSettings.Current.PlayerInputBufferTime;
        lastGuardPressVersion = player.InputHandler.GuardPressVersion;
        defense.Enter(Time.time, freshInput, CombatSettings.Current);
        if (player.InputBuffer.PeekValidCommand() == ActionCommand.Guard)
            player.InputBuffer.ConsumeCurrentCommand();
        UpdateDefense();

        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.ForceStopLocomotion();
        if (!isGuardHitReaction)
            PlayGuardEntry();
    }

    private void HandleGuardInput()
    {
        if (inputResetVersion != player.InputHandler.InputResetVersion)
        {
            inputResetVersion = player.InputHandler.InputResetVersion;
            lastGuardPressVersion = player.InputHandler.GuardPressVersion;
            defense.Exit();
        }

        if (player.InputBuffer.PeekValidCommand() != ActionCommand.Guard)
            return;
        if (player.InputHandler.GuardPressVersion == lastGuardPressVersion)
        {
            player.InputBuffer.ConsumeCurrentCommand();
            return;
        }

        // 허용 전 입력은 공용 버퍼에 남깁니다. 만료·다른 행동·입력 초기화가 취소합니다.
        if (!defense.TryReenter(Time.time, CombatSettings.Current))
            return;

        lastGuardPressVersion = player.InputHandler.GuardPressVersion;
        player.InputBuffer.ConsumeCurrentCommand();
        if (!isGuardHitReaction && !IsRecoveringFromHit)
            PlayGuardEntry();
    }

    private void UpdateDefense()
    {
        player.Combat.CurrentDefenseType = defense.GetDefense(Time.time, player.InputHandler.GuardInput);
    }

    public override void Update()
    {
        hitRecoveryTimer -= Time.deltaTime;
        guardReactionTimer -= Time.deltaTime;
        HandleGuardInput();
        float guardTimer = Mathf.Max(0f, Time.time - defense.ActionStartTime);
        UpdateDefense();
        UpdateGuardMotion();

        if (HitReactionTimer > 0f || IsRecoveringFromHit)
        {
            HitReactionTimer -= Time.deltaTime;
            if (!IsRecoveringFromHit)
                player.InputBuffer.KeepAttackUntil(Time.time + Mathf.Max(0f, HitReactionTimer));
            HandleGuardMovement(Vector3.zero);
            return;
        }

        if (player.InputBuffer.PeekValidCommand() != ActionCommand.Guard)
            base.HandleInput();

        if (stateMachine.CurrentState != this)
            return;

        if (guardTimer < GuardLockTime || !player.InputHandler.GuardInput && (HasParryWindow || HasGuardDefense))
        {
            HandleGuardMovement(Vector3.zero);
            return;
        }

        if (!player.InputHandler.GuardInput)
        {
            if (!isGuardHitReaction && !player.AnimatorController.IsPlayingReaction)
                player.AnimatorController.PlayGuardMotion(AnimHash.PlayerGuardRelease);
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        HandleGuardMovement(player.GetDesiredMoveDirection());
    }

    private void PlayGuardEntry()
    {
        isPlayingGuardEntry = true;
        player.AnimatorController.PlayGuardMotion(AnimHash.PlayerGuardEnter);
    }

    private void PlayGuardHold()
    {
        isPlayingGuardEntry = false;
        player.AnimatorController.PlayGuardMotion(AnimHash.Guard);
    }

    private void UpdateGuardMotion()
    {
        if (isGuardHitReaction)
        {
            if (HitReactionTimer <= 0f && guardReactionTimer <= 0f && !player.AnimatorController.IsPlayingReaction)
            {
                isGuardHitReaction = false;
                PlayGuardHold();
            }
            return;
        }

        if (isPlayingGuardEntry
            && player.AnimatorController.TryGetAttackProgress(
            AnimHash.PlayerGuardEnter,
            out _,
            out float progress)
            && progress >= 1f)
            PlayGuardHold();
    }

    /// <summary>
    /// 가드 중 이동 및 회전 통합 처리
    /// </summary>
    private void HandleGuardMovement(Vector3 moveDir)
    {
        if (isGuardHitReaction && moveDir != Vector3.zero)
        {
            isGuardHitReaction = false;
            PlayGuardHold();
        }

        player.Motor.SetMovement(moveDir * player.Motor.GuardSpeed);

        RotateToTargetOrMove(moveDir);

        UpdateLocomotionAnimation(moveDir);
    }

    /// <summary>
    /// 움직임 애니메이션 동기화
    /// </summary>
    private void UpdateLocomotionAnimation(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero)
        {
            player.AnimatorController.UpdateLocomotion(0, 0);
        }
        else
        {
            Vector2 input = GetLocomotionInput(moveDir);
            player.AnimatorController.UpdateLocomotion(input.x, input.y);
        }
    }

    protected override void OnAttackCommand()
    {
        if (RequestDeathblow())
            return;

        stateMachine.RequestedAttackData = player.Combat.FirstAttackData;
        stateMachine.TransitionTo(stateMachine.PlayerAttackState);
    }

    private void PlayGuardReaction(int animationHash, in DamageResult result)
    {
        float recoveryDuration = CombatSettings.Current.GetPlayerDefenseRecovery(result);

        defense.BeginContact(Time.time, result, CombatSettings.Current);
        UpdateDefense();
        HitReactionTimer = recoveryDuration;
        guardReactionTimer = CombatSettings.Current.GetGuardReactionDuration(result, recoveryDuration);
        isGuardHitReaction = true;
        isNormalGuardReaction = result.DefenseType == DefenseType.NormalGuard;
        isPlayingGuardEntry = false;
        player.AnimatorController.PlayReaction(animationHash, AnimationController.GuardTransitionDuration);
    }

    public override void Exit()
    {
        player.InputBuffer.CancelHeldAttack();
        defense.Exit();
        lastGuardPressVersion = player.InputHandler.GuardPressVersion;
        HitReactionTimer = 0f;
        hitRecoveryTimer = 0f;
        guardReactionTimer = 0f;
        isGuardHitReaction = false;
        isNormalGuardReaction = false;
        isPlayingGuardEntry = false;
        player.Combat.CurrentDefenseType = DefenseType.None;
    }
}
