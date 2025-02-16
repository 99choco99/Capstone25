using UnityEngine;

public class PlayerIdleState : IState
{
    public PlayerIdleState(PlayerController player) { }
    public void Enter() { Debug.Log("기본 상태"); }
    public void Update() { }
    public void Exit()
    {

    }
}
