using UnityEngine;

public class PlayerIdleState : IState
{
    public PlayerIdleState(PlayerController player) { }
    public void Enter() { Debug.Log("�⺻ ����"); }
    public void Update() { }
    public void Exit()
    {

    }
}
