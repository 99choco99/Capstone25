using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationEventHandler : MonoBehaviour
{
    Player player;
    Animator Anim;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        Anim = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        if (player.StateMachine.CurrentState != null && player.StateMachine.CurrentState.UseRootMotion)
        {
            player.Motor.ApplyRootMotion(Anim.deltaPosition, Anim.deltaRotation);

            if (player.StateMachine.CurrentState is PlayerAttackState attackState)
            {
                attackState.UpdateAnimation();
                if (player.StateMachine.CurrentState == attackState)
                    attackState.RotateDuringWindUp();
            }
        }
    }
}
