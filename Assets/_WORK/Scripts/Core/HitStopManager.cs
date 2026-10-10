using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private Coroutine hitStopRoutine;
    private float timeScaleBeforeHitStop = 1f;
    private float hitStopEndTime;

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
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(result.Request.KnockBackLevel);
        float duration;

        if (result.DefenseType == DefenseType.Parry)
            duration = reaction.ParryHitStop;
        else if (result.DefenseType == DefenseType.NormalGuard)
            duration = reaction.GuardHitStop;
        else if (!result.Request.CanGuard)
            duration = reaction.SpecialHitStop;
        else
            duration = reaction.DirectHitStop;

        HitStop(duration);
    }

    /// <summary>duration 으로 hitstop</summary>
    public void HitStop(float duration)
    {
        if (duration <= 0f) return;

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
        float targetTimeScale = Mathf.Min(timeScaleBeforeHitStop, Mathf.Clamp01(CombatSettings.Current.HitStopTimeScale));
        Time.timeScale = targetTimeScale;

        while (Time.unscaledTime < hitStopEndTime)
        {
            yield return null;
        }

        ResetTimeScale();
    }

    private void ResetTimeScale()
    {
        if (hitStopRoutine == null) return;

        Time.timeScale = timeScaleBeforeHitStop;
        hitStopRoutine = null;
    }

    private void OnDisable()
    {
        ResetTimeScale();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        ResetTimeScale();
        Instance = null;
    }
}
