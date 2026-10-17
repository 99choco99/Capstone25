using UnityEngine;

/// <summary>
/// 일반공격은 가드·패링 가능, 특수공격은 패링만 가능
/// </summary>
public enum AttackType
{
    Normal = 0,
    Special = 1
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

public enum AttackPhase
{
    WindUp,
    Active,
    Recovery
}

public enum HitReactionType
{
    [InspectorName("일반 피격")] Normal,
    [InspectorName("다운")] Knockdown
}

public class AttackData : ScriptableObject
{
    public const float FrameRate = 60f;

    [Header("타격 정보")]
    [Tooltip("피해량. 플레이어 공격은 공격력 1 기준이며, 공격력 배율을 적용합니다.")]
    [Min(0f)]
    public float damage;
    [Tooltip("직접 피격 체간 피해량. 플레이어 공격은 공격력 1 기준입니다.")]
    [Min(0f)]
    public float postureDamage;

    [Header("방어 결과별 체간 피해")]
    [Tooltip("지정하면 공통 비율 대신 이 기술의 가드·패링 체간 피해를 사용합니다. 플레이어는 공격력 1 기준이며, 공격자 성장과 개별 타격 배율을 한 번씩 적용합니다.")]
    public bool OverrideDefensePostureDamage;
    [Min(0f)] public float GuardPostureDamage;
    [Min(0f)] public float ParryPostureDamage;
    [Tooltip("패링당한 공격자에게 쌓이는 기본 체간 피해입니다. 공격자의 레벨 보정 대신 패링한 플레이어의 공격력 배율을 적용합니다.")]
    [Min(0f)] public float DeflectedPostureDamage;

    [Header("피격 반응")]
    [Tooltip("밀림·경직·방어 반동의 기본 등급입니다. Heavy는 준비 동작이 분명한 묵직한 공격에 지정하며, 플레이어가 직접 맞으면 다운됩니다. None은 밀림 없이 Light 연출을 사용합니다.")]
    [InspectorName("피격 강도"), SerializeField] private KnockBackLevel impactLevel = KnockBackLevel.Light;
    public KnockBackLevel ImpactLevel => impactLevel;

    [Tooltip("미지정이면 피격 강도의 공통 설정을 사용합니다. 프리셋에서 선택한 항목만 덮어쓰며, 피해량·가드·패링 가능 여부와 등급별 재방어 대기는 바꾸지 않습니다.")]
    [InspectorName("반응 프리셋 (선택)"), SerializeField] private AttackImpactProfile impactProfile;
    public AttackImpactProfile ImpactProfile => impactProfile;

    [Header("공격 종류")]
    [Tooltip("Normal: 가드·패링 가능 / Special: 가드 불가·패링 가능")]
    [SerializeField] private AttackType type = AttackType.Normal;
    public AttackType Type => type;
    public bool CanGuard => Type == AttackType.Normal;

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

    public virtual float ActiveStartFrame => activeStartFrame;
    public virtual float RecoveryStartFrame => recoveryStartFrame;

    [SerializeField] private bool hasHitWindow = true;

    /// <summary>
    /// 공격 판정이 있는 동작인가
    /// </summary>
    public bool HasHitWindow => hasHitWindow;
}
