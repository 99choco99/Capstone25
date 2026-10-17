using UnityEngine;

[System.Flags]
public enum EnemyAttackContext
{
    Neutral = 1,
    AfterGuard = 2,
    AfterDeflect = 4,
    AfterDeflectApproach = 8,
    AfterDeflectFollowup = 16
}

public enum EnemyAttackRole
{
    [InspectorName("연계 동작 / 미지정")] None,
    [InspectorName("단일 공격")] Single,
    [InspectorName("연속 공격")] Combo,
    [InspectorName("돌진 공격")] Rush,
    [InspectorName("위협 공격")] Perilous,
    [InspectorName("엇박자 공격")] Delayed,
    [InspectorName("후퇴 후 재진입")] Retreat,
    [InspectorName("빠른 반격")] Counter,
    [InspectorName("접근 동작")] Approach
}

public enum EnemyAttackFollowup
{
    Automatic,
    Observe,
    Recover
}

public enum EnemyAttackEndResponse
{
    [InspectorName("기존 공통 규칙")] Automatic = 0,
    [InspectorName("간격을 벌리지 않고 재판단")] Resume = 1,
    [InspectorName("압박 이어가기")] Pressure = 2,
    [InspectorName("공격권 돌려주기")] Yield = 3,
    [InspectorName("간격 확보 후 관찰")] Recover = 4
}

[CreateAssetMenu(fileName = "EnemyAttackData", menuName = "Scriptable Objects/EnemyAttackData")]
public class EnemyAttackData : AttackData
{
    [Header("다음 공격 데이터")]
    public EnemyAttackData NextComboAttack;
    public EnemyAttackData GuardComboAttack;
    public EnemyAttackData ParryComboAttack;
    [InspectorName("다음 공격 연결 프레임 (0 = 기존 후반 연결)"), Min(0f)]
    public float NextComboStartFrame;
    [InspectorName("빗나가면 연격 종료")] public bool RequireContactForCombo;

    [Header("마지막 타격의 결과별 공방")]
    [Tooltip("지정된 다음 연계가 없거나 이어가지 못했을 때만 적용합니다. 연계 중간에는 공격권을 나누지 않습니다.")]
    [InspectorName("명중 후")] public EnemyAttackEndResponse AfterHit;
    [InspectorName("일반 가드에 막힌 후")] public EnemyAttackEndResponse AfterGuard;
    [InspectorName("패링당한 후")] public EnemyAttackEndResponse AfterParry;
    [InspectorName("헛공격 후")] public EnemyAttackEndResponse AfterMiss;
    [Tooltip("공격권을 돌려주는 결과에만 적용합니다. 마지막 회수 시작부터 세며, 패링 반동은 접촉부터 셉니다. 플레이어의 현재 동작에 따라 늘리지 않습니다.")]
    [InspectorName("이 기술의 회수 기회 (초)"), Min(0f)] public float ResponseOpportunityDuration = 0.7f;

    [SerializeField, Min(0)] private int hitGroup;

    [InspectorName("준비 동작 중 피격 경직 저항")] public bool ResistWindUpHit;
    [Tooltip("0이면 기존 준비 경직 설정을 사용합니다. 양수이면 이 모션 프레임부터 준비 동작의 피격 경직을 버팁니다. 피해와 체간 피해, 패링 반동, 마지막 회수의 빈틈은 유지합니다.")]
    [InspectorName("준비 경직 저항 시작 프레임 (0 = 미사용)"), Min(0f)] public float WindUpResistanceStartFrame;

