using UnityEngine;

using BehaviourTree;


public class TaskPatrol : Node
{
    public Transform _transform;
    public Transform[] _wayPoints;

    private Animator _animator;

    private int _currentWaypointIndex = 0;

    private float _waitTime = 2f;
    private float _waitCounter = 0f;
    private bool _waiting = false;
    public TaskPatrol(Transform transform, Transform[] wayPoints)
    {
        _animator = transform.GetComponent<Animator>();
        _transform = transform;
        _wayPoints = wayPoints;
    }

    public override NodeState Evaluate()
    {
        if (_waiting)
        {
            _waitCounter += Time.deltaTime;
            if (_waitCounter < _waitTime)
            {
                _waiting = false;
                _animator.SetBool("Move", false);
            }
        }
        else
        {
            Transform wp = _wayPoints[_currentWaypointIndex];
            if (Vector3.Distance(_transform.position, wp.position) < 0.1f)
            {
                _transform.position = wp.position;
                _waitCounter = 0f;
                _waiting = true;

                _currentWaypointIndex = (_currentWaypointIndex + 1) % _wayPoints.Length;
            }
            else
            {
                _transform.position = Vector3.MoveTowards(_transform.position, wp.position, EnemyBT.speed * Time.deltaTime);
                _transform.LookAt(wp.position);
                _animator.SetBool("Move", true);
            }
        }

        state = NodeState.Running;
        return state;
    }
}
