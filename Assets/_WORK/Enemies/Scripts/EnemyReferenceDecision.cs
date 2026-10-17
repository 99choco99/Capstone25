using System;
using System.Collections.Generic;

public enum EnemyReferenceScope { Neutral, Exchange }

public readonly struct EnemyReferenceChoice
{
    public EnemyReferenceScope Scope { get; }
    public int Action { get; }
    public int TotalWeight { get; }
    public bool FellThrough { get; }
    public bool IsValid => Action > 0;

    public EnemyReferenceChoice(EnemyReferenceScope scope, int action, int totalWeight, bool fellThrough = false)
    {
        Scope = scope;
        Action = action;
        TotalWeight = totalWeight;
        FellThrough = fellThrough;
    }
}

/// <summary>모션과 분리된 원본 판단 입력. 각 Enemy가 자기 전투 상태와 공간 질의를 전달합니다.</summary>
public sealed class EnemyReferenceInput
{
    private static readonly int[] exchangeEvents = { 200200, 200201, 200205, 200206, 200210, 200211, 200215, 200216,
        200225, 200226, 200227, 200228, 200229 };

    public float Distance;
    public float HealthRatio = 1f;
    public float PostureRatio;
    public float Now;
    public float TargetAngle;
    public int Lives = 2;
    public int Event;
    public bool CommonActionHandled;
    public bool SpaceLeft45 = true, SpaceRight45 = true, SpaceLeft90 = true, SpaceRight90 = true;
    public bool SpaceBack2 = true, SpaceBack1 = true;
    public Func<float, float, bool> SpaceQuery;
    private int queriedSpaces;
    private int availableSpaces;
    public readonly int[] Numbers = new int[11];
    public readonly float[] Timers = new float[8];
    public readonly HashSet<int> SelfEffects = new();
    public readonly HashSet<int> TargetEffects = new();
    public IReadOnlyDictionary<int, float> ActionTimes;

    public EnemyReferenceInput() { Reset(); }

    public int EventCode
    {
        get
        {
            foreach (int code in exchangeEvents)
                if (HasEffect(true, code))
                    return code;
            return 0;
        }
    }

    public void Reset()
    {
        Array.Clear(Numbers, 0, Numbers.Length);
        for (int i = 0; i < Timers.Length; i++)
            Timers[i] = float.NegativeInfinity;
        Numbers[7] = Numbers[10] = 1;
        SelfEffects.Clear();
        TargetEffects.Clear();
        Event = 0;
        BeginSpaceQueries();
    }

    public void ResetExchange()
    {
        Event = 0;
        Numbers[0] = Numbers[2] = 0;
    }

    public bool HasEffect(bool self, int id) => self
        ? Event == id
        || SelfEffects.Contains(id)
        : TargetEffects.Contains(id);
    public int GetNumber(int index) => Numbers[index];
    public void SetNumber(int index, int value) => Numbers[index] = value;
    public bool TimerFinished(int index) => Now >= Timers[index];
    public bool Inside(bool behind, float angle) => behind
        ? Math.Abs(TargetAngle) >= 90f
        : Math.Abs(TargetAngle) <= angle * 0.5f;
    public bool HasSpace(float angle, float distance)
    {
        if (angle == -45f)
            return QuerySpace(1, -45f, 2f, SpaceLeft45);
        if (angle == 45f)
            return QuerySpace(2, 45f, 2f, SpaceRight45);
        if (angle == -90f)
            return QuerySpace(4, -90f, 1f, SpaceLeft90);
        if (angle == 90f)
            return QuerySpace(8, 90f, 1f, SpaceRight90);
        return distance <= 1f ? QuerySpace(32, 180f, 1f, SpaceBack1) : QuerySpace(16, 180f, 2f, SpaceBack2);
    }

    /// <summary>한 번의 판단에서 같은 공간 질의는 재사용하고, 다음 판단에는 새로 확인합니다.</summary>
    public void BeginSpaceQueries()
    {
        queriedSpaces = availableSpaces = 0;
    }

    private bool QuerySpace(int bit, float angle, float distance, bool fallback)
    {
        if (SpaceQuery == null)
            return fallback;
        if ((queriedSpaces & bit) == 0)
        {
            if (SpaceQuery(angle, distance))
                availableSpaces |= bit;
            queriedSpaces |= bit;
        }
        return (availableSpaces & bit) != 0;
    }
}

