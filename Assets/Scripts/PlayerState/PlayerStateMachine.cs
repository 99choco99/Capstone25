using UnityEngine;
using System;
using Unity.VisualScripting;
using static System.TimeZoneInfo;
using System.Collections;
using System.Threading.Tasks;


[Serializable]
public class PlayerStateMachine
{
    public IState currentState { get; private set; }  // 현재 상태
    public IState preState { get; private set; }  // 이전 상태

    public PlayerIdleState playerIdleState;  // 가만히 있는 상태
    public PlayerMoveState playerMoveState;  // 움직이는 상태
    public PlayerUnControllableState playerUnControllableState; // 제어 불가 상태
    public PlayerAttackState playerAttackState;   // 공격 중인 상태
    public PlayerGuardState playerGuardState;  // 가드 상태
    public PlayerSlideState playerSlideState;   // 슬라이드 상태
    public PlayerDamagedState playerDamagedState;

    public bool isTransitionPosible; //상태 전이가 가능한가?

    
    //플레이어 상태들
    public PlayerStateMachine(PlayerController player)
    {
        playerMoveState = new PlayerMoveState(player);
        playerIdleState = new PlayerIdleState(player);
        playerUnControllableState = new PlayerUnControllableState(player);
        playerAttackState = new PlayerAttackState(player);
        playerGuardState = new PlayerGuardState(player);
        playerSlideState = new PlayerSlideState(player);
        playerDamagedState = new PlayerDamagedState(player);
    }


    // 상태 초기화
    public void Initialized(IState startingState) {
        currentState = startingState;
        isTransitionPosible = true;
        startingState.Enter();
    }


    // 상태 전이
    public void TransitionTo(IState nextState)
    {
        if (!isTransitionPosible || nextState == currentState) { return; }
        currentState.Exit();
        preState = currentState;
        currentState = nextState;
        nextState.Enter();
    }

    //상태 전이 지연
    public async Task TransitionDelay(float time)
    {
        isTransitionPosible = false;
        await Task.Delay((int)(time * 1000)); // 밀리초 단위로 지연
        isTransitionPosible = true;
    }


    //입력 받을 수 있는 상태인가?
    public bool IsControll() { return isTransitionPosible; }


    //상태 반복
    public void Update()
    {
        if (currentState != null)
        {
            Debug.Log(currentState);
            currentState.Update();
        }
    }
}



