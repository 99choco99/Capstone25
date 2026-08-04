using UnityEngine;
using UnityEngine.Playables;


[RequireComponent(typeof(EnemyAIController), typeof(EnemyMotor), typeof(EnemyStats))]
[RequireComponent(typeof(EnemyCombat), typeof(EnemySense), typeof(EnemyAttackObserver))]
public class Enemy : MonoBehaviour, ITargetable, ILockOnCameraProfileProvider
{
    public Transform TargetTransform => transform;

    [SerializeField] private Transform lockOnPoint;
    public Transform LockOnPoint => lockOnPoint;
    [SerializeField] private LockOnCameraProfile lockOnCameraProfile;
    public LockOnCameraProfile LockOnCameraProfile => lockOnCameraProfile;
    public bool IsDead => Stats.IsDead;

    [field: Header("Core Systems")]
    [field: SerializeField] public EnemyAIController AIController { get; private set; }
    [field: SerializeField] public EnemyMotor Motor { get; private set; }
    [field: SerializeField] public EnemyStats Stats { get; private set; }

    [field: Header("Combat")]
    [field: SerializeField] public EnemyCombat Combat { get; private set; }
    [field: SerializeField] public EnemySense Sense { get; private set; }
    [field: SerializeField] public EnemyAttackObserver AttackObserver { get; private set; }
    [field: SerializeField] public AnimationController AnimationController { get; private set; }

    public EnemyStateMachine StateMachine { get; private set; }

    /// <summary>
    /// 인살을 받을 수 있는 상태
    /// </summary>
    public bool IsDeathblowReady => StateMachine != null&& StateMachine.CurrentState is EnemyGroggyState;

    /// <summary>
    /// 인살을 받고 있는 상태
    /// </summary>
    public bool IsBeingExecuted => StateMachine != null && StateMachine.CurrentState is EnemyBeingExecuteState;

    [Header("인살 Timeline")]
    [Tooltip("정면 인살 Timeline")]
    [SerializeField] private PlayableAsset frontDeathblowTimeline;
    [Tooltip("후방 인살 Timeline")]
    [SerializeField] private PlayableAsset behindDeathblowTimeline;

    private void Awake()
    {
        AIController = GetComponent<EnemyAIController>();
        Motor = GetComponent<EnemyMotor>();
        Stats = GetComponent<EnemyStats>();
        Combat = GetComponent<EnemyCombat>();
        Sense = GetComponent<EnemySense>();
        AttackObserver = GetComponent<EnemyAttackObserver>();

        StateMachine = new EnemyStateMachine(this);
    }

    private void OnEnable()
    {
        Stats.OnDamage += HandleDamage;
        Stats.OnHealthDepleted += HandleHealthDepleted;
        Stats.OnPostureBroken += HandlePostureBroken;
        Stats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        Stats.OnDamage -= HandleDamage;
        Stats.OnHealthDepleted -= HandleHealthDepleted;
        Stats.OnPostureBroken -= HandlePostureBroken;
        Stats.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        StateMachine.Tick();
    }

    /// <summary>
    /// 피격시 호출되는 함수
    /// </summary>
    private void HandleDamage(DamageResult result)
    {
        if (!result.IsAccepted) return;

        if (Stats.IsDead || Stats.IsHealthDepleted || Stats.IsPostureBroken) return;

        StateMachine.CurrentState?.OnHit(result);
    }

    /// <summary>
    /// HP가 0이 됐을 때 호출되는 함수
    /// </summary>
    private void HandleHealthDepleted()
    {
        EnterGroggy();
    }

    /// <summary>
    /// 체간 붕괴시 호출되는 함수
    /// </summary>
    private void HandlePostureBroken()
    {
        EnterGroggy();
    }

    /// <summary>
    /// Groggy 전환
    /// </summary>
    private void EnterGroggy()
    {
        if (Stats.IsDead || StateMachine == null || IsBeingExecuted || StateMachine.CurrentState == StateMachine.EnemyGroggyState)
        {
            return;
        }

        StateMachine.TransitionTo(StateMachine.EnemyGroggyState);
    }

    /// <summary>
    /// 죽었을 때 호출되는 함수
    /// </summary>
    private void HandleDeath()
    {
        if (Player.LocalPlayer != null)
            Player.LocalPlayer.Stats.AddExp(Stats.ExpReward);

        bool killedByDeathblow = StateMachine.CurrentState is EnemyBeingExecuteState;
        StateMachine.EnemyDeadState.SetPlayDeathAnimation(!killedByDeathblow);

        StateMachine.TransitionTo(StateMachine.EnemyDeadState);
    }

    /// <summary>인살 접근 방향에 맞는 Timeline을 반환</summary>
    public PlayableAsset GetExecutionTimeline(DeathblowDirection approach)
    {
        return approach == DeathblowDirection.Front ? frontDeathblowTimeline : behindDeathblowTimeline;
    }

    /// <summary>
    /// 인살 대상으로 선점
    /// </summary>
    public bool SelectExecuted()
    {
        if (Stats.IsDead || StateMachine == null || IsBeingExecuted)
            return false;

        StateMachine.TransitionTo(StateMachine.EnemyBeingExecuteState);
        return IsBeingExecuted;
    }

}