    [Header("재생 속도 곡선")]
    [Tooltip("X: 공격 진행도 0~1, Y: 기본 재생 속도에 곱할 값. 판정도 같은 속도로 진행합니다.")]
    public AnimationCurve playbackSpeed = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("AI 선택 조건")]
    [Tooltip("패턴 진입에 지정하는 특징. 연계의 두 번째 이후 동작은 미지정으로 두어 한 패턴을 중복 집계하지 않습니다.")]
    [InspectorName("패턴 특징"), SerializeField] private EnemyAttackRole patternRole;
    [InspectorName("공격 종료 후 행동")] public EnemyAttackFollowup Followup;
    [InspectorName("공격 시작 최소 거리"), SerializeField, Min(0f)] private float minDistance;
    [Tooltip("새 패턴을 시작할 최대 거리입니다. 0이면 기존 최대 사거리를 사용하며, 이미 시작한 연속공격의 연결 거리는 바꾸지 않습니다.")]
    [InspectorName("패턴 시작 최대 거리 (0 = 최대 사거리)"), SerializeField, Min(0f)] private float startMaxDistance;
    [InspectorName("거리 조정 목표 (0 = 범위 중앙)"), SerializeField, Min(0f)] private float preferredStartDistance;
    [Tooltip("최소/최대 시작 거리의 경계 여유. 목표 거리의 한 점에 도착하지 않아도 이 범위 안에서 공격합니다.")]
    [InspectorName("공격 시작 거리 여유"), SerializeField, Min(0f)] private float startDistanceTolerance;
    [Tooltip("선택 가중치")]
    [SerializeField, Range(0f, 100f)] private float weight = 50f;

    [Tooltip("패링 후 반격으로 선택할 수 있는 행동인지")]
    [SerializeField]
    private EnemyAttackContext selectionContexts = EnemyAttackContext.Neutral
        | EnemyAttackContext.AfterGuard
        | EnemyAttackContext.AfterDeflect;

    [Header("공격별 쿨다운")]
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;

    [Header("후퇴 루트모션")]
    [Tooltip("이 EnemyData의 후퇴 동작에 적용할 수평 이동량. 0이면 원본 이동을 사용합니다. 예약 공격의 준비로 사용할 때는 필요한 간격만큼 줄일 수 있습니다.")]
    [InspectorName("후퇴 이동량"), SerializeField, Min(0f)] private float retreatDistance;
    [Tooltip("현재 Override 모션의 연결 프레임까지 측정한 수평 후퇴량. 후퇴 이동량을 바꿔도 높이와 회전은 원본을 사용합니다.")]
    [InspectorName("후퇴 모션 기준 이동량"), SerializeField, Min(0f)] private float retreatMotionDistance;
    [Tooltip("독립 후퇴 패턴 뒤에 관찰할 시간. 공격 준비용 후퇴는 이 시간을 사용하지 않습니다.")]
    [InspectorName("후퇴 후 관찰 시간"), Min(0f)] public float RetreatObservationDuration;
    [InspectorName("후퇴 후 최대 관찰 시간 (0 = 고정 시간)"), Min(0f)] public float RetreatObservationMaximumDuration;
    [Tooltip("원본 모션의 전방 이동에만 적용합니다. 높이와 회전, 뼈의 자세는 유지합니다.")]
    [InspectorName("공격 전진 이동 배율"), Min(1f)] public float AdvanceMotionMultiplier = 1f;

    [Header("무기별 타격 구간")]
    [Tooltip("비워두면 기존 공통 판정창을 사용합니다. 각 구간은 대상당 한 번만 타격하며, 다른 구간은 독립적으로 가드·패링할 수 있습니다.")]
    public AttackHitWindow[] hitWindows = System.Array.Empty<AttackHitWindow>();
    public bool HasSeparateHitWindows => hitWindows != null && hitWindows.Length > 0;

    public override float ActiveStartFrame => TryGetHitWindowRange(out float start, out _)
        ? start
        : base.ActiveStartFrame;
    public override float RecoveryStartFrame => TryGetHitWindowRange(out _, out float end)
        ? end
        : base.RecoveryStartFrame;

    private bool TryGetHitWindowRange(out float start, out float end)
    {
        start = float.PositiveInfinity;
        end = float.NegativeInfinity;
        if (!HasHitWindow || !HasSeparateHitWindows)
            return false;

        foreach (AttackHitWindow window in hitWindows)
        {
            if (window == null || !(window.startFrame >= 0f && window.endFrame > window.startFrame)
                || float.IsInfinity(window.endFrame))
                continue;
            start = Mathf.Min(start, window.startFrame);
            end = Mathf.Max(end, window.endFrame);
        }

        return start < end;
    }

    [Header("점프 공격 연출 (0 = 착지 연출 없음)")]
    [Min(0f)] public float LandingImpactFrame;
    public string LandingSoundKey;
    public string LandingEffectKey;
    [Min(0f)] public float LandingShakeStrength;
    [Tooltip("락온 중 실제 몸의 상승 높이를 따라갑니다. 카메라 반경·수평 축·FOV는 바꾸지 않습니다.")]
    public bool TrackAnimatedBody;

    public int GetHitWindowIndex(float frame, WeaponSlot slot)
    {
        if (!HasHitWindow || frame < ActiveStartFrame || frame >= RecoveryStartFrame)
            return -1;
        if (!HasSeparateHitWindows)
            return 0;
        for (int i = 0; i < hitWindows.Length; i++)
            if (hitWindows[i] != null && hitWindows[i].Includes(frame, slot))
                return i;
        return -1;
    }

    public float SelectionWeight => weight;
    public EnemyAttackRole PatternRole => patternRole;
    public float MinDistance => minDistance;
    public float StartMinDistance => Mathf.Max(0f, MinDistance - startDistanceTolerance);
    private float MaximumStartDistance => startMaxDistance > 0f
        ? Mathf.Clamp(
        startMaxDistance,
        MinDistance,
        MaxDistance)
        : MaxDistance;
    public float StartMaxDistance => MaximumStartDistance + startDistanceTolerance;
    public float PreferredStartDistance => Mathf.Clamp(
        preferredStartDistance > 0f
        ? preferredStartDistance
        : (MinDistance + MaximumStartDistance) * 0.5f,
        MinDistance,
        MaximumStartDistance);
    public float Cooldown => attackCooldown;
    public int HitGroup => hitGroup;
    public float RetreatDistance => retreatDistance;
    public float RetreatMotionDistance => retreatMotionDistance;

    public EnemyAttackEndResponse GetEndResponse(EnemyAttackContact contact) => contact switch
    {
        EnemyAttackContact.Hit => AfterHit,
        EnemyAttackContact.Guarded => AfterGuard,
        EnemyAttackContact.Deflected => AfterParry,
        _ => AfterMiss
    };

    public bool SupportsContext(EnemyAttackContext context)
    {
        return (selectionContexts & context) != 0;
    }

    public float GetPlaybackSpeed(float normalizedTime)
    {
        return Mathf.Max(0.05f, playbackSpeed.Evaluate(Mathf.Clamp01(normalizedTime)));
    }

    public bool IsInStartRange(float distance)
    {
        return distance >= StartMinDistance && distance <= StartMaxDistance;
    }

    public float GetStartDistanceError(float distance) => distance - Mathf.Clamp(
        distance,
        StartMinDistance,
        StartMaxDistance);
}
