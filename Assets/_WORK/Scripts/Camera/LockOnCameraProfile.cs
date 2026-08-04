using UnityEngine;


/// <summary>
/// 락온 대상별 카메라 구도와 추적 반응을 정의합니다.
/// 프로필이 지정되지 않은 대상은 SekiroCamera의 기본 설정을 사용합니다.
/// Radius, FOV, Yaw/Pitch 허용 범위는 의도적으로 포함하지 않습니다.
/// 대상별 프로필도 FreeCam의 고정 구면 궤도 안에서만 구도를 조절해야 하기 때문입니다.
/// </summary>
[CreateAssetMenu(
    fileName = "LockOnCameraProfile",
    menuName = "Scriptable Objects/Camera/Lock On Camera Profile")]
public sealed class LockOnCameraProfile : ScriptableObject
{
    [Header("구도")]
    [SerializeField, Min(0f)] private float playerAimHeight = 1.3f;
    [Tooltip("고정 궤도 안에서 LookAt 지점을 플레이어에서 적 쪽으로 옮기는 비율")]
    [SerializeField, Range(0f, 1f)] private float targetFramingWeight = 0.15f;
    [Tooltip("화면에서 적 LockPoint가 플레이어 Aim보다 위에 놓일 목표 각도. Renderer 전체 Bounds를 보장하는 값은 아닙니다.")]
    [SerializeField, Range(0f, 30f)] private float enemyAngleAbovePlayer = 9.5f;

    [Header("추적")]
    [SerializeField, Range(0f, 10f)] private float yawPlayAngle = 1f;
    [SerializeField, Range(0f, 10f)] private float pitchPlayAngle = 1f;
    [SerializeField, Min(0f)] private float yawResponse = 8f;
    [SerializeField, Min(0f)] private float pitchResponse = 8f;
    [SerializeField, Min(0f)] private float maxYawSpeed = 240f;
    [SerializeField, Min(0f)] private float maxPitchSpeed = 90f;

    [Header("전환")]
    [Tooltip("다른 락온 프로필 또는 자유 카메라 설정으로 전환할 때의 보간 시간")]
    [SerializeField, Min(0f)] private float blendDuration = 0.25f;

    public float PlayerAimHeight => playerAimHeight;
    public float TargetFramingWeight => targetFramingWeight;
    public float EnemyAngleAbovePlayer => enemyAngleAbovePlayer;
    public float YawPlayAngle => yawPlayAngle;
    public float PitchPlayAngle => pitchPlayAngle;
    public float YawResponse => yawResponse;
    public float PitchResponse => pitchResponse;
    public float MaxYawSpeed => maxYawSpeed;
    public float MaxPitchSpeed => maxPitchSpeed;
    public float BlendDuration => blendDuration;
}


/// <summary>
/// 카메라 프로필을 제공할 수 있는 락온 대상만 선택적으로 구현합니다.
/// </summary>
public interface ILockOnCameraProfileProvider
{
    LockOnCameraProfile LockOnCameraProfile { get; }
}
