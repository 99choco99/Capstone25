using BehaviourTree;
using Unity.VisualScripting;
using UnityEngine;

public class TaskGoToTarget : Node
{
    private Transform _transform;
    private Rigidbody _rigid;
    private Animator _animator;

    public TaskGoToTarget(Transform transform)
    {
        _transform = transform;
        _rigid = transform.GetComponent<Rigidbody>();
        _animator = transform.GetComponent<Animator>();
    }

    public override NodeState Evaluate()
    {
        Transform target = (Transform)GetData("target");

        if(Vector3.Distance(_transform.position, target.position) > 6.0f)
        {
            Vector3 dir = (target.position - _transform.position).normalized;
            _rigid.MovePosition(_rigid.position + dir * Time.deltaTime * EnemyBT.speed * 2);
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
