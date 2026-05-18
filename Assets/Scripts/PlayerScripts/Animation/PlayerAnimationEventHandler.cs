using UnityEngine;

public class PlayerAnimationEventHandler : MonoBehaviour
{
    Player player;
    private void Awake() => player = GetComponentInParent<Player>();

    public void OnAnimationEnd() => player.StateMachine.CurrentState.OnAnimationEnd();

    public void OnParryWindowOpen() { }
    public void OnParryWindowClose() { }


    //AttackPhase : WindUp   -> Active
    public void OnAnimationPlayerAttackStart()
    {
        if (player.Combat != null)
            player.Combat.OnAnimationPlayerAttackStart();
        if (player.StateMachine.CurrentState is PlayerAttackState attackState)
            attackState.OnAttackActiveStart();
    }

    //AttackPhase: Active -> Recovery
    public void OnAnimationPlayerAttackEnd()
    {
        if (player.Combat != null)
            player.Combat.OnAnimationPlayerAttackEnd();
    }

    //AttackPhase == Recovery
    public void OnComboWindowOpen()
    {
        if (player.StateMachine.CurrentState is PlayerAttackState attackState)
        {
            attackState.OnCheckNextAttack();
        }
    }

    private void OnAnimatorMove()
    {
        if (player.StateMachine.CurrentState != null && player.StateMachine.CurrentState.UseRootMotion)
        {
            player.Motor.controller.Move(player.Anim.deltaPosition);
            transform.rotation *= player.Anim.deltaRotation;
        }
    }
}
