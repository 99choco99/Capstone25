using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private PlayerStats playerStats;

    [Header("상태 UI")]
    public Slider PlayerHpUI;
    public Slider PostureGauge;
    public Slider ExpUI;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI ExpText;

    private void Awake()
    {
        Player.OnLocalPlayerSpawned += Setup;
    }

    private void Start()
    {
        if (playerStats == null && Player.LocalPlayer != null)
            Setup(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Setup;
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnHpChanged -= UpdateHp;
            playerStats.OnPostureChanged -= UpdatePostureGauge;
            playerStats.OnExpChanged -= UpdateExp;
            playerStats.OnStatsChanged -= UpdateStats;
        }
    }

    public void Setup(Player player)
    {
        Unsubscribe();

        playerStats = player.Stats;

        playerStats.OnHpChanged += UpdateHp;
        playerStats.OnPostureChanged += UpdatePostureGauge;
        playerStats.OnExpChanged += UpdateExp;
        playerStats.OnStatsChanged += UpdateStats;

        // 초기값 세팅
        UpdateStats(playerStats);
        UpdateExp(playerStats.Exp, playerStats.Level);
    }

    // 능력치 투자로 최대 HP·체간이 바뀌었을 때도 표시를 갱신합니다.
    private void UpdateStats(PlayerStats stats)
    {
        UpdateHp(stats.CurrentHp, stats.MaxHp.GetValue());
        UpdatePostureGauge(stats.CurrentPosture, stats.MaxPosture.GetValue());
    }

    public void UpdateHp(float currenthp, float maxHp) => PlayerHpUI.value = currenthp / maxHp;

    public void UpdateExp(int exp, int level)
    {
        float maxExp = PlayerStats.GetMaxExpForLevel(level);
        ExpUI.normalizedValue = exp / maxExp;
        levelText.text = $"Lv. {level}";
        ExpText.text = $"{ExpUI.normalizedValue * 100:0.#}%";
    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {
        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }
}
