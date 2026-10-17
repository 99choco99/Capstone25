using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private Coroutine hitStopRoutine;
    private float timeScaleBeforeHitStop = 1f;
    private float hitStopEndTime;
    private Object cinematicOwner;
    private float timeScaleBeforeCinematic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// DamageResult 로 hitstop
    /// </summary>
    public void HitStop(in DamageResult result)
    {
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(result.Request);
        HitStop(reaction.GetHitStopDuration(result));
    }

    /// <summary>duration 으로 hitstop</summary>
    public void HitStop(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f)
            return;

        float endTime = Time.unscaledTime + duration;
        if (hitStopRoutine != null)
        {
            hitStopEndTime = Mathf.Max(hitStopEndTime, endTime);
            return;
        }

        timeScaleBeforeHitStop = Time.timeScale;
        hitStopEndTime = endTime;
        hitStopRoutine = StartCoroutine(HitStopRoutine());
    }

    /// <summary>
    /// 실제 히트스톱을 작동시킬 곳
    /// </summary>
    private IEnumerator HitStopRoutine()
    {
        float targetTimeScale = Mathf.Min(
            timeScaleBeforeHitStop,
            Mathf.Clamp01(CombatSettings.Current.HitStopTimeScale));
        Time.timeScale = targetTimeScale;

        while (Time.unscaledTime < hitStopEndTime)
        {
            yield return null;
        }

        ResetTimeScale();
    }

    private void ResetTimeScale()
    {
        if (hitStopRoutine == null)
            return;

        Time.timeScale = timeScaleBeforeHitStop;
        hitStopRoutine = null;
    }

    private void CancelHitStop()
    {
        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);
        ResetTimeScale();
    }

    /// <summary>히트스톱과 같은 복귀 경로를 사용해 보스 연출의 시간 배율을 유지합니다.</summary>
    public bool BeginCinematic(Object owner, float timeScale)
    {
        if (owner == null || !isActiveAndEnabled || cinematicOwner != null)
            return false;
        timeScaleBeforeCinematic = hitStopRoutine != null ? timeScaleBeforeHitStop : Time.timeScale;
        cinematicOwner = owner;
        SetCinematicTimeScale(owner, timeScale);
        return true;
    }

    public void SetCinematicTimeScale(Object owner, float timeScale)
    {
        if (cinematicOwner != owner || owner == null)
            return;
        ApplyTimeScale(Mathf.Clamp(timeScale, 0.01f, 1f));
    }

    public void EndCinematic(Object owner)
    {
        if (cinematicOwner != owner || owner == null)
            return;
        cinematicOwner = null;
        ApplyTimeScale(timeScaleBeforeCinematic);
    }

    private void ApplyTimeScale(float timeScale)
    {
        if (hitStopRoutine != null)
        {
            timeScaleBeforeHitStop = timeScale;
            Time.timeScale = Mathf.Min(timeScale, Mathf.Clamp01(CombatSettings.Current.HitStopTimeScale));
        }
        else
        {
            Time.timeScale = timeScale;
        }
    }

    private void OnDisable()
    {
        CancelHitStop();
        if (cinematicOwner != null)
            EndCinematic(cinematicOwner);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;
        CancelHitStop();
        if (cinematicOwner != null)
            EndCinematic(cinematicOwner);
        Instance = null;
    }
}
