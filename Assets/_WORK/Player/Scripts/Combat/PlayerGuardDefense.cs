using UnityEngine;

public enum GuardDefensePhase
{
    Inactive,
    Held,
    First,
    Second,
    Third,
    Exhausted,
    Contact,
    Additional
}

/// <summary>플레이어 방어 단계와 판정 효과의 유효 구간. 입력 버퍼·이동·모션은 상태가 처리합니다.</summary>
public sealed class PlayerGuardDefense
{
    private readonly struct EffectWindow
    {
        private readonly float start;
        private readonly float end;

        public EffectWindow(float start, float duration)
        {
            this.start = start;
            end = start + Mathf.Max(0f, duration);
        }

        public bool Contains(float time) => time >= start && time < end;
    }

    private EffectWindow parryEffect;
    private EffectWindow repeatEffect;
    private EffectWindow additionalInputEffect;
    private float reinputAllowedTime = float.NegativeInfinity;
    private float guardEndAllowedTime = float.NegativeInfinity;

    public GuardDefensePhase Phase { get; private set; }
    public float ActionStartTime { get; private set; } = float.NegativeInfinity;

    public void Enter(float time, bool freshInput, CombatSettings settings)
    {
        Exit();
        if (freshInput)
            StartAction(GuardDefensePhase.First, time, settings);
        else
        {
            Phase = GuardDefensePhase.Held;
            ActionStartTime = time;
        }
    }

    public bool TryReenter(float time, CombatSettings settings)
    {
        if (time < reinputAllowedTime)
            return false;

        GuardDefensePhase next;
        if (Phase == GuardDefensePhase.Contact && additionalInputEffect.Contains(time))
            next = GuardDefensePhase.Additional;
        else if (repeatEffect.Contains(time))
        {
            next = Phase switch
            {
                GuardDefensePhase.First or GuardDefensePhase.Additional => GuardDefensePhase.Second,
                GuardDefensePhase.Second => GuardDefensePhase.Third,
                GuardDefensePhase.Third or GuardDefensePhase.Exhausted => GuardDefensePhase.Exhausted,
                _ => GuardDefensePhase.First
            };
        }
        else
            next = GuardDefensePhase.First;

        StartAction(next, time, settings);
        return true;
    }

    private void StartAction(GuardDefensePhase phase, float time, CombatSettings settings)
    {
        Phase = phase;
        ActionStartTime = time;
        float duration = phase switch
        {
            GuardDefensePhase.First => settings.ParryWindow,
            GuardDefensePhase.Second => settings.SecondParryWindow,
            GuardDefensePhase.Third => settings.ThirdParryWindow,
            GuardDefensePhase.Additional => settings.AdditionalParryWindow,
            _ => 0f
        };
        parryEffect = new EffectWindow(time, duration);
        guardEndAllowedTime = time + settings.GuardMinimumDuration;
        additionalInputEffect = default;

        repeatEffect = new EffectWindow(time, settings.GuardRepeatEffectDuration);
        reinputAllowedTime = time + settings.GuardRepeatInputStartTime;
    }

    public void BeginContact(float time, in DamageResult result, CombatSettings settings)
    {
        Phase = GuardDefensePhase.Contact;
        parryEffect = default;
        repeatEffect = default;
        reinputAllowedTime = time + settings.GetGuardReinputDelay(result);
        additionalInputEffect = new EffectWindow(reinputAllowedTime, settings.AdditionalParryWindow);
    }

    public bool HasGuardDefense(float time, bool held) => Phase != GuardDefensePhase.Inactive
        && (held
        || time < guardEndAllowedTime);

    public DefenseType GetDefense(float time, bool held)
    {
        if (parryEffect.Contains(time))
            return DefenseType.Parry;
        return HasGuardDefense(time, held) ? DefenseType.NormalGuard : DefenseType.None;
    }

    public void Exit()
    {
        Phase = GuardDefensePhase.Inactive;
        ActionStartTime = float.NegativeInfinity;
        parryEffect = default;
        repeatEffect = default;
        additionalInputEffect = default;
        reinputAllowedTime = float.NegativeInfinity;
        guardEndAllowedTime = float.NegativeInfinity;
    }
}
