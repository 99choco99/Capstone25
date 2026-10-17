using UnityEngine;

/// <summary>
/// Root Motion을 EnemyMotor 에 전달
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

    /// <summary>Root Motion에 대한 이동량을 NavMeshAgent에 반영</summary>
    public void OnAnimatorMove()
    {
        if (enemy == null)
            return;

        EnemyState currentState = enemy.StateMachine?.CurrentState;
        if (currentState == null || !currentState.UseRootMotion)
            return;

        Transform attackTarget = currentState is EnemyAttackState
            && Player.LocalPlayer != null
            ? Player.LocalPlayer.transform
            : null;
        enemy.Motor.ApplyRootMotion(anim.deltaPosition, anim.deltaRotation, attackTarget);
        if (currentState is EnemyAttackState attackState)
            attackState.UpdateAnimation();
    }
}
