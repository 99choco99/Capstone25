using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

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

        // 가드 가능 여부와 별개로 타격 강도를 반영하되, 연타 리듬을 해치지 않게 소폭 늘립니다.
        // 등급별 최종 시간은 CombatSettings에 저장되어 있습니다.
        TriggerHitStop(duration);
    }

    /// <summary>인살처럼 일반 피해 판정을 거치지 않는 연출에도 같은 히트 스톱을 사용합니다.</summary>
    public void TriggerHitStop(float duration)
    {
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
        Time.timeScale = Mathf.Min(timeScaleBeforeHitStop, Mathf.Clamp01(CombatSettings.Current.HitStopTimeScale));

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
