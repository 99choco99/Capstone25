using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 인살(忍殺) 전용 시네마틱 카메라.
///
/// PlayerExecution이 인살을 시작하면(OnExecuteStart) 이 vcam의 우선순위를 올려 살아있는 카메라가 된다.
/// → CinemachineBrain이 SekiroCamera에서 이 카메라로 '블렌드 인' = 화면상 카메라가 확 들어가는 "돌입".
/// 인살이 끝나면(OnExecuteEnd) 우선순위를 내려 SekiroCamera로 다시 블렌드 = "풀백".
///
/// 홀드 동안 피격자(적)를 기준으로 프레이밍하며, 살짝 밀어넣어(push-in) 극적인 느낌을 준다.
/// 고정반경 규칙(SekiroCamera)을 일시적으로 벗어나는 유일한 구간이다.
///
/// 셋업: 빈 GameObject + CinemachineCamera 하나. 절차적 Body/Aim은 붙이지 않는다(트랜스폼으로 직접 제어).
///       activePriority는 SekiroCamera vcam의 Priority보다 높게, idlePriority는 낮게 둘 것.
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CinemachineCamera))]
public class DeathblowCamera : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("인살을 재생하는 PlayerExecution. 비우면 런타임에 Player에서 찾는다.")]
    [SerializeField] private PlayerExecution execution;

    [Header("우선순위")]
    [Tooltip("인살 중 우선순위 (SekiroCamera vcam보다 높게)")]
    [SerializeField] private int activePriority = 100;
    [Tooltip("평상시 우선순위 (SekiroCamera vcam보다 낮게)")]
    [SerializeField] private int idlePriority = 0;

    [Header("샷 구도 (피격자 기준)")]
    [Tooltip("피격자 정면 방향으로 떨어질 거리 (양수=적 앞에서 잡음)")]
    [SerializeField] private float distanceFront = 2.0f;
    [Tooltip("옆으로 치우칠 거리 (3/4 앵글)")]
    [SerializeField] private float distanceSide = 1.3f;
    [Tooltip("카메라 높이")]
    [SerializeField] private float height = 1.3f;
    [Tooltip("바라볼 지점 높이(피격자 발 기준, 가슴쯤)")]
    [SerializeField] private float lookHeight = 1.2f;
    [Tooltip("홀드 동안 초당 밀어넣는 거리(dolly). 0이면 고정.")]
    [SerializeField] private float pushInPerSecond = 0.4f;

    private CinemachineCamera cam;
    private Transform attacker;   // 공격자(플레이어)
    private Transform victim;     // 피격자(적) — 프레이밍 기준
    private bool active;
    private float elapsed;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        cam.Priority = idlePriority;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += TryBindFromPlayer;
        Subscribe();
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= TryBindFromPlayer;
        Unsubscribe();
    }

    private void TryBindFromPlayer(Player p)
    {
        if (execution != null || p == null) return;
        execution = p.Execution;
        Subscribe();
    }

    private void Subscribe()
    {
        if (execution == null) return;
        execution.OnExecuteStart += Begin;
        execution.OnExecuteEnd += End;
    }

    private void Unsubscribe()
    {
        if (execution == null) return;
        execution.OnExecuteStart -= Begin;
        execution.OnExecuteEnd -= End;
    }

    private void Begin(Transform attackerTransform, Transform victimTransform)
    {
        attacker = attackerTransform;
        victim = victimTransform;
        elapsed = 0f;
        active = true;

        PositionCamera(0f);          // 첫 프레임 위치를 먼저 잡고
        cam.Priority = activePriority; // 우선순위 ↑ → Brain이 이 카메라로 블렌드(돌입)
    }

    private void End()
    {
        active = false;
        cam.Priority = idlePriority;   // 우선순위 ↓ → SekiroCamera로 블렌드(풀백)
    }

    private void LateUpdate()
    {
        if (!active || victim == null) return;
        elapsed += Time.deltaTime;
        PositionCamera(elapsed);
    }

    /// <summary>피격자를 기준으로 3/4 앵글에서 잡고, 시간에 따라 살짝 밀어넣는다.</summary>
    private void PositionCamera(float t)
    {
        // 피격자가 바라보는 수평 방향을 기준축으로. (백스탭이면 공격자는 뒤에 있으므로 적의 '앞'을 잡게 됨)
        Vector3 fwd = victim.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        float front = Mathf.Max(0.3f, distanceFront - pushInPerSecond * t); // 밀어넣되 너무 붙지 않게
        Vector3 pos = victim.position
                    + fwd * front
                    + right * distanceSide
                    + Vector3.up * height;

        Vector3 look = victim.position + Vector3.up * lookHeight;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation((look - pos).normalized, Vector3.up);
    }

    /// <summary>이벤트 없이 직접 구동하고 싶을 때(테스트 등).</summary>
    public void Play(Transform attackerTransform, Transform victimTransform) => Begin(attackerTransform, victimTransform);
    public void Stop() => End();
}
