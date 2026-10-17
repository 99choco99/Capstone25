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

    [Header("넉백")]
    public KnockbackSettings Knockback = new();

    [Header("검 접촉")]
    [InspectorName("몸 앞의 방어 거리 (유닛)"), Min(0f)] public float GuardContactReach = 0.45f;

    [Header("연격 도달 보정")]
    [InspectorName("직전 접촉 간격으로 돌아갈 최대 추가 거리 (유닛)"), Min(0f)] public float AttackReachMaxExtraDistance = 1.1f;
    [InspectorName("원본 전진량 대비 최대 배율"), Min(1f)] public float AttackReachMaxForwardMultiplier = 2f;
    public float AttackReachExtraDistance => AttackReachMaxForwardMultiplier > 1f ? AttackReachMaxExtraDistance : 0f;
    [InspectorName("같은 상대의 접촉 기록 유지 시간 (초)"), Min(0f)] public float AttackReachContactMemoryDuration = 1f;

    [Header("행동 대기 시간 (게임 시간 / 초)")]
    [Tooltip("일반 가드 반응 후 다음 행동까지 기다리는 시간입니다. 루트모션 압박은 별도 거리나 시간을 사용하지 않습니다.")]
    [InspectorName("일반 가드 후 최소 행동 대기"), Min(0f)] public float GuardRecoveryDuration = 0.24f;
    [InspectorName("플레이어 가드 후 행동 대기"), Min(0f)] public float PlayerGuardRecoveryDuration = 0.5f;
    [InspectorName("플레이어 패링 후 행동 대기"), Min(0f)] public float PlayerParryRecoveryDuration = 0.2f;
    [InspectorName("플레이어가 패링당한 후 행동 대기"), Min(0f)] public float PlayerParriedRecoveryDuration = 0.4f;
    [Tooltip("패링 후 적 반격은 플레이어 반동 복귀 시간에 이 여유를 더한 뒤 시작합니다. 적별 반격 대기가 더 길면 그 값을 사용합니다.")]
    [InspectorName("플레이어 방어 복귀 후 반격 시작 여유"), Min(0f)] public float CounterDefenseMargin = 0.05f;
    [InspectorName("강한 끝타 방어 자세의 표시 시간"), Min(0f)] public float HeavyGuardReactionDuration = 0.4f;
    [Tooltip("패링당한 공격자의 행동 대기 시간입니다. 밀림 시간과 반응 클립 재생속도는 바꾸지 않습니다.")]
    [InspectorName("패링당한 공격자의 최소 행동 대기"), Min(0f)] public float ParriedRecoveryDuration = 0.35f;
    [InspectorName("적 반동 이후 행동 가능 진행도"), Range(0f, 1f)] public float EnemyReboundExitProgress = 0.6f;

    /// <summary>
    /// 패링 성공 시 원래 공격자에게 되돌릴 체간 비율
    /// </summary>
    [Min(0f)] public float DeflectPostureRatio = 1f;

    /// <summary>
    /// 패링시 자기가 받는 체간 비율
    /// </summary>
    [Min(0f)] public float ParryPostureRatio = 0.25f;
    [InspectorName("일반 가드 체간 피해 비율"), Min(0f)] public float GuardPostureRatio = 0.5f;
    [InspectorName("플레이어 일반 가드 체간 피해 비율"), Min(0f)] public float PlayerGuardPostureRatio = 1f;

    [Header("플레이어 패링 입력 (게임 시간 / 초)")]
    [Min(0f)] public float PlayerInputBufferTime = 0.25f;
    [InspectorName("첫 입력 판정 시간"), Min(0f)] public float ParryWindow = 0.2f;
    [InspectorName("두 번째 연속 입력 판정 시간"), Min(0f)] public float SecondParryWindow = 0.1f;
    [InspectorName("세 번째 연속 입력 판정 시간"), Min(0f)] public float ThirdParryWindow = 0.06666667f;
    [InspectorName("접촉 후 추가 방어 입력·판정 시간"), Min(0f)] public float AdditionalParryWindow = 0.3f;
    [InspectorName("반복 방어 재입력 허용 시작"), Min(0f)] public float GuardRepeatInputStartTime = 0.2f;
    [InspectorName("반복 방어 효과 유지 시간"), Min(0f)] public float GuardRepeatEffectDuration = 0.6f;
    [InspectorName("중간 가드 반동 후 재방어 허용"), Min(0f)] public float GuardReinputMediumDelay = 0.4f;
    [InspectorName("강한 가드 반동 후 재방어 허용"), Min(0f)] public float GuardReinputHeavyDelay = 0.8f;
    [InspectorName("강한 패링 반동 후 재방어 허용"), Min(0f)] public float ParryReinputHeavyDelay = 0.3f;
    [InspectorName("가드 입력 직후 제자리 유지 시간"), Min(0f)] public float GuardInputLockTime = 0.12f;
    [InspectorName("짧은 입력의 최소 가드 유지 시간"), Min(0f)] public float GuardMinimumDuration = 0.3f;

    public float GetGuardReinputDelay(in DamageResult result)
    {
        if (result.DefenseType == DefenseType.Parry)
            return result.Request.KnockBackLevel == KnockBackLevel.Heavy ? ParryReinputHeavyDelay : 0f;

        return result.Request.KnockBackLevel switch
        {
            KnockBackLevel.Medium => GuardReinputMediumDelay,
            KnockBackLevel.Heavy => GuardReinputHeavyDelay,
            _ => 0f
        };
    }

    [Header("플레이어 회피와 체간 붕괴 (게임 시간 / 초)")]
    [InspectorName("회피 무적 시간"), Min(0f)] public float PlayerDodgeInvincibleDuration = 0.2f;
    [InspectorName("회피 전체 시간"), Min(0f)] public float PlayerDodgeDuration = 0.74f;
    [InspectorName("체간 붕괴 후 행동 대기"), Min(0f)] public float PlayerPostureBreakDuration = 1f;

    [Header("플레이어 강공격 그로기 (반응 모션 종료 후 복귀)")]
    [InspectorName("회피 입력 복귀 허용 (초)"), Min(0f)] public float PlayerKnockdownDodgeDelay = 0.6f;
    [InspectorName("자동 복귀 대기 (초)"), Min(0f)] public float PlayerKnockdownRecoveryDelay = 1f;
    [Tooltip("복귀 시작 전 대기와 별개로, 복귀를 시작한 후 행동을 다시 허용할 때까지의 대기 시간입니다.")]
    [InspectorName("복귀 시작 후 행동 대기 (초)"), Min(0f)] public float PlayerKnockdownRecoveryDuration = 0.65f;
    [Tooltip("회피 입력 복귀의 행동 대기를 이 배율로 나눕니다. 애니메이션 재생속도는 바꾸지 않습니다.")]
    [InspectorName("회피 입력 복귀 시간 단축 배율"), Min(1f)] public float PlayerKnockdownEarlyRecoveryMultiplier = 1.25f;

    [Header("적의 가드와 공격 간격")]
    [InspectorName("가드 준비 유지 시간 (초)"), Min(0f)] public float EnemyGuardDuration = 0.3f;
    [InspectorName("공격 판정 뒤 가드 유지 여유 (초)"), Min(0f)] public float EnemyGuardHoldPadding = 0.1f;
    [InspectorName("패링 성공 후 반응 시간 (초)"), Min(0f)] public float EnemyParryReactionDuration = 0.16f;
    [InspectorName("실제 가드 이력 유지 시간 (초)"), Min(0f)] public float EnemyGuardPressureMemoryDuration = 1f;

    [Header("적의 그로기 (게임 시간 / 초)")]
    [InspectorName("체간 붕괴 후 회복까지 시간"), Min(0f)] public float EnemyPostureBreakRecoveryDelay = 2f;
    [InspectorName("그로기 진입 동작 시간"), Min(0f)] public float EnemyGroggyEnterDuration = 0.9f;
    [InspectorName("그로기 회복 동작 시간"), Min(0f)] public float EnemyGroggyRecoverDuration = 0.32f;

    [Header("체간 회복")]
    [InspectorName("플레이어 체간 회복")]
    public PostureRecovery PlayerPostureRecovery = new()
    {
        Delay = 1f,
        RateRatio = 0.25f,
        GuardMultiplier = 2f
    };
    [Tooltip("겐이치로의 기본 회복 비율과 체력 구간을 사용합니다. 고정 피격 후 대기 없이 행동 상태가 회복을 허용하며, 가드 추가 배율은 적용하지 않습니다.")]
    [InspectorName("적 체간 회복")]
    public PostureRecovery EnemyPostureRecovery = new()
    {
        Delay = 0f,
        RateRatio = 60f / 660f,
        GuardMultiplier = 1f,
        HighHealthThreshold = 0.8f,
        MediumHealthThreshold = 0.6f,
        LowHealthThreshold = 0.4f,
        FullHealthMultiplier = 1f,
        HighHealthMultiplier = 0.5f,
        MediumHealthMultiplier = 0.167f,
        LowHealthMultiplier = 0.0833f
    };

    [Header("히트스톱과 인살")]
    [Tooltip("히트스톱 동안만 적용하는 시간 배율. 0은 완전 정지, 1은 정상 속도입니다.")]
    [InspectorName("히트스톱 중 시간 배율"), Range(0f, 1f)] public float HitStopTimeScale = 0.05f;
    [InspectorName("인살 히트스톱 (실제 초)"), Min(0f)] public float DeathblowHitStopDuration = 0.06f;
    [InspectorName("인살 화면 흔들림 강도"), Min(0f)] public float DeathblowShakeStrength = 0.25f;

    [Tooltip("성공한 인살 한 번마다 최대 체간의 이 비율만큼 회복합니다. 스킬로 얻는 추가 회복은 포함하지 않습니다.")]
    [InspectorName("인살 성공 시 최대 체간 회복 비율"), Range(0f, 1f)] public float DeathblowPostureRecoveryRatio = 0.34f;

    [Tooltip("물약 대신 성공한 인살 한 번마다 최대 체력의 이 비율만큼 회복합니다. 보스의 중간 인살에도 적용하며 취소된 인살에는 적용하지 않습니다.")]
    [InspectorName("인살 성공 시 최대 체력 회복 비율"), Range(0f, 1f)] public float DeathblowHealthRecoveryRatio = 0.35f;

    [Header("체간 붕괴")]
    [InspectorName("붕괴 히트스톱 (실제 초)"), Min(0f)] public float PostureBreakHitStopDuration = 0.12f;
    [InspectorName("붕괴 화면 흔들림 강도"), Min(0f)] public float PostureBreakShakeStrength = 0.36f;

    [Header("공격 등급별 반응 (숨은 배율 없이 아래 값을 그대로 적용)")]
    [InspectorName("Light - 약한 공격")]
    public Reaction Light = new();

    [InspectorName("Medium - 중간 공격")]
    public Reaction Medium = new()
    {
        HitRecoveryDuration = 0.5f,
        HitGuardRecoveryDuration = 0.4f,
        DirectHitStop = 0.02875f,
        GuardHitStop = 0.03f,
        ParryHitStop = 0.075f,
        SpecialHitStop = 0.046f,
        DirectHitShake = 0.12f,
        GuardShake = 0.096f,
        ParryShake = 0.26f,
        SpecialHitShake = 0.264f
    };

    [InspectorName("Heavy - 강한 공격")]
    public Reaction Heavy = new()
    {
        HitRecoveryDuration = 0.6f,
        HitGuardRecoveryDuration = 0.4f,
        DirectHitStop = 0.0325f,
        GuardHitStop = 0.035f,
        ParryHitStop = 0.085f,
        SpecialHitStop = 0.052f,
        DirectHitShake = 0.14f,
        GuardShake = 0.16f,
        ParryShake = 0.36f,
        SpecialHitShake = 0.308f
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

    public Reaction GetReaction(in DamageRequest request)
    {
        return request.ImpactProfile != null && request.ImpactProfile.OverrideReactionSettings && request.ImpactProfile.Reaction != null
            ? request.ImpactProfile.Reaction : GetReaction(request.KnockBackLevel);
    }

    public float GetPlayerDefenseRecovery(in DamageResult result)
    {
        return result.DefenseType == DefenseType.Parry
            ? PlayerParryRecoveryDuration : PlayerGuardRecoveryDuration;
    }

    public float GetGuardReactionDuration(in DamageResult result, float recoveryDuration)
    {
        if (!result.Request.IsHeavyRebound)
            return recoveryDuration;

        return Mathf.Max(recoveryDuration, HeavyGuardReactionDuration);
    }

    public float GetHitRecoveryTime(KnockBackLevel level)
    {
        return GetReaction(level).HitRecoveryDuration;
    }

    [Serializable]
    public sealed class KnockbackSettings
    {
        [InspectorName("Light / Medium 접촉 반동의 최대 합산 거리 (0 = 제한 없음)"), Min(0f)] public float ContactSeparationDistanceLimit = 1.1f;
        [Tooltip("Heavy 공격 한 번의 접촉으로 발생하는 수비자·공격자 반동을 합산해 제한합니다. 반응 프리셋의 개별 상한이 우선합니다.")]
        [InspectorName("Heavy 접촉 반동의 최대 합산 거리 (0 = 제한 없음)"), Min(0f)] public float HeavyContactSeparationDistanceLimit = 2.1f;

        [InspectorName("패링당한 공격자")]
        public KnockbackReaction Parried = new()
        {
            Duration = 0.40f,
            LightDistance = 0.60f,
            MediumDistance = 0.80f,
            HeavyDistance = 1.10f,
            MovementCurve = new(
                new Keyframe(0f, 0f, 0f, 2.7f),
                new Keyframe(0.2f, 0.5f, 1.15f, 1.15f),
                new Keyframe(0.6f, 0.8f, 0.65f, 0.65f),
                new Keyframe(1f, 1f, 0.2f, 0f))
        };
        [InspectorName("시간 대비 누적 이동 비율")]
        [Tooltip("가로는 시간 비율, 세로는 누적 거리 비율. (0, 0)에서 (1, 1)까지 상승하도록 설정합니다.")]
        public AnimationCurve MovementCurve = new(new Keyframe(0f, 0f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f)); //1 - (1-t)^3 (Cubic Ease-Out)

        [InspectorName("Light - 약한 공격")]
        public ContactReactionPreset Light = new()
        {
            DirectHit = new(0.9f, 0.3f),
            Guard = new(0.9f, 0.26f),
            Parry = new(0.28f, 0.18f),
            MovementCurve = new(
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(0.2f, 0.45f, 1.25f, 1.25f),
                new Keyframe(0.6f, 0.8f, 0.75f, 0.75f),
                new Keyframe(1f, 1f, 0.25f, 0f))
        };
        [InspectorName("Medium - 중간 공격")]
        public ContactReactionPreset Medium = new()
        {
            DirectHit = new(1.05f, 0.34f),
            Guard = new(0.95f, 0.32f),
            Parry = new(0.85f, 0.28f),
            MovementCurve = new(
                new Keyframe(0f, 0f, 0f, 2.3f),
                new Keyframe(0.2f, 0.5f, 1.1f, 1.1f),
                new Keyframe(0.65f, 0.85f, 0.65f, 0.65f),
                new Keyframe(1f, 1f, 0.2f, 0f))
        };
        [InspectorName("Heavy - 강한 공격")]
        public ContactReactionPreset Heavy = new()
        {
            DirectHit = new(1.5f, 0.45f),
            Guard = new(1.35f, 0.42f),
            Parry = new(0.95f, 0.32f),
            MovementCurve = new(
                new Keyframe(0f, 0f, 0f, 2.5f),
                new Keyframe(0.2f, 0.55f, 0.9f, 0.9f),
                new Keyframe(0.7f, 0.9f, 0.55f, 0.55f),
                new Keyframe(1f, 1f, 0.15f, 0f))
        };

        public KnockbackSpec GetDefenderSpec(DefenseType defense, KnockBackLevel level)
        {
            if (level == KnockBackLevel.None)
                return default;

            ContactReactionPreset preset = level switch
            {
                KnockBackLevel.Medium => Medium,
                KnockBackLevel.Heavy => Heavy,
                _ => Light
            };
            return preset.GetSpec(defense);
        }
    }

    [Serializable]
    public sealed class ContactReactionPreset
    {
        [InspectorName("피격")] public ContactImpulse DirectHit;
        [InspectorName("일반 가드")] public ContactImpulse Guard;
        [InspectorName("패링")] public ContactImpulse Parry;
        [InspectorName("누적 이동 커브 (미설정 = 공통 커브)")] public AnimationCurve MovementCurve;

        public KnockbackSpec GetSpec(DefenseType defense)
        {
            ContactImpulse impulse = defense switch
            {
                DefenseType.NormalGuard => Guard,
                DefenseType.Parry => Parry,
                _ => DirectHit
            };
            AnimationCurve curve = MovementCurve != null && MovementCurve.length > 0 ? MovementCurve : null;
            return new KnockbackSpec(impulse.Distance, impulse.Duration, curve);
        }
    }

    [Serializable]
    public struct ContactImpulse
    {
        [InspectorName("거리 (유닛)"), Min(0f)] public float Distance;
        [InspectorName("시간 (초)"), Min(0f)] public float Duration;

        public ContactImpulse(float distance, float duration)
        {
            Distance = distance;
            Duration = duration;
        }
    }

    [Serializable]
    public sealed class KnockbackReaction
    {
        [InspectorName("밀림 시간 (게임 초)"), Min(0f)] public float Duration;
        [InspectorName("Light 거리 (유닛)"), Min(0f)] public float LightDistance;
        [InspectorName("Medium 거리 (유닛)"), Min(0f)] public float MediumDistance;
        [InspectorName("Heavy 거리 (유닛)"), Min(0f)] public float HeavyDistance;
        [InspectorName("누적 이동 커브 (미설정 = 공통 커브)")] public AnimationCurve MovementCurve;

        public KnockbackSpec GetSpec(KnockBackLevel level)
        {
            float distance = level switch
            {
                KnockBackLevel.Light => LightDistance,
                KnockBackLevel.Medium => MediumDistance,
                KnockBackLevel.Heavy => HeavyDistance,
                _ => 0f
            };
            AnimationCurve curve = MovementCurve != null && MovementCurve.length > 0 ? MovementCurve : null;
            return new KnockbackSpec(distance, Duration, curve);
        }
    }

    /// <summary>한 공격 등급의 경직, 연출 수치</summary>
    [Serializable]
    public sealed class Reaction
    {
        [Header("피격 경직 (게임 시간 / 초)")]
        [Tooltip("직접 맞은 뒤 다음 행동까지 기다리는 시간입니다. 밀림 시간과 반응 클립 재생속도는 바꾸지 않습니다.")]
        [InspectorName("최소 행동 불가 시간"), Min(0f)] public float HitRecoveryDuration = 0.4f;
        [InspectorName("플레이어 방어 복귀 시간"), Min(0f)] public float HitGuardRecoveryDuration = 0.2f;

        [Header("히트스톱 (실제 시간 / 초)")]
        [InspectorName("직접 피격"), Min(0f)] public float DirectHitStop = 0.025f;
        [InspectorName("일반 가드"), Min(0f)] public float GuardHitStop = 0.025f;
        [InspectorName("패링"), Min(0f)] public float ParryHitStop = 0.065f;
        [InspectorName("가드 불가 공격의 직접 피격"), Min(0f)] public float SpecialHitStop = 0.04f;

        [Header("화면 흔들림 강도 (0이면 없음)")]
        [InspectorName("직접 피격"), Min(0f)] public float DirectHitShake = 0.1f;
        [InspectorName("일반 가드"), Min(0f)] public float GuardShake = 0.08f;
        [InspectorName("패링"), Min(0f)] public float ParryShake = 0.22f;
        [InspectorName("가드 불가 공격의 직접 피격"), Min(0f)] public float SpecialHitShake = 0.22f;

        public float GetHitStopDuration(in DamageResult result)
        {
            return SelectFeedbackValue(result, DirectHitStop, GuardHitStop, ParryHitStop, SpecialHitStop);
        }

        public float GetShakeStrength(in DamageResult result)
        {
            return SelectFeedbackValue(result, DirectHitShake, GuardShake, ParryShake, SpecialHitShake);
        }

        /// <summary>확정된 방어 결과를 우선하고, 직접 피격일 때만 가드 가능 여부로 연출 수치를 구분합니다.</summary>
        private static float SelectFeedbackValue(
            in DamageResult result,
            float directHit,
            float guardHit,
            float parry,
            float specialHit)
        {
            return result.DefenseType switch
            {
                DefenseType.Parry => parry,
                DefenseType.NormalGuard => guardHit,
                _ => result.Request.CanGuard ? directHit : specialHit
            };
        }
    }

    [Serializable]
    public sealed class PostureRecovery
    {
        [InspectorName("피해·피격 반응 후 회복 대기 (초)"), Min(0f)] public float Delay = 2f;
        [InspectorName("초당 최대 체간 회복 비율"), Min(0f)] public float RateRatio = 0.25f;
        [InspectorName("가드 회복 배율"), Min(1f)] public float GuardMultiplier = 2f;

        [Header("체력 비율 경계 (위에서부터 높은 값 / 해당 값 초과)")]
        [InspectorName("높은 체력 경계"), Range(0f, 1f)] public float HighHealthThreshold = 0.75f;
        [InspectorName("중간 체력 경계"), Range(0f, 1f)] public float MediumHealthThreshold = 0.5f;
        [InspectorName("낮은 체력 경계"), Range(0f, 1f)] public float LowHealthThreshold = 0.25f;

        [Header("체력 구간별 회복 배율")]
        [InspectorName("높은 경계 초과"), Min(0f)] public float FullHealthMultiplier = 1f;
        [InspectorName("중간 경계 초과"), Min(0f)] public float HighHealthMultiplier = 0.75f;
        [InspectorName("낮은 경계 초과"), Min(0f)] public float MediumHealthMultiplier = 0.5f;
        [InspectorName("낮은 경계 이하"), Min(0f)] public float LowHealthMultiplier = 0.25f;

        /// <summary>
        /// hp 비율당 체간 회복률 가져오기
        /// </summary>
        public float GetHealthMultiplier(float hpPercentage)
        {
            if (hpPercentage > HighHealthThreshold)
                return FullHealthMultiplier;
            if (hpPercentage > MediumHealthThreshold)
                return HighHealthMultiplier;
            if (hpPercentage > LowHealthThreshold)
                return MediumHealthMultiplier;
            return LowHealthMultiplier;
        }
    }
}
