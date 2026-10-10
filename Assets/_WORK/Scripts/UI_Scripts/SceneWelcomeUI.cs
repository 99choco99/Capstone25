using System.Collections;
using UnityEngine;

public class SceneWelcomeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup veil;
    [SerializeField] private CanvasGroup caption;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.45f;
    [SerializeField, Min(0f)] private float captionHoldDuration = 3f;
    [SerializeField, Min(0.01f)] private float captionFadeDuration = 0.8f;

    private IEnumerator Start()
    {
        veil.blocksRaycasts = false;
        veil.interactable = false;
        caption.blocksRaycasts = false;
        caption.interactable = false;
        veil.alpha = 1f;
        caption.alpha = 0f;

        yield return null;
        yield return Fade(veil, 1f, 0f, revealDuration);
        veil.gameObject.SetActive(false);
        yield return Fade(caption, 0f, 1f, revealDuration);
        yield return new WaitForSecondsRealtime(captionHoldDuration);
        yield return Fade(caption, 1f, 0f, captionFadeDuration);
        caption.gameObject.SetActive(false);
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }
}
