using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerAttackState : StateMachineBehaviour
{
    private PlayerController player;
    private const int MAX_ATTACK_ANIMATIONS = 5; // 사용할 공격 애니메이션의 총 개수

    private bool hasQueuedAttackInput = false;  //사용자 입력 버퍼

    public int currentAttackIndex = -1; // 현재 재생할 공격 애니메이션의 인덱스

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.attack = false;
        hasQueuedAttackInput = false;
        player.anim.ResetTrigger("Attack");
        player.anim.ResetTrigger("HeavyAttack");
        currentAttackIndex = -1;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Attack;
        player.playerBehaviour.canMove = false;
        currentAttackIndex = (currentAttackIndex + 1) % MAX_ATTACK_ANIMATIONS;
        if(currentAttackIndex < 0) { currentAttackIndex = 0; }
        player.playerSetting.currentAttack = player.playerSetting.playerNormalAttack[currentAttackIndex];
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 사용자 입력 버퍼링
        if (player.attack)
        {
            hasQueuedAttackInput = true;
        }
        if (player.isAttackPress)
        {
            player.pressedTime += Time.deltaTime;
        }

        if(player.pressedTime >= 0.5f)
        {
            player.anim.SetTrigger("HeavyAttack");
            player.anim.SetBool("isMove", false);
            player.pressedTime = 0;
        }
        // 공격 조건
        if (hasQueuedAttackInput)
        {
            Attack();
        }
    }
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.anim.ResetTrigger("Attack");
        player.anim.ResetTrigger("HeavyAttack");
        player.currentState = PlayerState.Move;
    }

    void Attack()
    {
        player.anim.SetBool("isMove", false);
        player.attack = false;
        hasQueuedAttackInput = false;
        player.anim.SetTrigger("Attack");
        player.currentState = PlayerState.Attack;
        player.anim.ResetTrigger("HeavyAttack");
    }
}
