
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

        Player.OnLocalPlayerSpawned += Setup;
        if (Player.LocalPlayer != null) Setup(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Setup;
        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;
    }

    public void Setup(Player localPlayer)
    {
        if (localPlayer == null) return;

        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;

        canvasGroup.alpha = 0f;
        backgroundText.characterSpacing = 0f;

        player = localPlayer;
        player.Stats.OnDeath += StartDeathEffect;
    }

    public async void StartDeathEffect()
    {

        backgroundText.characterSpacing = 0;

        Awaitable fade = FadeInEffect();
        Awaitable stretch = StretchTextEffect();

        await fade;
        await stretch;

        await GameManager.Instance.Retry();
    }

    private async Awaitable FadeInEffect()
    {
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            await Awaitable.NextFrameAsync();
        }
        canvasGroup.alpha = 1f;
    }

    private async Awaitable StretchTextEffect()
    {
        float timer = 0f;

        while (timer < textStretchDuration)
        {
            timer += Time.deltaTime;
            backgroundText.characterSpacing = Mathf.Lerp(0, targetCharacterSpacing, timer / textStretchDuration);
            backgroundText.alpha = Mathf.Lerp(backgroundText.alpha, 1, timer / textStretchDuration);
            await Awaitable.NextFrameAsync();
        }
        backgroundText.characterSpacing = targetCharacterSpacing;
    }
}
