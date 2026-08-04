using UnityEngine;


/// <summary>
/// 일반공격, 특수공격 나누는 카테고리 특수공격은 방어 불가
/// </summary>
public enum AttackType
{
    Normal,
    Special
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

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
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

    [Header("공격 종류")]
    [SerializeField] private AttackType type = AttackType.Normal;
    public AttackType Type => type;

    [Header("패링당했을 때")]
    [SerializeField] private DeflectResponse deflectResponse = DeflectResponse.Rebound;
    public DeflectResponse DeflectResponse => deflectResponse;

    [Header("다음 공격 데이터")]
    public AttackData NextComboAttack;


    [Header("애니메이션")]
    public string animationStateName;
    public int AnimationHash => Animator.StringToHash(animationStateName);

    [Tooltip("애니메이션 재생 속도")]
    [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

    public float AnimationSpeed => Mathf.Max(0.01f, animationSpeed);


    [Header("타이밍 설정 (프레임 단위 입력)")]
    [Tooltip("애니메이션의 전체 프레임 수")]
    public float totalFrames = 60f;
    [Tooltip("공격 방향과 실행을 확정하는 프레임")]
    public float commitFrame = 10f;
    [Tooltip("타격 판정이 켜지는 프레임")]
    public float activeStartFrame = 15f;
    [Tooltip("Recovery 시작 프레임")]
    public float recoveryStartFrame = 45f;
    [Tooltip("다음 공격으로 전환할 수 있는 프레임")]
    public float comboStartFrame = 50f;

    [Tooltip("피해 판정이 전혀 없는 동작이면 false")]
    [SerializeField] private bool hasHitWindow = true;


    /// <summary>
    /// 공격 판정이 있는 동작인가
    /// </summary>
    public bool HasHitWindow => hasHitWindow;
    public float CommitTime => totalFrames > 0 ? Mathf.Clamp(commitFrame, 0f, activeStartFrame) / totalFrames : 0f;
    public float ActiveStartTime => totalFrames > 0 ? Mathf.Clamp01(activeStartFrame / totalFrames) : 0f;
    public float RecoveryStartTime => totalFrames > 0 ? Mathf.Clamp01(recoveryStartFrame / totalFrames) : 0f;

    /// <summary>
    /// 다음 콤보 전환 시점
    /// </summary>
    public float ComboStartTime => totalFrames > 0 ? Mathf.Clamp01(Mathf.Max(recoveryStartFrame, comboStartFrame) / totalFrames) : 0f;


    /// <summary>
    /// 애니메이션 총 재생 시간
    /// </summary>
    public float DurationInSeconds => totalFrames / (60f * AnimationSpeed);
}