/// <summary>710300의 판단 예산과 정수 추첨. 원본 행동을 고른 뒤 모션 대응은 별도로 처리합니다.</summary>
// Reference: zoloypzuo/SekiroAI, 710300_battle.lua
// Source SHA-256: 7eea71b2fde492f194df95b22b31db845db3bb157a49d775da98987b3b470d52
public static class EnemyReferenceDecision
{
    public const int SourceVersion = 710300;
    public const int BudgetSize = 51;

    public static int Evaluate(EnemyReferenceInput input, bool exchange, int[] weights)
    {
        if (weights == null || weights.Length < BudgetSize)
            throw new ArgumentException(nameof(weights));
        Array.Clear(weights, 0, weights.Length);
        if (exchange)
        {
            if (input.EventCode == 0)
                return 0;
            BuildExchange(input, weights);
        }
        else
            BuildNeutral(input, weights);
        int total = 0;
        for (int i = 1; i < BudgetSize; i++)
            total += weights[i];
        return total;
    }

    public static int SelectIndex(int[] weights, int roll)
    {
        if (roll < 1)
            throw new ArgumentOutOfRangeException(nameof(roll));
        int cumulative = 0;
        for (int i = 1; i < BudgetSize; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return i;
        }
        return 0;
    }

    public static EnemyReferenceChoice Choose(
        EnemyReferenceInput input,
        int[] weights,
        Func<int, int, int> nextIntInclusive)
    {
        int total = Evaluate(input, true, weights);
        bool fellThrough = false;
        if (total > 0)
        {
            int action = SelectIndex(weights, nextIntInclusive(1, total));
            if (action != 50)
                return new EnemyReferenceChoice(EnemyReferenceScope.Exchange, action, total);
            // NoAction은 -1을 반환하며, 공통 검격 처리가 false가 되어 같은 Activate의 중립 판단으로 이어집니다.
            nextIntInclusive(1, 100);
            fellThrough = true;
        }
        total = Evaluate(input, false, weights);
        return new EnemyReferenceChoice(EnemyReferenceScope.Neutral,
            total > 0 ? SelectIndex(weights, nextIntInclusive(1, total)) : 0, total, fellThrough);
    }

    private static int Cooldown(EnemyReferenceInput input, int key, float duration, int weight, int repeatedWeight)
    {
        if (weight <= 0)
            return 0;
        return input.ActionTimes != null && input.ActionTimes.TryGetValue(key, out float started) && input.Now - started <= duration
            ? repeatedWeight : weight;
    }

