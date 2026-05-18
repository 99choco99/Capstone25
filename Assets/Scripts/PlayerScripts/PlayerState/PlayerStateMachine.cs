using UnityEngine;
using System;


public class PlayerStateMachine
{
    //==============상태들==============
    public PlayerGroundedState PlayerGroundedState { get; private set; }  // 움직임 상태
    public PlayerJumpState PlayerJumpState { get; private set; } //점프 상태
    public PlayerAttackState PlayerAttackState { get; private set; }   // 공격 중인 상태
    public PlayerGuardState PlayerGuardState { get; private set; }  // 가드 상태

    public PlayerDodgeState PlayerRollingState { get; private set; } // 구르고 있는 상태
    public PlayerSprintState PlayerSprintState { get; private set; }   // 슬라이드 상태
    public PlayerExecuteState PlayerExecuteState { get; private set; }
    public ConversationState ConversationState { get; private set; }  // 대화 상태

    public PlayerHitState PlayerHitState { get; private set; }
    public PlayerStunState PlayerStunState { get; private set; }
    public PlayerDeadState PlayerDeadState { get; private set; }


    //==============변수들==============
    public AttackType RequestedAttack { get; set; } = AttackType.None;
    public int RequestedHitAnimHash { get; set; }
    public State CurrentState { get; private set; }  // 현재 상태



    //플레이어 상태들
    public PlayerStateMachine(Player player)
    {
        PlayerGroundedState = new PlayerGroundedState(player, this);
        PlayerJumpState = new PlayerJumpState(player, this);
        PlayerAttackState = new PlayerAttackState(player, this);
        PlayerGuardState = new PlayerGuardState(player, this);
        PlayerRollingState = new PlayerDodgeState(player, this);
        PlayerSprintState = new PlayerSprintState(player, this);
        PlayerExecuteState = new PlayerExecuteState(player, this);
        PlayerDeadState = new PlayerDeadState(player, this);
        PlayerHitState = new PlayerHitState(player, this);
        PlayerStunState = new PlayerStunState(player, this);
        ConversationState = new ConversationState(player, this);

        player.Combat.OnHitStunTriggered += HandleHitStun;

        TransitionTo(PlayerGroundedState);
    }
    private void HandleHitStun(int hitAnimHash)
    {
        RequestedHitAnimHash = hitAnimHash;

        TransitionTo(PlayerHitState);
    }

    // 상태 전이
    public void TransitionTo(State nextState)
    {
        CurrentState?.Exit();
        CurrentState = nextState;
        nextState.Enter();
    }

    public void Update() => CurrentState?.Update();
    public void FixedUpdate() => CurrentState?.FixedUpdate();

}



