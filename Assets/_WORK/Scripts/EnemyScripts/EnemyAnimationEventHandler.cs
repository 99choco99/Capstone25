using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimationEventHandler : MonoBehaviour
{
    private Enemy enemy;
    private Animator anim;
    private NavMeshAgent navAgent;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        anim = GetComponent<Animator>();
        navAgent = GetComponentInParent<NavMeshAgent>();
    }

    public void OnAnimationEnd() => enemy.StateMachine.CurrentState.OnAnimationEnd();

    public void OnAnimatorMove()
    {
        if (anim != null || enemy != null || navAgent != null)
        {
            if (enemy.StateMachine.CurrentState != null && enemy.StateMachine.CurrentState.UseRootMotion)
            {
                navAgent.Move(anim.deltaPosition);
                enemy.transform.rotation *= anim.deltaRotation;
            }
        }
    }
}
