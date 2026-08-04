using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;


/// <summary>
/// 인살 시작 방향
/// </summary>
public enum DeathblowDirection
{
    Front,
    Behind
}

/// <summary>
/// 현재의 인살을 실행하기 위한 정보를 담는 구조체
/// <para>인살에 대한 계획</para>
/// </summary>
public readonly struct DeathblowPlan
{
    public Enemy Target { get; }
    public TimelineAsset Timeline { get; }
    public DeathblowDirection Direction { get; }  //정면 or 후방 인살인지
    public Pose PlayerPose { get; }
    public Quaternion VictimRotation { get; }

    public bool IsValid => Target != null && Timeline != null;

    public DeathblowPlan(Enemy target, TimelineAsset timeline, DeathblowDirection direction, Pose playerPose, Quaternion victimRotation)
    {
        Target = target;
        Timeline = timeline;
        Direction = direction;
        PlayerPose = playerPose;
        VictimRotation = victimRotation;
    }
}


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayableDirector))]
public class PlayerExecution : MonoBehaviour
{
    private const string AttackerTrackName = "AttackerAnimation";
    private const string VictimTrackName = "VictimAnimation";

    [Header("인살 가능 조건")]
    [Tooltip("인살이 가능한 최대 거리")]
    [SerializeField, Min(0.1f)] private float maxExecutionDistance = 2.2f;
    [Tooltip("후방 인살 판정 각도")]
    [SerializeField, Range(-1f, 0f)] private float BehindThreshold = -0.2f;

    [Header("연출 시작 위치")]
    [Tooltip("정면 인살시 플레이어가 적 앞에 서는 거리")]
    [SerializeField, Min(0.1f)] private float frontAlignDistance = 1.0f;
    [Tooltip("후방 인살시 플레이어가 적 뒤에 서는 거리")]
    [SerializeField, Min(0.1f)] private float behindAlignDistance = 0.9f;
    [Tooltip("현재 위치에서 인살 시작 위치까지 보간하는 시간")]
    [SerializeField, Min(0f)] private float alignDuration = 0.12f;

    [Header("인살 공간 검사")]
    [Tooltip("플레이어와 적 사이에 있으면 안 되는 레이어")]
    [SerializeField] private LayerMask CollisionLayers = Physics.DefaultRaycastLayers;
    [Tooltip("인살 시작 위치의 바닥을 찾을 때 사용하는 레이어")]
    [SerializeField] private LayerMask GroundLayers = Physics.DefaultRaycastLayers;
    [Tooltip("바닥을 찾는 최대 거리")]
    [SerializeField, Min(0.1f)] private float groundDistance = 2f;

    public PlayableDirector DeathblowDirector { get; private set; }
    public bool IsExecuting{ get; private set; }

    // 카메라가 정렬 전 현재 위치가 아니라 최종 연출 축을 기준으로 샷을 잡을 수 있도록
    // 실행 대상과 최종 Pose를 함께 전달한다.
    public event Action<DeathblowPlan> OnExecuteStart;
    public event Action<TimelineAsset> OnExecuteTimelineReady;
    public event Action OnExecuteEnd;

    private readonly Collider[] overlapBuffer = new Collider[24];
    private readonly RaycastHit[] raycastBuffer = new RaycastHit[24];

    private Player player;
    private Enemy enemy;
    private CharacterController characterController;
    private Coroutine alignRoutine;


    private void Awake()
    {
        player = GetComponent<Player>();
        characterController = GetComponent<CharacterController>();
        DeathblowDirector = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        DeathblowDirector.stopped += HandleDirectorStopped;
    }

    private void OnDisable()
    {
        DeathblowDirector.stopped -= HandleDirectorStopped;

        if (IsExecuting)
            CompleteDeathblow();
    }

    //==================== 인살 검사용 코드들 ============================


