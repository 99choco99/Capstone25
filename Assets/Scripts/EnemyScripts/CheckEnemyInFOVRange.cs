using BehaviourTree;
using UnityEngine;

public class CheckEnemyInFOVRange: Node
{
    private Transform _transform;
    private Animator _animator;
    private static int _LayerMask = (1 << 7);
    public CheckEnemyInFOVRange(Transform transform)
    {
        _transform = transform;
        _animator = transform.GetComponent<Animator>();
    }

    public override NodeState Evaluate()
    {
        Collider[] collider = Physics.OverlapSphere(_transform.position, EnemyBT.fovRange, _LayerMask);

        if (collider.Length > 0)
        {
            parent.parent.SetData("target", collider[0].transform);
            state = NodeState.Success;
            return state;
        }
        else
        {
            ClearData("target");
            _animator.SetBool("Chase", false);
            state = NodeState.Failure;
            return state;
        }
    }
}
