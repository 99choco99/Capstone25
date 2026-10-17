using System;
using UnityEngine;

/// <summary>체간 붕괴 구도의 진입·복귀와 높이·각도 보정. 카메라 반경과 입력 축은 소유하지 않습니다.</summary>
public sealed class PostureBreakCameraFraming
{
    [Serializable]
    public sealed class Settings
    {
        [InspectorName("체간 붕괴 구도 사용")] public bool Enabled = true;
        [InspectorName("구도 진입 시간 (실제 초)"), Min(0f)] public float EnterDuration = 0.14f;
        [InspectorName("구도 복귀 시간 (실제 초)"), Min(0f)] public float ExitDuration = 0.28f;
        [InspectorName("최소 시선 낮춤 (유닛)"), Min(0f)] public float MinimumAimDrop = 0.2f;
        [Tooltip("가슴이 낮아지는 양을 참고하되 이 범위까지만 시선을 내립니다.")]
        [InspectorName("최대 시선 낮춤 (유닛)"), Min(0f)] public float MaximumAimDrop = 0.5f;
        [InspectorName("낮은 구도의 목표 수직 각도 (도)")] public float LowPitch = -3f;
        [Tooltip("현재 구도에서 내릴 수 있는 최대 각도입니다. 이미 낮은 구도이면 올리지 않습니다.")]
        [InspectorName("최대 수직 각도 낮춤 (도)"), Min(0f)] public float MaximumPitchDrop = 22f;
    }

    private float blend;
    private float bodyAimDrop;

    public bool IsActive => blend > 0f;
    public float Weight => Mathf.SmoothStep(0f, 1f, blend);
    public float AimDrop => bodyAimDrop * Weight;

    public void Update(bool isCollapsed, float bodyHeightDrop, Settings settings, float deltaTime)
    {
        bool enter = isCollapsed && settings.Enabled;
        if (enter)
        {
            float maximum = Mathf.Max(0f, settings.MaximumAimDrop);
            float minimum = Mathf.Clamp(settings.MinimumAimDrop, 0f, maximum);
            bodyAimDrop = Mathf.Clamp(bodyHeightDrop, minimum, maximum);
        }

        float duration = enter ? settings.EnterDuration : settings.ExitDuration;
        float target = enter ? 1f : 0f;
        blend = duration <= 0f ? target : Mathf.MoveTowards(blend, target, Mathf.Max(0f, deltaTime) / duration);
        if (!IsActive)
            bodyAimDrop = 0f;
    }

    public float GetPitch(float normalPitch, Settings settings)
    {
        if (!IsActive)
            return normalPitch;
        float lowPitch = Mathf.Max(
            normalPitch - Mathf.Max(0f, settings.MaximumPitchDrop),
            Mathf.Min(normalPitch, settings.LowPitch));
        return Mathf.Lerp(normalPitch, lowPitch, Weight);
    }

    public void Reset()
    {
        blend = bodyAimDrop = 0f;
    }
}
