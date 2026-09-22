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
            Vector3 deltaPosition = Anim.deltaPosition;
            if (player.StateMachine.CurrentState is PlayerAttackState attack && attack.CurrentAttackData != null)
                deltaPosition = attack.CurrentAttackData.ScaleAttackAdvance(deltaPosition, player.transform.forward);
            player.Motor.ApplyRootMotion(deltaPosition, Anim.deltaRotation);

            if (player.StateMachine.CurrentState is PlayerAttackState attackState)
                attackState.RotateDuringWindUp();
        }
    }

}
