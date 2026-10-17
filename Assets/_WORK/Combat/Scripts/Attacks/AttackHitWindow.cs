using System;
using UnityEngine;

public enum WeaponSlot
{
    Any,
    MainHand,
    OffHand
}

/// <summary>한 모션 안에서 한 번 타격할 무기와 판정 구간을 지정합니다.</summary>
[Serializable]
public sealed class AttackHitWindow
{
    [InspectorName("사용 무기")] public WeaponSlot weapon;
    [InspectorName("판정 시작 프레임"), Min(0f)] public float startFrame;
    [InspectorName("판정 종료 프레임"), Min(0f)] public float endFrame;
    [InspectorName("체력 피해 배율"), Min(0f)] public float damageMultiplier = 1f;
    [InspectorName("체간 피해 배율"), Min(0f)] public float postureMultiplier = 1f;

    public bool Includes(float frame, WeaponSlot slot) => frame >= startFrame && frame < endFrame
        && (weapon == WeaponSlot.Any || weapon == slot);
}
