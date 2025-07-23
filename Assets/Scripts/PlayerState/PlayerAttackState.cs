using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerAttackState : StateMachineBehaviour
{
    private PlayerController player;
    private const int MAX_ATTACK_ANIMATIONS = 4; // 사용할 공격 애니메이션의 총 개수

    private bool hasQueuedAttackInput = false;  //사용자 입력 버퍼
    public float lastAttackTime;  // 마지막 공격 시간
    public int currentAttackIndex = -1; // 현재 재생할 공격 애니메이션의 인덱스
    public override void OnStateMachineEnter(UnityEngine.Animator animator, int stateMachinePathHash)
    {
        Attack();
        player.currentState = PlayerState.Attack;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Attack;
        if (!player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle"))
        {
            currentAttackIndex = (currentAttackIndex + 1) % MAX_ATTACK_ANIMATIONS;
            player.anim.SetInteger("AttackCount", currentAttackIndex);
            player.playerSetting.currentAttack = player.playerSetting.playerNormalAttack[currentAttackIndex];
        }
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

        //종료조건
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle")
            && Time.time >= lastAttackTime + 1.0f
            && !player.attack
            && !hasQueuedAttackInput)
        {
            player.currentState = PlayerState.Move;
        }

        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle") && player.jump)
        {
            player.currentState = PlayerState.Move;
        }
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle") && player.guard)
        {
            player.currentState = PlayerState.Guard;
            player.anim.SetBool("Guard", true);
        }
    }
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.attack = false;
        hasQueuedAttackInput = false;
        currentAttackIndex = -1;
        player.anim.SetInteger("AttackCount", 0);
        player.anim.ResetTrigger("Attack");
        player.anim.ResetTrigger("HeavyAttack");

    }

    void Attack()
    {
        player.currentState = PlayerState.Attack;
        player.anim.SetTrigger("Attack");
        player.anim.SetBool("isMove", false);
        hasQueuedAttackInput = false;
        player.anim.ResetTrigger("HeavyAttack");
        player.attack = false;
        lastAttackTime = Time.time;
    }
}
