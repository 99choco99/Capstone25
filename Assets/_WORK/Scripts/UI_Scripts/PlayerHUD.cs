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
    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Setup;
        if (playerStats != null)
        {
            playerStats.OnHpChanged -= UpdateHp;
            playerStats.OnPostureChanged -= UpdatePostureGauge;
            playerStats.OnExpChanged -= UpdateExp;
        }
    }

    public void Setup(Player player)
    {
        if (playerStats != null)
        {
            playerStats.OnHpChanged -= UpdateHp;
            playerStats.OnPostureChanged -= UpdatePostureGauge;
            playerStats.OnExpChanged -= UpdateExp;
        }

        playerStats = player.Stats;

        playerStats.OnHpChanged += UpdateHp;
        playerStats.OnPostureChanged += UpdatePostureGauge;
        playerStats.OnExpChanged += UpdateExp;

        // 초기값 세팅
        UpdateHp(playerStats.CurrentHp, playerStats.MaxHp.GetValue());
        UpdatePostureGauge(playerStats.CurrentPosture, playerStats.MaxPosture.GetValue());
        UpdateExp(playerStats.Exp, playerStats.Level);
    }

    public void UpdateHp(float currenthp, float maxHp) => PlayerHpUI.value = currenthp / maxHp;
    public void UpdateExp(int exp, int level)
    {
        float maxExp = DataManager.Instance.GetMaxExpForLevel(level);
        ExpUI.value = exp / maxExp;
        levelText.text = $"Lv. {level}";
        ExpText.text = $"{(ExpUI.value / ExpUI.maxValue) * 100}%";
    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {
        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }

}
