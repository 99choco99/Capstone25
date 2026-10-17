using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PlayerProfileUI : UIBase
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI postureText;
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private Button healthUpButton;
    [SerializeField] private Button postureUpbutton;
    [SerializeField] private Button attackPowerUpButton;

    private PlayerStats stats;

    public override void Init()
    {
        healthUpButton.onClick.AddListener(() => UpgradeStat(PlayerStatType.Health));
        postureUpbutton.onClick.AddListener(() => UpgradeStat(PlayerStatType.MaxPosture));
        attackPowerUpButton.onClick.AddListener(() => UpgradeStat(PlayerStatType.AttackPower));
    }

    public override void SetUp(Player localPlayer)
    {
        PlayerStats Stats = localPlayer.Stats;

        if (stats != null)
        {
            stats.OnStatsChanged -= UpdateUI;
        }

        stats = Stats;
        stats.OnStatsChanged += UpdateUI;

        UpdateUI(stats);
    }

    private void UpgradeStat(PlayerStatType statType)
    {
        stats.UpAbility(statType);
        GameManager.Instance.SaveGame();
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnStatsChanged -= UpdateUI;
        }
    }

    public void UpdateUI(PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return;
        }
        abilityText.text = $"Point : {playerStats.AbilityPoint}";

        RefreshStatText(healthText, playerStats.MaxHp);
        RefreshStatText(postureText, playerStats.MaxPosture);
        RefreshStatText(attackPowerText, playerStats.AttackPower);

        healthUpButton.interactable = playerStats.CanUpgradeStat(PlayerStatType.Health);
        postureUpbutton.interactable = playerStats.CanUpgradeStat(PlayerStatType.MaxPosture);
        attackPowerUpButton.interactable = playerStats.CanUpgradeStat(PlayerStatType.AttackPower);
    }

    private void RefreshStatText(TextMeshProUGUI textUI, Stat stat)
    {
        if (textUI == null || stat == null)
            return;
        textUI.text = $"{stat.GetValue()}";
    }
}
