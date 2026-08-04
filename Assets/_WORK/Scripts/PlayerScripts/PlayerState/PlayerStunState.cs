using UnityEngine;

public class PlayerStunState : PlayerState
{
    public PlayerStunState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private float stateTimer = 0f;
    private float stunTime = 1.2f;

    public override void Enter()
    {
        stateTimer = 0f;
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();
        player.AnimatorController.PlayAction(AnimHash.Stun);
        
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if(stateTimer >= stunTime)
        {
            //일어나는 애니메이션 재생
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
        player.Stats.ResetPosture();
    }

}
