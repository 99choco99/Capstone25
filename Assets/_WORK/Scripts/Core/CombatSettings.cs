using System;
using UnityEngine;

/// <summary>공통 전투 수치. 에셋 한 곳에서 조절</summary>
public sealed class CombatSettings : ScriptableObject
{
    private static CombatSettings current;

    /// <summary>씬마다 연결하지 않고 같은 설정 에셋을 한 번 불러와 공유.</summary>
    public static CombatSettings Current
    {
        get
        {
            if (current == null)
            {
                current = Resources.Load<CombatSettings>("CombatSettings");
                if (current == null)
                    throw new InvalidOperationException("Resources/CombatSettings 에셋이 필요합니다.");
            }
            return current;
        }
    }

    [Header("밀림 시간 (게임 시간 / 초)")]
    [InspectorName("직접 피격 시 밀림 시간"), Min(0f)] public float DirectHitMoveDuration = 0.26f;
    [InspectorName("일반 가드 시 밀림 시간"), Min(0f)] public float GuardMoveDuration = 0.20f;
    [InspectorName("패링당한 공격자의 밀림 시간"), Min(0f)] public float ParriedMoveDuration = 0.24f;

    [Header("행동 대기 시간 (게임 시간 / 초)")]
    [Tooltip("밀림이 끝나기 전에 상태가 풀리지 않도록, 밀림 시간과 이 값 중 긴 시간을 사용합니다.")]
    [InspectorName("일반 가드 후 최소 행동 대기"), Min(0f)] public float GuardRecoveryDuration = 0.20f;
    [Tooltip("패링당한 공격자의 반동 시간. 밀림 시간보다 짧게 설정하면 밀림이 끝날 때까지 기다립니다.")]
    [InspectorName("패링당한 공격자의 최소 행동 대기"), Min(0f)] public float ParriedRecoveryDuration = 0.35f;

    [Header("플레이어 패링 입력 (실제 시간 / 초)")]
    [InspectorName("첫 입력 판정 시간"), Min(0f)] public float ParryWindow = 0.2f;
    [InspectorName("두 번째 연속 입력 판정 시간"), Min(0f)] public float SecondParryWindow = 0.1f;
    [InspectorName("세 번째 연속 입력 판정 시간"), Min(0f)] public float ThirdParryWindow = 0.067f;
    [InspectorName("연타 페널티 초기화 시간"), Min(0f)] public float ParrySpamResetTime = 0.5f;
    [InspectorName("가드 입력 직후 제자리 유지 시간"), Min(0f)] public float GuardInputLockTime = 0.12f;

    [Header("적의 가드와 공격 간격")]
    [InspectorName("가드 준비 유지 시간 (초)"), Min(0f)] public float EnemyGuardDuration = 0.3f;
    [InspectorName("패링 성공 후 반응 시간 (초)"), Min(0f)] public float EnemyParryReactionDuration = 0.22f;
    [Tooltip("공격 전진 중 플레이어 중심과 유지할 최소 거리. 플레이어의 몸 충돌 반경을 바꾸는 값은 아닙니다.")]
    [InspectorName("적 공격 시 최소 중심 간격 (유닛)"), Min(0f)] public float EnemyAttackMinimumDistance = 1.35f;

    [Header("히트스톱과 인살")]
    [Tooltip("히트스톱 동안만 적용하는 시간 배율. 0은 완전 정지, 1은 정상 속도입니다.")]
    [InspectorName("히트스톱 중 시간 배율"), Range(0f, 1f)] public float HitStopTimeScale = 0.05f;
    [InspectorName("인살 히트스톱 (실제 초)"), Min(0f)] public float DeathblowHitStopDuration = 0.06f;
    [InspectorName("인살 화면 흔들림 강도"), Min(0f)] public float DeathblowShakeStrength = 0.25f;

    [Header("공격 등급별 반응 (숨은 배율 없이 아래 값을 그대로 적용)")]
    [InspectorName("Light - 약한 공격")]
    public Reaction Light = new Reaction();

    [InspectorName("Medium - 중간 공격")]
    public Reaction Medium = new Reaction
    {
        DirectHitDistance = 0.65f, GuardDistance = 0.27f, ParriedDistance = 0.30f,
        HitRecoveryDuration = 0.5f,
        DirectHitStop = 0.02875f, GuardHitStop = 0.0345f, ParryHitStop = 0.06325f, SpecialHitStop = 0.046f,
        DirectHitShake = 0.12f, GuardShake = 0.096f, ParryShake = 0.216f, SpecialHitShake = 0.264f
    };

    [InspectorName("Heavy - 강한 공격")]
    public Reaction Heavy = new Reaction
    {
        DirectHitDistance = 0.95f, GuardDistance = 0.42f, ParriedDistance = 0.46f,
        HitRecoveryDuration = 0.6f,
        DirectHitStop = 0.0325f, GuardHitStop = 0.039f, ParryHitStop = 0.0715f, SpecialHitStop = 0.052f,
        DirectHitShake = 0.14f, GuardShake = 0.112f, ParryShake = 0.252f, SpecialHitShake = 0.308f
    };

    /// <summary>이번 공격 등급의 설정. None은 기존처럼 Light 연출을 쓰되 KnockBackPolicy에서 이동만 없앱니다.</summary>
    public Reaction GetReaction(KnockBackLevel level)
    {
        return level switch
        {
            KnockBackLevel.Medium => Medium,
            KnockBackLevel.Heavy => Heavy,
            _ => Light
        };
    }

    /// <summary>한 공격 등급의 밀림·경직·연출 수치. 거리는 충돌로 막히지 않았을 때의 목표 이동량입니다.</summary>
    [Serializable]
    public sealed class Reaction
    {
        [Header("밀림 거리 (유닛)")]
        [InspectorName("직접 맞은 대상"), Min(0f)] public float DirectHitDistance = 0.40f;
        [InspectorName("일반 가드한 대상"), Min(0f)] public float GuardDistance = 0.15f;
        [InspectorName("패링당한 공격자"), Min(0f)] public float ParriedDistance = 0.18f;

        [Header("피격 경직 (게임 시간 / 초)")]
        [Tooltip("직접 맞은 뒤 행동하지 못하는 최소 시간. 밀림 시간보다 짧으면 밀림이 끝날 때까지 기다립니다.")]
        [InspectorName("최소 행동 불가 시간"), Min(0f)] public float HitRecoveryDuration = 0.4f;

        [Header("히트스톱 (실제 시간 / 초)")]
        [InspectorName("직접 피격"), Min(0f)] public float DirectHitStop = 0.025f;
        [InspectorName("일반 가드"), Min(0f)] public float GuardHitStop = 0.03f;
        [InspectorName("패링"), Min(0f)] public float ParryHitStop = 0.055f;
        [InspectorName("방어 불가 공격의 직접 피격"), Min(0f)] public float SpecialHitStop = 0.04f;

        [Header("화면 흔들림 강도 (0이면 없음)")]
        [InspectorName("직접 피격"), Min(0f)] public float DirectHitShake = 0.1f;
        [InspectorName("일반 가드"), Min(0f)] public float GuardShake = 0.08f;
        [InspectorName("패링"), Min(0f)] public float ParryShake = 0.18f;
        [InspectorName("방어 불가 공격의 직접 피격"), Min(0f)] public float SpecialHitShake = 0.22f;
    }
}
