using System.Collections;
using TMPro;
using UnityEngine;

public class DeathUI : MonoBehaviour
{
    Player player;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI backgroundText;

    [SerializeField] private float fadeInDuration = 4f;
    [SerializeField] private float textStretchDuration = 3f;
    [SerializeField] private float targetCharacterSpacing = 9f;



    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        Player.OnLocalPlayerSpawned += Init;
        if (Player.LocalPlayer != null) Init(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;
    }

    public void Init(Player localPlayer)
    {
        if (localPlayer == null) return;

        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;

        player = localPlayer;
        player.Stats.OnDeath += StartDeathEffect;
    }

    public void StartDeathEffect()
    {

        backgroundText.characterSpacing = 0;

        StartCoroutine(FadeInEffect());
        StartCoroutine(StretchTextEffect());
    }

    private IEnumerator FadeInEffect()
    {
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator StretchTextEffect()
    {
        float timer = 0f;

        while (timer < textStretchDuration)
        {
            timer += Time.deltaTime;
            backgroundText.characterSpacing = Mathf.Lerp(0, targetCharacterSpacing, timer / textStretchDuration);
            backgroundText.alpha = Mathf.Lerp(backgroundText.alpha, 1, timer / textStretchDuration);
            yield return null;
        }
        backgroundText.characterSpacing = targetCharacterSpacing;
    }
}
