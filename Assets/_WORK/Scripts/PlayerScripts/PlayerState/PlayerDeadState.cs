using UnityEditor.Rendering;
using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    float timer = 0f;

    public override void Enter()
    {
        timer = 0f;

        player.Motor.SetMovement(Vector3.zero);
        player.Motor.StopKnockback();
        player.Combat.ForceResetAttackState();
        player.AnimatorController.PlayAction(AnimHash.Death);
    }


    public override void Update()
    {
        timer += Time.deltaTime;
        if(timer > 5f) { }
    }

}
