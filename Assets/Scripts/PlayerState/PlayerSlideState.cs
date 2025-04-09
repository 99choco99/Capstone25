using UnityEngine;
public class PlayerSlideState : IState
{
    private readonly PlayerController player;

    public PlayerSlideState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        player.anim.SetTrigger("Dodge");
    }

    public void Update()
    {
        if (!player.anim.GetCurrentAnimatorStateInfo(0).IsName("Dodge")) {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.PreState);
        }
    }

    public void Exit()
    {
        player.anim.ResetTrigger("Dodge");
        player.sprint = false;
    }
}


