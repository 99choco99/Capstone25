using UnityEngine;
using System;


public class PlayerStateMachine
{
    //==============상태들==============
    public PlayerGroundedState PlayerGroundedState { get; private set; }  // 움직임 상태
    public PlayerJumpState PlayerJumpState { get; private set; } //점프 상태
    public PlayerDodgeState PlayerDodgeState { get; private set; } // Shift 후방 도약 상태



    public PlayerAttackState PlayerAttackState { get; private set; }   // 공격 중인 상태
    public PlayerGuardState PlayerGuardState { get; private set; }  // 가드 상태
    public PlayerExecuteState PlayerExecuteState { get; private set; }
    public PlayerHitState PlayerHitState { get; private set; }


    public PlayerStunState PlayerStunState { get; private set; }
    public PlayerDeadState PlayerDeadState { get; private set; }
    public ConversationState ConversationState { get; private set; }  // 대화 상태


    //==============변수들==============
    public AttackData RequestedAttackData { get; set; }

    /// <summary>
    /// 판정된 인살 정보를 ExecuteState에 전달
    /// </summary>
    public DeathblowPlan? RequestedDeathblowPlan { get; set; }

    public PlayerState CurrentState { get; private set; }  // 현재 상태



    //플레이어 상태들
    public PlayerStateMachine(Player player)
    {
        PlayerGroundedState = new PlayerGroundedState(player, this);
        PlayerJumpState = new PlayerJumpState(player, this);
        PlayerAttackState = new PlayerAttackState(player, this);
        PlayerGuardState = new PlayerGuardState(player, this);
        PlayerDodgeState = new PlayerDodgeState(player, this);
        PlayerExecuteState = new PlayerExecuteState(player, this);
        PlayerDeadState = new PlayerDeadState(player, this);
        PlayerHitState = new PlayerHitState(player, this);
        PlayerStunState = new PlayerStunState(player, this);
        ConversationState = new ConversationState(player, this);

        TransitionTo(PlayerGroundedState);
    }

    /// <summary>
    /// 상태 전이
    /// </summary>
    public void TransitionTo(PlayerState nextState)
    {
        CurrentState?.Exit();
        CurrentState = nextState;
        nextState.Enter();
    }

    public void Update() => CurrentState?.Update();
}



