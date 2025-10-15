using UnityEngine;

public class AttackSMB : StateMachineBehaviour
{
    private Weapon weapon;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon == null)
        {
            weapon = animator.GetComponentInChildren<Weapon>();
        }

        if (weapon != null)
        {
            weapon.EnableWeaponCollider();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon != null)
        {
            weapon.DisableWeaponCollider();
        }
    }
}