    /// <summary>
    /// 인살이 가능한지 검사
    /// <para>UI와 입력이 함께 사용</para>
    /// true를 반환할 때만 붉은 인살 마커가 나타남
    /// </summary>
    public bool CreateDeathblowPlan(Enemy enemy, out DeathblowPlan plan)
    {
        plan = default;

        if (IsExecuting || DeathblowDirector.state == PlayState.Playing) return false;
        if (enemy == null || enemy.IsDead || enemy.IsBeingExecuted) return false;


        //거리 체크
        Vector3 enemyToPlayer = transform.position - enemy.transform.position;
        enemyToPlayer.y = 0f;
        float sqrDistance = enemyToPlayer.sqrMagnitude;
        float maxSqrDistance = maxExecutionDistance * maxExecutionDistance;

        if (sqrDistance > maxSqrDistance || sqrDistance < 0.0001f)
            return false;

        //앞 인살인지 뒤 인살인지 판정
        Vector3 enemyForward = FlattenDirection(enemy.transform.forward);
        float sideDot = Vector3.Dot(enemyForward, enemyToPlayer.normalized);

        bool FrontDeathblow = enemy.IsDeathblowReady;
        bool BehindDeathBlow = !FrontDeathblow && !enemy.Sense.IsAlerted && sideDot <= BehindThreshold;

        if (!FrontDeathblow && !BehindDeathBlow) return false;
        DeathblowDirection direction = FrontDeathblow && sideDot >= 0f ? DeathblowDirection.Front : DeathblowDirection.Behind;
        if (enemy.GetExecutionTimeline(direction) is not TimelineAsset timeline) return false;

        //위치 정렬 계산
        if (!CalcAlignPose(enemy, direction, out Pose playerPose, out Quaternion victimRotation))
        {
            return false;
        }

        if (!HasClearPath(enemy)) return false;
        if (!HasSpace(playerPose.position, enemy)) return false;

        plan = new DeathblowPlan(enemy, timeline, direction, playerPose, victimRotation);
        return true;
    }

    ///<summary> 정렬할 좌표 계산. 계산만 함</summary>
    private bool CalcAlignPose(Enemy enemy, DeathblowDirection direction, out Pose playerPose, out Quaternion victimRotation)
    {
        Vector3 enemyForward = FlattenDirection(enemy.transform.forward);
        float offset = direction == DeathblowDirection.Front ? frontAlignDistance : -behindAlignDistance;

        playerPose.position = enemy.transform.position + enemyForward * offset;
        playerPose.position.y = transform.position.y;

        //땅 위치 찾기, 인살 시 공중부양 방지
        if (!GetGroundPosition(playerPose.position, enemy, out playerPose.position))
        {
            playerPose.rotation = Quaternion.identity;
            victimRotation = Quaternion.identity;
            return false;
        }

        //회전
        Vector3 playerToEnemy = FlattenDirection(enemy.transform.position - playerPose.position);
        playerPose.rotation = Quaternion.LookRotation(playerToEnemy, Vector3.up);
        victimRotation = Quaternion.LookRotation(enemyForward, Vector3.up);
        return true;
    }


    ///<summary> 땅 좌표값 구하기</summary>
    private bool GetGroundPosition(Vector3 playerPosition, Enemy enemy, out Vector3 groundPosition)
    {
        groundPosition = playerPosition;
        Vector3 origin = playerPosition + Vector3.up;
        float castDistance = 1f + groundDistance;

        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, raycastBuffer, castDistance, GroundLayers, QueryTriggerInteraction.Ignore);
        

