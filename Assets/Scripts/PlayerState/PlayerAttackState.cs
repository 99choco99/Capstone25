using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerAttackState : StateMachineBehaviour
{
    private PlayerController player;
    private const int MAX_ATTACK_ANIMATIONS = 5; // 사용할 공격 애니메이션의 총 개수

    private bool hasQueuedAttackInput = false;  //사용자 입력 버퍼
    private bool canCombo = true;               // 
    public float lastAttackTime;  // 마지막 공격 시간
    public int currentAttackIndex = 0; // 현재 재생할 공격 애니메이션의 인덱스
    public override void OnStateMachineEnter(UnityEngine.Animator animator, int stateMachinePathHash)
    {
        player.currentState = PlayerState.Attack;
        hasQueuedAttackInput = false;
        Attack();
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("PlayerController를 찾을 수 없습니다. Animator와 동일한 GameObject에 있는지 확인하세요.", animator.gameObject);
            }
        }
        player.currentState = PlayerState.Attack;
        if (!player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle"))
        {
            currentAttackIndex = (currentAttackIndex + 1) % MAX_ATTACK_ANIMATIONS;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 사용자 입력 버퍼링
        if (player.attack)
        {
            hasQueuedAttackInput = true;
            player.pressedTime += Time.deltaTime;
        }

        if(player.pressedTime >= 0.5f)
        {
            player.anim.SetTrigger("HeavyAttack");
            player.anim.SetBool("isMove", false);
            player.pressedTime = 0;
        }

        canCombo = true;
        // 공격 조건
        if (!player.attack && hasQueuedAttackInput && canCombo)
        {
            Attack();
        }

        //종료조건
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("AttackIdle")
            && player.anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f 
            && !player.attack 
            && !hasQueuedAttackInput)
        {
            player.currentState = PlayerState.Move;
        }
    }
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.attack = false;
        canCombo = true;
        hasQueuedAttackInput = false;
        currentAttackIndex = 0;
        player.anim.SetInteger("AttackCount", 0);
        player.anim.ResetTrigger("Attack");

    }

    void Attack()
    {
        player.anim.SetTrigger("Attack");
        player.anim.SetInteger("AttackCount", currentAttackIndex);
        player.anim.SetBool("isMove", false);
        hasQueuedAttackInput = false;
        canCombo = false;
        lastAttackTime = Time.time;
    }
}
