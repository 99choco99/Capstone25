using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Animation Event와 Root Motion을 EnemyState 에 전달
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimationEventHandler : MonoBehaviour
{
    [Header("공격 Root Motion")]
    [Tooltip("공격 전진 중 플레이어 중심과 유지할 최소 거리. 긴 Root Motion이 플레이어를 관통하지 않게 합니다.")]
    [SerializeField, Min(0f)] private float minimumCombatDistance = 1.35f;

    private Enemy enemy;
    private Animator anim;
    private NavMeshAgent navAgent;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        anim = GetComponent<Animator>();
        navAgent = GetComponentInParent<NavMeshAgent>();
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
        if (enemy == null || navAgent == null) return;
        if (!navAgent.enabled || !navAgent.isOnNavMesh) return;

        EnemyState currentState = enemy.StateMachine?.CurrentState;
        if (currentState == null || !currentState.UseRootMotion) return;

        Vector3 deltaPosition = anim.deltaPosition;
        if (currentState is EnemyAttackState)
            deltaPosition = ClampAttackAdvance(deltaPosition);

        navAgent.Move(deltaPosition);
        enemy.transform.rotation *= anim.deltaRotation;
    }

    private Vector3 ClampAttackAdvance(Vector3 deltaPosition)
    {
        Player target = Player.LocalPlayer;
        if (target == null || minimumCombatDistance <= 0f)
            return deltaPosition;

        Vector3 toTarget = target.transform.position - enemy.transform.position;
        toTarget.y = 0f;
        float currentDistance = toTarget.magnitude;
        if (currentDistance < 0.0001f)
            return deltaPosition;

        Vector3 targetDirection = toTarget / currentDistance;
        float forwardDistance = Vector3.Dot(deltaPosition, targetDirection);
        if (forwardDistance <= 0f)
            return deltaPosition;

        float allowedForwardDistance = Mathf.Max(0f, currentDistance - minimumCombatDistance);
        if (forwardDistance <= allowedForwardDistance)
            return deltaPosition;

        // 회전·높이·횡이동은 보존하고, 타깃을 향해 파고드는 성분만 최소 간격까지 잘라냅니다.
        return deltaPosition
            - targetDirection * (forwardDistance - allowedForwardDistance);
    }
}
