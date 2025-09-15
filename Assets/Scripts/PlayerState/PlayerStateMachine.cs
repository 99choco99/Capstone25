using UnityEngine;
using System;


public class PlayerStateMachine : MonoBehaviour
{
    public State CurrentState { get; private set; }  // 현재 상태

    public PlayerIdleState PlayerIdleState { get; private set; }
    public PlayerMoveState PlayerMoveState { get; private set; }  // 움직임 상태
    public PlayerJumpState PlayerJumpState { get; private set; } //점프 상태
    public PlayerAttackState PlayerAttackState { get; private set; }   // 공격 중인 상태
    public PlayerGuardState PlayerGuardState { get; private set; }  // 가드 상태
    public PlayerSprintState PlayerSprintState { get; private set; }   // 슬라이드 상태
    public PlayerDamagedState PlayerDamagedState { get; private set; }   // 데미지를 입은 상태
    public PlayerExecuteState PlayerExecuteState { get; private set; }
    public PlayerDeadState PlayerDeadState { get; private set; }   // 죽은 상태
    public ConversationState ConversationState { get; private set; }  // 대화 상태


    Player player;

    
    //플레이어 상태들
    private void Awake()
    {
        player = GetComponent<Player>();

        PlayerIdleState = new PlayerIdleState(player, this);
        PlayerMoveState = new PlayerMoveState(player, this);
        PlayerJumpState = new PlayerJumpState(player, this);
        PlayerAttackState = new PlayerAttackState(player, this);
        PlayerGuardState = new PlayerGuardState(player, this);
        PlayerSprintState = new PlayerSprintState(player, this);
        PlayerDamagedState = new PlayerDamagedState(player, this);
        ConversationState = new ConversationState(player, this);
        PlayerDeadState = new PlayerDeadState(player, this);
        PlayerExecuteState = new PlayerExecuteState(player, this);
    }


    private void Start()
    {
        TransitionTo(PlayerIdleState);
    }

    // 상태 전이
    public void TransitionTo(State nextState)
    {
        CurrentState?.Exit();
        CurrentState = nextState;
        Debug.Log(CurrentState?.ToString());
        nextState.Enter();
    }

    private void Update() => CurrentState?.Update();
    private void FixedUpdate() => CurrentState?.FixedUpdate();

}



