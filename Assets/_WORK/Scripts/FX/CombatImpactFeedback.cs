using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 확정된 DamageResult를 카메라 충격과 패드 진동으로 번역합니다.
///
/// 전투 씬은 싱글플레이이므로 한 번의 충돌을 로컬 카메라와 로컬 패드에 즉시 전달합니다.
/// 피해 계산·AI·State는 Cinemachine이나 InputSystem을 알 필요가 없고,
/// 이 클래스만 연출 장치에 의존하도록 경계를 둡니다.
/// </summary>
public sealed class CombatImpactFeedback : MonoBehaviour
{
    private const int CombatImpulseChannel = 1;

    private static CombatImpactFeedback instance;

    [Header("Cinemachine Impulse")]
    [SerializeField, Min(0f)] private float directHitStrength = 0.1f;
    [SerializeField, Min(0f)] private float guardStrength = 0.08f;
    [SerializeField, Min(0f)] private float parryStrength = 0.18f;
    [SerializeField, Min(0f)] private float specialStrength = 0.22f;
    [SerializeField, Min(0.01f)] private float impulseDuration = 0.12f;

    [Header("Gamepad Rumble")]
    [SerializeField, Range(0f, 1f)] private float directLowMotor = 0.18f;
    [SerializeField, Range(0f, 1f)] private float directHighMotor = 0.28f;
    [SerializeField, Range(0f, 1f)] private float clashLowMotor = 0.25f;
    [SerializeField, Range(0f, 1f)] private float clashHighMotor = 0.5f;
    [SerializeField, Min(0f)] private float rumbleDuration = 0.09f;

    private CinemachineImpulseSource impulseSource;
    private Coroutine rumbleRoutine;
    private Gamepad activeGamepad;
    private bool hasRegisteredCamera;

    /// <summary>
    /// 씬에 별도 오브젝트가 없어도 피드백이 빠지지 않도록 런타임 기본 인스턴스를 만듭니다.
    /// 나중에 Inspector에서 직접 튜닝한 인스턴스를 배치하면 그 인스턴스가 우선됩니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (instance != null)
            return;