        //가장 가까운 땅 찾기
        float nearestDistance = float.PositiveInfinity;
        bool foundGround = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastBuffer[i];
            if (hit.collider == null || !IsObstacle(hit.collider, enemy)) continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > characterController.slopeLimit) continue;
            if (hit.distance >= nearestDistance) continue;

            nearestDistance = hit.distance;
            groundPosition.y = hit.point.y;
            foundGround = true;
        }

        return foundGround;
    }

    /// <summary>
    /// 인살 시 앞에 장애물이 있는지 확인
    /// </summary>
    private bool HasClearPath(Enemy enemy)
    {
        Vector3 start = transform.TransformPoint(characterController.center);
        Vector3 dir = enemy.LockOnPoint.position - start;
        float distance = dir.magnitude;
        if (distance < 0.0001f) return true;

        int hitCount = Physics.RaycastNonAlloc(start, dir / distance, raycastBuffer, distance,  CollisionLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = raycastBuffer[i].collider;
            if (hit != null && IsObstacle(hit, enemy))
                return false;
        }

        return hitCount < raycastBuffer.Length;
    }

    /// <summary>
    /// 인살 시 플레이어가 들어갈 공간이 있는지 검사
    /// </summary>
    private bool HasSpace(Vector3 position, Enemy enemy)
    {
        float radius = characterController.radius;
        float height = characterController.height;
        Vector3 center = characterController.center;

        radius = Mathf.Max(0.05f, radius * 0.9f);
        height = Mathf.Max(height, radius * 2f);

        Vector3 worldCenter = position + center;
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 bottom = worldCenter - Vector3.up * halfSegment;
        Vector3 top = worldCenter + Vector3.up * halfSegment;

        int overlapCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, CollisionLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = overlapBuffer[i];
            if (overlap != null && IsObstacle(overlap, enemy))
                return false;
        }

        return overlapCount < overlapBuffer.Length;
    }

    /// <summary>
    /// Collider가 장애물인지 아닌지 검사
    /// </summary>
    private bool IsObstacle(Collider collider, Enemy enemy)
    {
        Transform owner = collider.transform.root;

        if (owner == transform.root) return false;
        if (enemy != null && owner == enemy.transform.root) return false;

        return true;
    }

    /// <summary>
    /// 방향을 2차원으로 정규화, 두번째 인수는 기본값
    /// </summary>
    private static Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
    }


    //==================== 인살 시작 코드들 ============================

    /// <summary>
    /// 공격 입력으로 인살을 시작
    /// </summary>
    public bool StartDeathblow(in DeathblowPlan requestedPlan)
    {
        if (!requestedPlan.IsValid || IsExecuting) return false;

        // 판정 당시의 위치가 아닌 현재 프레임의 위치와 상태로 갱신
        if (!CreateDeathblowPlan(requestedPlan.Target, out DeathblowPlan newPlan))
            return false;

        if (!newPlan.Target.SelectExecuted()) return false;

        enemy = newPlan.Target;
        IsExecuting = true;

        OnExecuteStart?.Invoke(newPlan);
        alignRoutine = StartCoroutine(AlignAndPlayTimeline(newPlan));
        return true;
    }

    /// <summary>
    /// 플레이어와 적을 각 Timeline이 기대하는 상대 위치로 짧게 보간뒤 재생
    /// </summary>
    private IEnumerator AlignAndPlayTimeline(DeathblowPlan plan)
    {
        transform.GetPositionAndRotation(out Vector3 startPlayerPosition, out Quaternion startPlayerRotation);
        Quaternion startEnemyRotation = plan.Target.transform.rotation;

        //타임라인 위치로 정렬
        float elapsed = 0f;
        while (elapsed < alignDuration)
        {
            if (enemy == null)
            {
                CompleteDeathblow();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = alignDuration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / alignDuration));

            player.Motor.SetTransform(
                Vector3.Lerp(startPlayerPosition, plan.PlayerPose.position, t),
                Quaternion.Slerp(startPlayerRotation, plan.PlayerPose.rotation, t)
            );
            enemy.transform.rotation = Quaternion.Slerp(startEnemyRotation, plan.VictimRotation, t);

            yield return null;
        }

        //최종 오차 보정
        player.Motor.SetTransform(plan.PlayerPose.position, plan.PlayerPose.rotation);
        enemy.transform.rotation = plan.VictimRotation;


        //타임라인 장착 후 실행
        DeathblowDirector.playableAsset = plan.Timeline;
        if (!BindAnimationTracks(plan.Timeline, plan.Target))
        {
            CompleteDeathblow();
            yield break;
        }

        OnExecuteTimelineReady?.Invoke(plan.Timeline);

        //인살 실행
        alignRoutine = null;
        DeathblowDirector.Play();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    /// <summary>
    /// 타임라인 애니메이션 바인드
    /// </summary>
    private bool BindAnimationTracks(TimelineAsset timeline, Enemy enemy)
    {
        if (timeline == null) return false;

        AnimationTrack attackerTrack = null;
        AnimationTrack victimTrack = null;

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is not AnimationTrack animationTrack) continue;

            if (track.name == AttackerTrackName)
                attackerTrack = animationTrack;
            else if (track.name == VictimTrackName)
                victimTrack = animationTrack;
        }
        if (attackerTrack == null || victimTrack == null) return false;

        Animator attackerAnimator = player.AnimatorController.Animator;
        Animator victimAnimator = enemy.AnimationController.Animator;


        DeathblowDirector.SetGenericBinding(attackerTrack, attackerAnimator);
        DeathblowDirector.SetGenericBinding(victimTrack, victimAnimator);
        return true;
    }

    //==================== 인살 마무리 코드들 ============================

    /// <summary>
    /// Timeline 끝나면 실행하는 
    /// </summary>
    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (director == DeathblowDirector)
            CompleteDeathblow();
    }

    /// <summary>
    /// 인살 후 양쪽 상태 머신을 복귀
    /// </summary>
    private void CompleteDeathblow()
    {
        if (!IsExecuting) return;
        IsExecuting = false;

        if (alignRoutine != null)
        {
            StopCoroutine(alignRoutine);
            alignRoutine = null;
        }

        Enemy completedTarget = enemy;
        enemy = null;

        if (completedTarget != null)
            completedTarget.StateMachine.EnemyBeingExecuteState.ExecutionFinished();

        OnExecuteEnd?.Invoke();
    }
}
