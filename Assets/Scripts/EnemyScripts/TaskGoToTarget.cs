using BehaviourTree;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class TaskGoToTarget : Node
{
    private Transform _transform;
    private Rigidbody _rigid;
    private Animator _animator;
    private NavMeshAgent _agent;


    public TaskGoToTarget(Transform transform)
    {
        _transform = transform;
        _rigid = transform.GetComponent<Rigidbody>();
        _agent = transform.GetComponent<NavMeshAgent>();
        _animator = transform.GetComponent<Animator>();

    }

    public override NodeState Evaluate()
    {
        Transform target = (Transform)GetData("target");
        if(Vector3.Distance(_transform.position, target.position) > 6.0f)
        {
            _agent.SetDestination(target.position);
            _animator.SetBool("Chase", true);
            _transform.LookAt(target.position);
        }
        else
        {
            _animator.SetBool("Chase", false);
        }
        state = NodeState.Running;
        return state;
    }
}
