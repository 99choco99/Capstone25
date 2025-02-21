using UnityEngine;

public class PlayerGuardState : IState
{
    PlayerController player;
    public PlayerGuardState(PlayerController player) { this.player = player; }
    public void Enter() { }
    public void Update() { }
    public void Exit() { }
}
