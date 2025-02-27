using UnityEngine;
using System;
using Unity.VisualScripting;
using static System.TimeZoneInfo;
using System.Collections;
using System.Threading.Tasks;


[Serializable]
public class PlayerStateMachine
{
    public IState currentState { get; private set; }  // ���� ����
    public IState preState { get; private set; }  // ���� ����

    public PlayerIdleState playerIdleState;  // ������ �ִ� ����
    public PlayerMoveState playerMoveState;  // �����̴� ����
    public PlayerUnControllableState playerUnControllableState; // ���� �Ұ� ����
    public PlayerAttackState playerAttackState;   // ���� ���� ����
    public PlayerGuardState playerGuardState;  // ���� ����
    public PlayerSlideState playerSlideState;   // �����̵� ����
    public PlayerDamagedState playerDamagedState;

    public bool isTransitionPosible; //���� ���̰� �����Ѱ�?

    
    //�÷��̾� ���µ�
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


    // ���� �ʱ�ȭ
    public void Initialized(IState startingState) {
        currentState = startingState;
        isTransitionPosible = true;
        startingState.Enter();
    }


    // ���� ����
    public void TransitionTo(IState nextState)
    {
        if (!isTransitionPosible || nextState == currentState) { return; }
        currentState.Exit();
        preState = currentState;
        currentState = nextState;
        nextState.Enter();
    }

    //���� ���� ����
    public async Task TransitionDelay(float time)
    {
        isTransitionPosible = false;
        await Task.Delay((int)(time * 1000)); // �и��� ������ ����
        isTransitionPosible = true;
    }


    //�Է� ���� �� �ִ� �����ΰ�?
    public bool IsControll() { return isTransitionPosible; }


    //���� �ݺ�
    public void Update()
    {
        if (currentState != null)
        {
            Debug.Log(currentState);
            currentState.Update();
        }
    }
}



