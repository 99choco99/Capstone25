using UnityEngine;

/// <summary>
/// 방어 상태와 공격 허용 조건의 공통 판정 규칙. 상태 전환과 방어각 설정은 각 소유자가 관리합니다.
/// </summary>
public static class DefenseResolver
{
    public static DefenseType Resolve(DefenseType currentDefense, in DamageRequest request)
    {
        return currentDefense switch
        {
            DefenseType.NormalGuard when request.CanGuard => DefenseType.NormalGuard,
            DefenseType.Parry when request.CanDeflect => DefenseType.Parry,
            _ => DefenseType.None
        };
    }

    public static bool IsAttackFromFront(Vector3 forward, Vector3 hitDirection, float guardAngle)
    {
        // hitDirection은 공격자에서 피격자로 향하므로 정면 공격은 음수 내적으로 판정합니다.
        float dot = Vector3.Dot(forward, hitDirection);
        float halfAngle = guardAngle / 2f;
        float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot <= -threshold;
    }
}
