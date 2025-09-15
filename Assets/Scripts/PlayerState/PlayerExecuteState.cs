using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private IDamageable target;

    public override void Enter()
    {
        target = player.TargetingSystem.CurrentTarget;
        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }

        player.Motor.StopMovement();
        player.Anim.SetTrigger("Execute");

        //TODO : ø¨√‚
    }

    public void OnExecuteClimax()
    {
        if (target != null && !target.dead)
        {
            target.Die();
        }
    }

    public void OnExecuteEnd()
    {
        stateMachine.TransitionTo(stateMachine.PlayerIdleState);
    }
}