    private static void BuildNeutral(EnemyReferenceInput input, int[] weights)
    {
        if (input.HasEffect(false, 110060) || input.HasEffect(false, 110010))
        {
            if (input.Inside(false, 90))
            {
                weights[21] = 1;
                weights[28] = 100;
            }
            else
            {
                weights[21] = 100;
            }
        }
        else if (input.CommonActionHandled)
        {
        }
        else if (input.GetNumber(7) == 0 && input.HasEffect(true, 200050))
        {
            weights[14] = 600;
        }
        else if (input.Distance >= 7 && input.GetNumber(7) == 1 && input.GetNumber(10) == 0)
        {
            weights[49] = 50;
        }
        else if (input.HasEffect(false, 110030))
        {
            weights[28] = 100;
        }
        else if (input.Inside(true, 180))
        {
            weights[21] = 100;
            weights[22] = 1;
        }
        else if (input.HasEffect(false, 110060) || input.HasEffect(false, 110010))
        {
            weights[27] = 100;
        }
        else if (input.Distance >= 7)
        {
            weights[7] = 600;
            weights[10] = 300;
            weights[14] = 600;
            weights[15] = 600;
            weights[16] = 600;
            if (input.Lives <= 1)
            {
                weights[38] = 700;
                weights[39] = 700;
            }
            else
            {
                weights[35] = 200;
            }
        }
        else if (input.Distance >= 5)
        {
            weights[7] = 600;
            weights[8] = 100;
            weights[10] = 300;
            weights[14] = 600;
            weights[34] = 100;
            weights[23] = 100;
            weights[30] = 100;
            if (input.Lives <= 1)
            {
                weights[38] = 700;
                weights[39] = 700;
            }
            else
            {
                weights[35] = 200;
            }
        }
        else if (input.Distance > 3)
        {
            weights[1] = 5;
            weights[2] = 10;
            weights[6] = 30;
            weights[11] = 15;
            weights[14] = 600;
            weights[23] = 15;
            weights[30] = 50;
            if (input.GetNumber(7) == 1)
            {
                weights[49] = 50;
            }
            if (input.Lives <= 1)
            {
                weights[38] = 700;
                weights[39] = 700;
            }
            else
            {
                weights[35] = 200;
            }
        }
        else
        {
            weights[3] = 15;
            weights[11] = 15;
            weights[23] = 10;
            weights[30] = 50;
            weights[31] = 30;
            if (input.GetNumber(7) == 1)
            {
                weights[49] = 50;
            }
            if (input.TimerFinished(7) == true)
            {
                weights[24] = 30;
            }
        }
        if ((input.HasEffect(false, 109031) || input.HasEffect(false, 110125)) && input.Distance <= 5)
        {
            weights[16] = 100;
        }
        if (input.HasEffect(false, 109900))
        {
            weights[1] = 5;
            weights[2] = 5;
            weights[3] = 5;
            weights[9] = 0;
            weights[11] = 5;
            weights[10] = 5;
            weights[14] = 0;
            weights[15] = 0;
            weights[35] = 0;
            weights[36] = 0;
            weights[48] = 30;
        }
        if (input.GetNumber(2) == 1)
        {
            weights[23] = 6000;
            input.SetNumber(2, 0);
        }
        if (input.TimerFinished(0) == false)
        {
            weights[3] = 0;
            weights[6] = 1;
        }
        if (input.TimerFinished(1) == false)
        {
            weights[2] = 0;
        }
        if (input.TimerFinished(3) == false)
        {
            weights[24] = 0;
        }
        if (input.TimerFinished(6) == false)
        {
            weights[9] = 0;
            weights[35] = 0;
            weights[36] = 0;
            if (input.GetNumber(9) == 1)
            {
                weights[38] = 0;
            }
        }
        if (input.HasEffect(true, 200051))
        {
            weights[8] = 0;
            weights[15] = 0;
            weights[19] = 0;
            weights[34] = 0;
            weights[48] = 0;
        }
        if (input.HasSpace(45, 2) == false && input.HasSpace(-45, 2) == false)
        {
            weights[22] = 0;
        }
        if (input.HasSpace(90, 1) == false && input.HasSpace(-90, 1) == false)
        {
            weights[23] = 0;
        }
        if (input.HasSpace(180, 2) == false)
        {
            weights[24] = 0;
        }
        if (input.HasSpace(180, 1) == false)
        {
            weights[25] = 0;
        }
        if (input.HasEffect(false, 110621))
        {
            weights[23] = 0;
            weights[24] = 0;
            weights[31] = 10;
        }
        weights[1] = Cooldown(input, 3000, 15f, weights[1], 1);
        weights[2] = Cooldown(input, 3004, 15f, weights[2], 1);
        weights[3] = Cooldown(input, 3005, 15f, weights[3], 1);
        weights[5] = Cooldown(input, 3007, 15f, weights[5], 1);
        weights[6] = Cooldown(input, 3016, 15f, weights[6], 1);
        weights[7] = Cooldown(input, 3038, 15f, weights[7], 1);
        weights[8] = Cooldown(input, 3038, 15f, weights[8], 1);
        weights[9] = Cooldown(input, 3092, 15f, weights[9], 1);
        weights[10] = Cooldown(input, 3006, 15f, weights[10], 1);
        weights[11] = Cooldown(input, 3037, 15f, weights[11], 1);
        weights[13] = Cooldown(input, 3031, 15f, weights[13], 1);
        weights[14] = Cooldown(input, 3026, 15f, weights[14], 1);
        weights[15] = Cooldown(input, 3014, 15f, weights[15], 1);
        weights[17] = Cooldown(input, 3014, 15f, weights[17], 1);
        weights[18] = Cooldown(input, 3014, 15f, weights[18], 1);
        weights[30] = Cooldown(input, 3009, 15f, weights[30], 1);
        weights[31] = Cooldown(input, 3045, 15f, weights[31], 1);
        weights[32] = Cooldown(input, 3006, 15f, weights[32], 1);
        weights[34] = Cooldown(input, 3007, 15f, weights[34], 1);
        weights[48] = Cooldown(input, 3013, 5f, weights[48], 1);
        weights[49] = Cooldown(input, 3018, 5f, weights[49], 1);
    }

