using UnityEngine;

public class PlayerAnimationEventHandler : MonoBehaviour
{
    Player player;
    Animator Anim;
    private void Awake()
    {
        player = GetComponentInParent<Player>();
        Anim = GetComponent<Animator>();
    }


    public void OnAnimationEnd() => player.StateMachine.CurrentState.OnAnimationEnd();

    private void OnAnimatorMove()
    {
        if (player.StateMachine.CurrentState != null && player.StateMachine.CurrentState.UseRootMotion)
        {
            player.Motor.ApplyRootMotion(Anim.deltaPosition, Anim.deltaRotation);
        }
    }
}
