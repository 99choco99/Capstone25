using UnityEngine;

/// <summary>일반 적의 스폰 위치, 인지 범위와 전투 종료 후 복귀를 관리합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy), typeof(EnemySense))]
[DefaultExecutionOrder(50)]
public class EnemyTerritory : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float idleDetectionRadius = 16f;
    [SerializeField, Min(0.1f)] private float arrivalDistance = 0.4f;
    [SerializeField, Min(0f)] private float reacquireDelay = 1f;
    [SerializeField, Min(0.5f)] private float returnStallTimeout = 3f;

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }
    public float ArrivalDistance => arrivalDistance;
    public float ReturnStallTimeout => returnStallTimeout;
    public bool IsReturning { get; private set; }

    private Enemy enemy;
    private bool hasSpawnPosition;
    private bool wasEngaged;
    private float canAcquireAt;

    public void Configure(Pose spawn, float detectionDistance)
    {
        SpawnPosition = spawn.position;
        SpawnRotation = spawn.rotation;
        hasSpawnPosition = true;
        idleDetectionRadius = Mathf.Max(0.1f, detectionDistance);
        GetComponent<EnemySense>().SetTerritory(this);
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        if (!hasSpawnPosition)
            Configure(new Pose(transform.position, transform.rotation), idleDetectionRadius);
        else
            GetComponent<EnemySense>().SetTerritory(this);

        wasEngaged = false;
        canAcquireAt = Time.time;
    }

    private void OnDisable()
    {
        CancelReturn();
        wasEngaged = false;
    }

    private void Update()
    {
        if (enemy.Stats.IsDead || enemy.IsBeingExecuted || enemy.StateMachine.CurrentState == null || IsReturning)
            return;

        if (enemy.Sense.IsAlerted)
            wasEngaged = true;
        if (!wasEngaged)
            return;

        // 시야를 잃은 뒤 EnemySense의 기억 시간이 끝났을 때만 복귀합니다.
        if (!enemy.Sense.IsAlerted)
            TryBeginReturn();
    }

    internal bool TryBeginReturn()
    {
        if (IsReturning || enemy.Stats.IsDead || enemy.Stats.IsHealthDepleted || enemy.Stats.IsPostureBroken
            || enemy.IsBeingExecuted || enemy.StateMachine.CurrentState == null || enemy.StateMachine.CurrentState is EnemyHitState)
            return false;

        IsReturning = true;
        wasEngaged = true;
        enemy.Sense.ForgetTarget();
        enemy.StateMachine.TransitionTo(enemy.StateMachine.EnemyReturnState);
        return true;
    }

    internal bool CanTrackTarget(Vector3 position, bool alreadyAlerted)
    {
        return !IsReturning && Time.time >= canAcquireAt
            && (alreadyAlerted || wasEngaged || !IsOutside(position, idleDetectionRadius));
    }

    internal void CompleteReturn()
    {
        if (!IsReturning)
            return;

        enemy.Stats.RestoreHealthAndPosture();
        enemy.Stats.ReleasePlayerLevel();
        CancelReturn();
        wasEngaged = false;
        canAcquireAt = Time.time + reacquireDelay;
        enemy.Sense.ForgetTarget();
    }

    internal void CancelReturn()
    {
        if (!IsReturning)
            return;
        IsReturning = false;
    }

    private bool IsOutside(Vector3 position, float radius)
    {
        Vector3 offset = position - SpawnPosition;
        offset.y = 0f;
        return offset.sqrMagnitude > radius * radius;
    }
}