        instance = FindAnyObjectByType<CombatImpactFeedback>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject feedbackObject = new("CombatImpactFeedback");
        instance = feedbackObject.AddComponent<CombatImpactFeedback>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // 카메라 같은 다른 시스템 오브젝트에 실수로 중복 부착돼도
            // 호스트 전체를 지우지 않고 중복 컴포넌트만 제거합니다.
            Destroy(this);
            return;
        }

        instance = this;

        // 런타임이 만든 빈 전용 오브젝트나 사용자가 만든 전용 Manager 루트만 유지합니다.
        // CameraSystem 등에 직접 붙인 경우 그 카메라 계층을 DontDestroy로 바꾸지 않습니다.
        bool isDedicatedManagerRoot =
            transform.parent == null
            && GetComponents<Component>().Length <= 2;
        if (isDedicatedManagerRoot)
            DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void OnEnable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopRumble();
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        StopRumble();
        instance = null;
    }

    private void Initialize()
    {
        bool createdImpulseSource = false;
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                createdImpulseSource = true;
            }
        }

        // Inspector에서 직접 조율한 Source가 있으면 그 정의를 보존합니다.
        // 런타임에 Source를 새로 만든 경우에만 안전한 기본 파형을 채웁니다.
        if (createdImpulseSource)
        {
            impulseSource.ImpulseDefinition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = CombatImpulseChannel,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                ImpulseDuration = impulseDuration,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                DissipationDistance = 100f,
                DissipationRate = 0.25f,
                PropagationSpeed = 343f
            };
            impulseSource.DefaultVelocity = Vector3.down;
        }

        EnsureCameraListeners();
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        hasRegisteredCamera = false;
        EnsureCameraListeners();
    }

    /// <summary>
    /// DamageResult 하나당 한 번 호출합니다.
    /// 패링 반환 체간은 전용 경로를 사용해 DamageResult를 만들지 않으므로 검 충돌이 중복 재생되지 않습니다.
    /// </summary>
    public static void Trigger(in DamageResult result)
    {
        EnsureRuntimeInstance();
        if (instance != null)
            instance.Play(result);
    }

    private void Play(in DamageResult result)
    {
        // 일반 씬 로드는 HandleSceneLoaded가 처리합니다.
        // 카메라가 더 늦게 생성된 특수한 씬에서만 최초 충돌 때 한 번 재탐색합니다.
        if (!hasRegisteredCamera)
            EnsureCameraListeners();

        float strength;
        float lowMotor;
        float highMotor;

        if (result.DefenseType == DefenseType.Parry)
        {
            strength = parryStrength;
            lowMotor = clashLowMotor;
            highMotor = clashHighMotor;
        }
        else if (result.DefenseType == DefenseType.NormalGuard)
        {
            strength = guardStrength;
            lowMotor = directLowMotor;
            highMotor = clashHighMotor * 0.7f;
        }
        else
        {
            strength = result.Request.CanGuard ? directHitStrength : specialStrength;
            lowMotor = directLowMotor;
            highMotor = result.Request.CanGuard ? directHighMotor : clashHighMotor;
        }

        if (impulseSource != null && strength > 0f)
        {
            Vector3 cameraSpaceKick = new(
                Random.Range(-0.12f, 0.12f),
                -1f,
                Random.Range(-0.05f, 0.05f));
            impulseSource.GenerateImpulseAtPositionWithVelocity(
                result.HitPoint,
                cameraSpaceKick.normalized * strength);
        }

        StartRumble(lowMotor, highMotor);
    }

    /// <summary>
    /// 현재 씬의 CinemachineCamera가 전투 채널을 듣도록 보장합니다.
    /// 프리캠과 락온이 같은 카메라를 공유하므로 두 모드의 궤도·반경은 건드리지 않습니다.
    /// </summary>
    private void EnsureCameraListeners()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        hasRegisteredCamera = cameras.Length > 0;

        foreach (CinemachineCamera camera in cameras)
        {
            if (camera == null) continue;

            CinemachineImpulseListener listener =
                camera.GetComponent<CinemachineImpulseListener>();

            if (listener == null)
            {
                listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
                listener.ApplyAfter = CinemachineCore.Stage.Noise;
                listener.ChannelMask = CombatImpulseChannel;
                listener.Gain = 1f;
                listener.Use2DDistance = false;
                listener.UseCameraSpace = true;
            }
            else
            {
                // 기존 컷신/카메라 설정은 보존하고 전투 채널만 추가합니다.
                listener.ChannelMask |= CombatImpulseChannel;
            }
        }
    }

    private void StartRumble(float lowMotor, float highMotor)
    {
        Gamepad gamepad = ResolveLocalGamepad();
        if (gamepad == null || rumbleDuration <= 0f)
            return;

        StopRumble();

        activeGamepad = gamepad;
        activeGamepad.SetMotorSpeeds(
            Mathf.Clamp01(lowMotor),
            Mathf.Clamp01(highMotor));
        rumbleRoutine = StartCoroutine(StopRumbleAfterDelay());
    }

    private IEnumerator StopRumbleAfterDelay()
    {
        yield return new WaitForSecondsRealtime(rumbleDuration);
        StopRumble();
    }

    private void StopRumble()
    {
        if (rumbleRoutine != null)
        {
            StopCoroutine(rumbleRoutine);
            rumbleRoutine = null;
        }

        activeGamepad?.ResetHaptics();
        activeGamepad = null;
    }

    private static Gamepad ResolveLocalGamepad()
    {
        if (Player.LocalPlayer != null && Player.LocalPlayer.InputHandler != null)
        {
            PlayerInput playerInput =
                Player.LocalPlayer.InputHandler.GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                foreach (InputDevice device in playerInput.devices)
                {
                    if (device is Gamepad gamepad)
                        return gamepad;
                }
            }
        }

        return Gamepad.current;
    }
}
