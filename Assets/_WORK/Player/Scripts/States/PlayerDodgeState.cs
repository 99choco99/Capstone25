using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    public PlayerDodgeState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override bool UseRootMotion => true;

    private float iframeDuration => CombatSettings.Current.PlayerDodgeInvincibleDuration;
    private float dodgeDuration => CombatSettings.Current.PlayerDodgeDuration;
    private float stateTimer = 0f;
    public float RemainingDuration => Mathf.Max(0f, dodgeDuration - stateTimer);

    public override void Enter()
    {
        // 입력 방향과 무관하게 현재 바라보는 방향을 유지한 채 뒤로 빠집니다.
        // 이 값을 0으로 고정해야 예전에 사용하던 방향 Dodge Blend Tree의 흔적도 남지 않습니다.
        player.AnimatorController.SetLocomotion(0f, 0f);
        player.Motor.SetMovement(Vector3.zero);
        player.Motor.StopKnockback();

        stateTimer = 0f;
        player.AnimatorController.PlayAction(AnimHash.Dodge, 0.08f);
        player.SetInvincible(true);
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= iframeDuration && player.Stats.IsInvincible)
        {
            player.SetInvincible(false);
        }

        if (stateTimer >= dodgeDuration)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
        player.SetInvincible(false);
        player.Motor.SetMovement(Vector3.zero);
    }
}
