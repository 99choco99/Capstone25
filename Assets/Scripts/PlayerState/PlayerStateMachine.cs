using UnityEngine;
using System;


public class PlayerStateMachine : MonoBehaviour
{
    public IState CurrentState { get; private set; }  // 현재 상태
    public IState PreState { get; private set; }  // 이전 상태

    public PlayerMoveState playerMoveState;  // 움직임 상태
    public PlayerAttackState playerAttackState;   // 공격 중인 상태
    public PlayerGuardState playerGuardState;  // 가드 상태
    public PlayerSlideState playerSlideState;   // 슬라이드 상태
    public PlayerDamagedState playerDamagedState;   // 데미지를 입은 상태
    public PlayerDeadState playerDeadState;   // 죽은 상태
    public PlayerConversationState playerConversationState;  // 대화 상태


    private bool isTransitionPosible; //상태 전이가 가능한가?

    
    //플레이어 상태들
    private void Awake()
    {
        PlayerController player = GetComponent<PlayerController>();
    }

    // 상태 초기화
    public void Initialized(IState startingState) {
        CurrentState = startingState;
        isTransitionPosible = true;
        startingState.Enter();
    }


    // 상태 전이
    public void TransitionTo(IState nextState)
    {
        if (!isTransitionPosible || nextState == CurrentState) { return; }
        CurrentState.Exit();
        PreState = CurrentState;
        CurrentState = nextState;
        nextState.Enter();
    }

    //입력 받을 수 있는 상태인가?
    public bool IsControll() { return isTransitionPosible; }


    //상태 반복
    public void StateUpdate()
    {
        CurrentState?.Update();
    }
}



