using UnityEngine;

/// <summary>
/// Animation Event와 Root Motion을 EnemyState 에 전달
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimationEventHandler : MonoBehaviour
{
    private Enemy enemy;
    private Animator anim;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        anim = GetComponent<Animator>();
    }

    /// <summary>Animation Event가 현재 State에 종료를 알림</summary>
    public void OnAnimationEnd()
    {
        if (enemy != null)
            enemy.StateMachine?.CurrentState?.OnAnimationEnd();
    }

    /// <summary>Root Motion에 대한 이동량을 NavMeshAgent에 반영</summary>
    public void OnAnimatorMove()
    {
        if (enemy == null) return;

        EnemyState currentState = enemy.StateMachine?.CurrentState;
        if (currentState == null || !currentState.UseRootMotion) return;

        Transform attackTarget = currentState is EnemyAttackState && Player.LocalPlayer != null ? Player.LocalPlayer.transform : null;
        Vector3 deltaPosition = anim.deltaPosition;
        if (currentState is EnemyAttackState attack && attack.CurrentAttackData != null)
            deltaPosition = attack.CurrentAttackData.ScaleAttackAdvance(deltaPosition, enemy.transform.forward);
        enemy.Motor.ApplyRootMotion(deltaPosition, anim.deltaRotation, attackTarget);
    }
}
