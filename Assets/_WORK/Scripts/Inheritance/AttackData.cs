using UnityEngine;

/// <summary>
/// 일반공격, 특수공격 나누는 카테고리 특수공격은 방어 불가
/// </summary>
public enum AttackType
{
    Normal = 0,
    Special = 1,
    Thrust = 2
}

/// <summary>
/// 공격이 캐릭터에 미치는 넉백 등급
/// </summary>
public enum KnockBackLevel
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3
}

/// <summary>
/// 공격이 패링됐을 때 공격 애니메이션을 어떻게 이어갈지 결정
/// </summary>
public enum DeflectResponse
{
    Rebound,
    Continue
}

public enum AttackPhase { WindUp, Active, Recovery }

public enum AttackReactionType { Slash = 1, Pressure = 2, Finisher = 3 }

public class AttackData : ScriptableObject
{
    public const float FrameRate = 60f;

    [Header("타격 정보")]
    [Tooltip("피해량")]
    [Min(0f)]
    public float damage;
    [Tooltip("체간피해량")]
    [Min(0f)]
    public float postureDamage;

    [Header("피격 반응")]
    [Tooltip("공격의 넉백 등급")]
    [SerializeField] private KnockBackLevel impactLevel = KnockBackLevel.Light;
    public KnockBackLevel ImpactLevel => impactLevel;

    [InspectorName("접촉 반응"), SerializeField] private AttackReactionType reactionType = AttackReactionType.Slash;
    public AttackReactionType ReactionType => reactionType;

    [Header("공격 종류")]
    [SerializeField] private AttackType type = AttackType.Normal;
    public AttackType Type => type;

    [Header("패링당했을 때")]
    [SerializeField] private DeflectResponse deflectResponse = DeflectResponse.Rebound;
    public DeflectResponse DeflectResponse => deflectResponse;

    [Header("사거리 (0 = 미설정)")]
    [Tooltip("최대 거리")]
    [SerializeField, Min(0f)] private float maxDistance;
    public float MaxDistance => maxDistance;

    [Header("애니메이션")]
    public AnimationClip animationClip;
    public string animationStateName;
    public int AnimationHash => Animator.StringToHash(animationStateName);

    [Tooltip("애니메이션 재생 속도")]
    [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

    public float AnimationSpeed => Mathf.Max(0.01f, animationSpeed);

    /// <summary>60fps 기준 클립에 재생 속도를 적용한 초당 진행 프레임 수. 게임 FPS와는 별개입니다.</summary>
    public float FramesPerSecond => FrameRate * AnimationSpeed;

    [Header("타이밍 설정 (프레임 단위 입력)")]
    [Tooltip("애니메이션의 전체 프레임 수")]
    [Min(1f)]
    public float totalFrames = 60f;

    [Tooltip("타격 판정이 켜지는 프레임")]
    public float activeStartFrame = 15f;
    [Tooltip("Recovery 시작 프레임")]
    public float recoveryStartFrame = 45f;

    [SerializeField] private bool hasHitWindow = true;

    /// <summary>
    /// 공격 판정이 있는 동작인가
    /// </summary>
    public bool HasHitWindow => hasHitWindow;
}
