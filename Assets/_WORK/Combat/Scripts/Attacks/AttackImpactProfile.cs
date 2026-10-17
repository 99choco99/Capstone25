using UnityEngine;

public enum GuardReactionMotion
{
    [InspectorName("피격 강도에 따름")] FromImpactLevel,
    [InspectorName("작은 가드 반동")] Light,
    [InspectorName("중간 가드 반동")] Medium,
    [InspectorName("큰 가드 반동")] Heavy
}

/// <summary>특정 공격의 밀림·경직·타격 연출. 미지정 공격은 공통 전투 설정을 사용합니다.</summary>
[CreateAssetMenu(fileName = "AttackImpactProfile", menuName = "Scriptable Objects/Attack Impact Profile")]
public sealed class AttackImpactProfile : ScriptableObject
{
    [Header("접촉 반응")]
    [InspectorName("밀림 수치 덮어쓰기")]
    public bool OverrideKnockback = true;
    public CombatSettings.ContactReactionPreset Knockback = new();
    [InspectorName("접촉 반동의 최대 합산 거리 (-1 = 공통, 0 = 제한 없음)"), Min(-1f)]
    public float ContactSeparationDistanceLimit = -1f;
    [Tooltip("패링 성공 때만 접촉 반동의 합산 거리를 덮어씁니다. 일반 가드와 직접 피격의 밀림은 유지합니다.")]
    [InspectorName("패링 성공 시 최대 합산 거리 (-1 = 기존 설정, 0 = 제한 없음)"), Min(-1f)]
    public float ParryContactSeparationDistanceLimit = -1f;
    [Tooltip("일반 가드의 반동 모션만 지정합니다. 경직·재방어 대기는 공격의 피격 강도를 사용합니다.")]
    [InspectorName("가드 반동 모션")]
    public GuardReactionMotion GuardMotion;
    [Tooltip("패링당하면 끊기는 공격의 강한 반동 판정·접촉 연출을 유지합니다. 피격 강도와 재방어 대기는 바꾸지 않습니다.")]
    [InspectorName("강한 공격 반동 사용")]
    public bool ForceHeavyRebound;
    [InspectorName("강한 가드·패링 동작·접촉 이펙트 사용")]
    public bool UseHeavyDefenseFeedback;

    [Header("피격 경직·히트스톱·화면 흔들림")]
    [InspectorName("경직·타격 연출 수치 덮어쓰기")]
    public bool OverrideReactionSettings = true;
    public CombatSettings.Reaction Reaction = new();
}
