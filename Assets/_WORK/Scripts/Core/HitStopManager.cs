using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("히트 스톱")]
    [Tooltip("히트 스톱 중 적용할 시간 배율")]
    [SerializeField] private float hitStopTimeScale = 0.05f;
    [Tooltip("방어하지 못하고 직접 맞았을 때의 실제 정지 시간")]
    [SerializeField, Min(0f)] private float directHitStopDuration = 0.025f;
    [Tooltip("가드 불가능한 특수 공격에 직접 맞았을 때의 실제 정지 시간")]
    [SerializeField, Min(0f)] private float specialHitStopDuration = 0.04f;
    [Tooltip("일반 가드 충돌이 멈춰 보이는 실제 시간")]
    [SerializeField, Min(0f)] private float guardHitStopDuration = 0.03f;
    [Tooltip("패링 충돌이 멈춰 보이는 실제 시간")]
    [SerializeField, Min(0f)] private float parryHitStopDuration = 0.055f;
    private Coroutine hitStopRoutine;
    private float timeScaleBeforeHitStop = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 최종 피해 결과에 맞는 실제 시간 동안 게임을 짧게 멈춥니다.
    /// </summary>
    public void TriggerHitStop(in DamageResult result)
    {
        float duration;
        if (result.DefenseType == DefenseType.Parry)
            duration = parryHitStopDuration;
        else if (result.DefenseType == DefenseType.NormalGuard)
            duration = guardHitStopDuration;
        else if (!result.Request.CanGuard)
            duration = specialHitStopDuration;
        else
            duration = directHitStopDuration;

        if (duration <= 0f) return;

        bool wasAlreadyStopping = hitStopRoutine != null;
        if (wasAlreadyStopping)
            StopCoroutine(hitStopRoutine);

        // 연속 충돌로 정지가 갱신될 때 0.05 같은 정지 중 배율을 "원래 값"으로 덮어쓰지 않습니다.
        if (!wasAlreadyStopping)
            timeScaleBeforeHitStop = Time.timeScale;

        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = Mathf.Min(timeScaleBeforeHitStop, Mathf.Clamp01(hitStopTimeScale));

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = timeScaleBeforeHitStop;
        hitStopRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (hitStopRoutine != null)
            Time.timeScale = timeScaleBeforeHitStop;

        Instance = null;
    }
}