    private static void BuildExchange(EnemyReferenceInput input, int[] weights)
    {
        if (input.EventCode == 200200)
        {
            input.SetNumber(0, input.GetNumber(0) + 1);
            if (input.Distance >= 2.5)
            {
                weights[50] = 100;
            }
            else if (input.GetNumber(0) >= 4)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[30] = 5;
                weights[41] = 50;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.Lives <= 1)
                {
                    weights[12] = 100;
                }
                if (input.GetNumber(6) == 0)
                {
                    weights[32] = 20;
                }
                else
                {
                    weights[33] = 20;
                }
            }
            else if (input.GetNumber(0) == 3)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[30] = 5;
                weights[41] = 50;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.Lives <= 1)
                {
                    weights[12] = 100;
                }
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[7] = 50;
                }
                else
                {
                    weights[4] = 100;
                }
            }
            else if (input.GetNumber(0) == 2)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[30] = 5;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[7] = 50;
                }
                else
                {
                    weights[4] = 100;
                }
            }
            else if (input.GetNumber(0) == 1)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[30] = 5;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[7] = 50;
                }
                else
                {
                    weights[4] = 100;
                    if (false)
                    {
                    }
                }
            }
        }
        else if (input.EventCode == 200201)
        {
            if (input.Distance >= 2.5)
            {
                weights[50] = 100;
            }
            else if (input.GetNumber(0) >= 4)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[41] = 50;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
            }
            else if (input.GetNumber(0) == 3)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[41] = 50;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[10] = 50;
                }
                else
                {
                    weights[4] = 100;
                    weights[22] = 50;
                }
            }
            else if (input.GetNumber(0) == 2)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[10] = 50;
                }
                else
                {
                    weights[4] = 100;
                    weights[22] = 50;
                }
            }
            else if (input.GetNumber(0) == 1)
            {
                weights[3] = 60;
                weights[20] = 60;
                weights[15] = 30;
                weights[43] = 50;
                weights[39] = 30;
                if (input.GetNumber(3) == 0)
                {
                    weights[1] = 50;
                    weights[10] = 50;
                }
                else
                {
                    weights[4] = 100;
                    weights[22] = 50;
                    if (false)
                    {
                    }
                }
            }
        }
        else if (input.EventCode == 200210)
        {
            weights[2] = 100;
            weights[17] = 100;
            weights[41] = 50;
            weights[31] = 50;
        }
        else if (input.EventCode == 200211)
        {
            weights[2] = 100;
            weights[10] = 50;
            weights[31] = 50;
            weights[41] = 50;
        }
        else if (input.EventCode == 200216)
        {
            if (input.Distance >= 2)
            {
                weights[50] = 10;
            }
            else
            {
                input.SetNumber(0, input.GetNumber(0) + 1);
                if (input.GetNumber(0) >= 4)
                {
                    weights[3] = 60;
                    weights[20] = 20;
                    weights[41] = 50;
                    weights[43] = 50;
                    if (input.Lives <= 1)
                    {
                        weights[12] = 100;
                    }
                    if (input.GetNumber(6) == 0)
                    {
                        weights[32] = 50;
                    }
                    else
                    {
                        weights[33] = 50;
                    }
                }
                else if (input.GetNumber(0) == 3)
                {
                    weights[3] = 60;
                    weights[20] = 20;
                    weights[43] = 50;
                    if (input.Lives <= 1)
                    {
                        weights[12] = 100;
                    }
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[30] = 50;
                    }
                    else
                    {
                        weights[14] = 50;
                        weights[13] = 50;
                    }
                }
                else if (input.GetNumber(0) == 2)
                {
                    weights[3] = 60;
                    weights[20] = 20;
                    weights[43] = 50;
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[30] = 50;
                    }
                    else
                    {
                        weights[14] = 50;
                        weights[13] = 50;
                    }
                }
                else if (input.GetNumber(0) == 1)
                {
                    weights[3] = 60;
                    weights[20] = 20;
                    weights[43] = 50;
                    weights[20] = 50;
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[30] = 50;
                    }
                    else
                    {
                        weights[14] = 50;
                        weights[13] = 50;
                        if (false)
                        {
                        }
                    }
                }
            }
        }
        else if (input.EventCode == 200215)
        {
            if (input.Distance >= 2)
            {
                weights[50] = 10;
            }
            else
            {
                input.SetNumber(0, input.GetNumber(0) + 1);
                if (input.GetNumber(0) >= 4)
                {
                    weights[20] = 20;
                    weights[41] = 50;
                    weights[31] = 15;
                    weights[43] = 10;
                    weights[39] = 10;
                    if (input.Lives <= 1)
                    {
                        weights[12] = 100;
                    }
                    if (input.GetNumber(6) == 0)
                    {
                        weights[32] = 50;
                    }
                    else
                    {
                        weights[33] = 50;
                    }
                }
                else if (input.GetNumber(0) == 3)
                {
                    weights[20] = 20;
                    weights[41] = 50;
                    weights[31] = 15;
                    weights[43] = 10;
                    if (input.Lives <= 1)
                    {
                        weights[12] = 100;
                    }
                    weights[39] = 10;
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[17] = 50;
                    }
                    else
                    {
                        weights[13] = 50;
                        weights[14] = 50;
                    }
                }
                else if (input.GetNumber(0) == 2)
                {
                    weights[20] = 20;
                    weights[31] = 15;
                    weights[43] = 10;
                    weights[39] = 10;
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[17] = 50;
                    }
                    else
                    {
                        weights[13] = 50;
                        weights[14] = 50;
                    }
                }
                else if (input.GetNumber(0) == 1)
                {
                    weights[20] = 20;
                    weights[31] = 15;
                    weights[43] = 10;
                    weights[39] = 10;
                    if (input.GetNumber(3) == 0)
                    {
                        weights[1] = 50;
                        weights[17] = 50;
                    }
                    else
                    {
                        weights[13] = 50;
                        weights[14] = 50;
                    }
                }
                else
                {
                }
            }
        }
        if (input.HasEffect(true, 200051))
        {
            weights[3] = 0;
            weights[9] = 0;
            weights[12] = 0;
            weights[15] = 0;
            weights[32] = 0;
            weights[33] = 0;
            weights[39] = 0;
            weights[46] = 0;
        }
        else if (input.HasEffect(true, 200050))
        {
            weights[20] = 0;
        }
        if (input.HasSpace(45, 2) == false && input.HasSpace(-45, 2) == false)
        {
            weights[20] = 0;
        }
        if (input.TimerFinished(6) == false)
        {
            weights[40] = 0;
            weights[48] = 0;
        }
        else if (input.TimerFinished(6) == true && input.HealthRatio <= 0.75)
        {
            weights[40] = 50;
            weights[48] = 50;
        }
        weights[1] = Cooldown(input, 3050, 8f, weights[1], 1);
        weights[2] = Cooldown(input, 5201, 10f, weights[2], 1);
        weights[3] = Cooldown(input, 3009, 10f, weights[3], 1);
        weights[4] = Cooldown(input, 3055, 8f, weights[4], 1);
        weights[7] = Cooldown(input, 3060, 8f, weights[7], 1);
        weights[9] = Cooldown(input, 3018, 8f, weights[9], 1);
        weights[10] = Cooldown(input, 3065, 8f, weights[10], 1);
        weights[11] = Cooldown(input, 3070, 8f, weights[11], 1);
        weights[12] = Cooldown(input, 3018, 8f, weights[12], 1);
        weights[13] = Cooldown(input, 3070, 8f, weights[13], 1);
        weights[14] = Cooldown(input, 3076, 8f, weights[14], 1);
        weights[15] = Cooldown(input, 3031, 15f, weights[15], 1);
        weights[16] = Cooldown(input, 3070, 8f, weights[16], 1);
        weights[17] = Cooldown(input, 3071, 8f, weights[17], 1);
        weights[18] = Cooldown(input, 3004, 8f, weights[18], 1);
        weights[20] = Cooldown(input, 5202, 15f, weights[20], 1);
        weights[22] = Cooldown(input, 3070, 8f, weights[22], 1);
        weights[30] = Cooldown(input, 3063, 15f, weights[30], 1);
        weights[31] = Cooldown(input, 3068, 15f, weights[31], 1);
        weights[32] = Cooldown(input, 3018, 15f, weights[32], 1);
        weights[33] = Cooldown(input, 3007, 15f, weights[33], 1);
        weights[34] = Cooldown(input, 3037, 15f, weights[34], 1);
        weights[35] = Cooldown(input, 3016, 8f, weights[35], 1);
        weights[37] = Cooldown(input, 3030, 8f, weights[37], 1);
        weights[38] = Cooldown(input, 3030, 8f, weights[38], 1);
        weights[39] = Cooldown(input, 3034, 15f, weights[39], 1);
        weights[40] = Cooldown(input, 3040, 15f, weights[40], 1);
        weights[41] = Cooldown(input, 3030, 15f, weights[41], 1);
        weights[42] = Cooldown(input, 3030, 15f, weights[42], 1);
        weights[43] = Cooldown(input, 3062, 15f, weights[43], 1);
        weights[44] = Cooldown(input, 3067, 15f, weights[44], 1);
        weights[45] = Cooldown(input, 3032, 15f, weights[45], 1);
    }
}
