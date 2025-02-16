using UnityEngine;
using System;
using Unity.VisualScripting;
using static System.TimeZoneInfo;


[Serializable]
public class PlayerStateMachine : MonoBehaviour
{
    public IState currentState { get; private set; }
    public IState preState { get; private set; }

    public PlayerIdleState playerIdleState;  // ������ �ִ� ����
    public PlayerMoveState playerMoveState;  // �����̴� ����
    public PlayerUnControllableState playerUnControllableState; // ���� �Ұ� ����
    public PlayerAttackState playerAttackState;   // ���� ���� ����
    public PlayerGuardState playerGuardState;  // ���� ����
    public bool isTransitionPosible; //���� ���̰� �����Ѱ�?

    public PlayerStateMachine(PlayerController player)
    {
        this.playerMoveState = new PlayerMoveState(player);
        this.playerIdleState = new PlayerIdleState(player);
        this.playerUnControllableState = new PlayerUnControllableState(player);
        this.playerAttackState = new PlayerAttackState(player);
        this.playerGuardState = new PlayerGuardState(player);
    }


    // ���� �ʱ�ȭ
    public void Initialized(IState startingState) {
        currentState = startingState;
        isTransitionPosible = true;
        startingState.Enter();
    }
    // ���� ����
    public void TransitionTo(IState nextState)
    {
        if (!isTransitionPosible) { return; }
        currentState.Exit();
        preState = currentState;
        currentState = nextState;
        nextState.Enter();
    }



    //���� �Ұ� ���·� ����
    public void UnControllable()
    {
        TransitionTo(playerUnControllableState);
        isTransitionPosible = false;
    }

    //���� ���� ���·� ����
    public void Controllable()
    {
        isTransitionPosible = true;
        TransitionTo(playerIdleState);
    }

    //�Է� ���� �� �ִ� �����ΰ�?
    public bool IsControll() { return isTransitionPosible; }


    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
}



