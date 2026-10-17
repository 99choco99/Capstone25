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
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        Player.OnLocalPlayerSpawned += Setup;
        if (Player.LocalPlayer != null)
            Setup(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Setup;
        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;
    }

    public void Setup(Player localPlayer)
    {
        if (localPlayer == null)
            return;

        if (player != null && player.Stats != null)
            player.Stats.OnDeath -= StartDeathEffect;

        canvasGroup.alpha = 0f;
        backgroundText.characterSpacing = 0f;

        player = localPlayer;
        player.Stats.OnDeath += StartDeathEffect;
    }

    public async void StartDeathEffect()
    {
        Player dyingPlayer = player;
        backgroundText.characterSpacing = 0;
        float timer = 0f;

        if (!(timer < fadeInDuration))
            canvasGroup.alpha = 1f;
        if (!(timer < textStretchDuration))
            backgroundText.characterSpacing = targetCharacterSpacing;

        while (timer < fadeInDuration || timer < textStretchDuration)
        {
            if (CancelObsoleteEffect(dyingPlayer))
                return;
            bool isFading = timer < fadeInDuration;
            bool isStretching = timer < textStretchDuration;
            timer += Time.deltaTime;

            if (isFading)
                canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            if (isStretching)
            {
                backgroundText.characterSpacing = Mathf.Lerp(0, targetCharacterSpacing, timer / textStretchDuration);
                backgroundText.alpha = Mathf.Lerp(backgroundText.alpha, 1, timer / textStretchDuration);
            }

            await Awaitable.NextFrameAsync();
        }

        if (CancelObsoleteEffect(dyingPlayer))
            return;
        canvasGroup.alpha = 1f;
        backgroundText.characterSpacing = targetCharacterSpacing;
        await GameManager.Instance.Retry();
    }

    private bool CancelObsoleteEffect(Player dyingPlayer)
    {
        if (dyingPlayer != null && player == dyingPlayer && !GameManager.Instance.IsChangingScene)
            return false;

        if (player == dyingPlayer || player == null)
            canvasGroup.alpha = 0f;
        return true;
    }
}
