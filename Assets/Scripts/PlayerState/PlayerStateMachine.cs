using UnityEngine;
using System;
using Unity.VisualScripting;
using static System.TimeZoneInfo;


[Serializable]
public class PlayerStateMachine : MonoBehaviour
{
    public IState currentState { get; private set; }
    public IState preState { get; private set; }

    public PlayerIdleState playerIdleState;  // 가만히 있는 상태
    public PlayerMoveState playerMoveState;  // 움직이는 상태
    public PlayerUnControllableState playerUnControllableState; // 제어 불가 상태
    public PlayerAttackState playerAttackState;   // 공격 중인 상태
    public PlayerGuardState playerGuardState;  // 가드 상태
    public bool isTransitionPosible; //상태 전이가 가능한가?

    public PlayerStateMachine(PlayerController player)
    {
        this.playerMoveState = new PlayerMoveState(player);
        this.playerIdleState = new PlayerIdleState(player);
        this.playerUnControllableState = new PlayerUnControllableState(player);
        this.playerAttackState = new PlayerAttackState(player);
        this.playerGuardState = new PlayerGuardState(player);
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
        if (!isTransitionPosible) { return; }
        currentState.Exit();
        preState = currentState;
        currentState = nextState;
        nextState.Enter();
    }



    //통제 불가 상태로 전이
    public void UnControllable()
    {
        TransitionTo(playerUnControllableState);
        isTransitionPosible = false;
    }

    //통제 가능 상태로 전이
    public void Controllable()
    {
        isTransitionPosible = true;
        TransitionTo(playerIdleState);
    }

    //입력 받을 수 있는 상태인가?
    public bool IsControll() { return isTransitionPosible; }


    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
}



