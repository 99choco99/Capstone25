using UnityEngine;
public class PlayerSlideState : IState
{
    PlayerController player;

    public PlayerSlideState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        player.anim.SetTrigger("Slide");
    }

    public void Update()
    {
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("Slide")){
            player.rb.MovePosition(player.rb.position + (player.transform.forward * player.slideSpeed * Time.deltaTime));
        }
        else
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.preState);
        }
    }

    public void Exit()
    {
        player.anim.ResetTrigger("Slide");
        player.sprint = false;
    }
}


