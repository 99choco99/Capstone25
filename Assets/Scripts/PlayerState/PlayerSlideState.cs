using UnityEngine;
public class PlayerSlideState : StateMachineBehaviour
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

        }
    }

    public void Exit()
    {
        player.anim.ResetTrigger("Dodge");
        player.sprint = false;
    }
}


