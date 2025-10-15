using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private IDamageable target;
    public override bool UseRootMotion => false;

    public override void Enter()
    {
        target = player.TargetingSystem.CurrentTarget;
        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }

        player.Motor.StopMovement();
        if (target.gameObject.GetComponent<Enemy>())
        {
            Debug.Log("¿ŒªÏ");
        }
        player.Combat.AttemptDeathblow(target.gameObject.GetComponent<Enemy>());
    }

    public void OnExecuteClimax()
    {
        if (target != null && !target.dead)
        {
            target.Die();
        }
    }

    public void AE_OnExecuteEnd()
    {
        stateMachine.TransitionTo(stateMachine.PlayerIdleState);
    }
}
