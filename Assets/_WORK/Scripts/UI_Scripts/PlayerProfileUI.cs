using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PlayerProfileUI : UIBase
{
    [Header("UI ÄÄÆ÷³ÍÆ®")]
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI postureText;
    [SerializeField] private Button AttackPowerUpButton;
    [SerializeField] private Button healthUpButton;
    [SerializeField] private Button defesnseUpbutton;
    [SerializeField] private Button postureUpbutton;

    private PlayerStats stats;


    public override void SetUp(Player localPlayer)
    {
        PlayerStats Stats = localPlayer.Stats;
        AttackPowerUpButton.onClick.RemoveAllListeners();
        healthUpButton.onClick.RemoveAllListeners();
        defesnseUpbutton.onClick.RemoveAllListeners();
        postureUpbutton.onClick.RemoveAllListeners();

        if (stats != null)
        {
            stats.OnStatsChanged -= UpdateUI;
        }

        stats = Stats;
        stats.OnStatsChanged += UpdateUI;

        AttackPowerUpButton.onClick.AddListener(() => { stats.UpAbility(PlayerStatType.AttackPower); });
        healthUpButton.onClick.AddListener(() => { stats.UpAbility(PlayerStatType.Health); });
        defesnseUpbutton.onClick.AddListener(() => { stats.UpAbility(PlayerStatType.Defense); });
        postureUpbutton.onClick.AddListener(() => stats.UpAbility(PlayerStatType.MaxPosture));

        UpdateUI(stats);
    }

    private void OnDestroy()
    {
        if(stats != null)
        {
            stats.OnStatsChanged -= UpdateUI;
        }
    }

    public void UpdateUI(PlayerStats playerStats)
    {
        if(playerStats == null) { return; }
        abilityText.text = $"Point : {playerStats.AbilityPoint}";

        RefreshStatText(attackPowerText, playerStats.AttackPower);
        RefreshStatText(healthText, playerStats.MaxHp);
        RefreshStatText(defenseText, playerStats.Defense);
        RefreshStatText(postureText, playerStats.MaxPosture);

        bool canUpgrade = playerStats.AbilityPoint > 0;
        AttackPowerUpButton.interactable = canUpgrade;
        healthUpButton.interactable = canUpgrade;
        defesnseUpbutton.interactable = canUpgrade;
        postureUpbutton.interactable = canUpgrade;
    }


    private void RefreshStatText(TextMeshProUGUI textUI, Stat stat)
    {
        if (textUI == null || stat == null) return;
        float bonus = stat.GetValue() - stat.GetBaseValue();
        string bonusStr = bonus > 0 ? $" (+ {bonus})" : "";
        textUI.text = $"{stat.GetValue()}{bonusStr}";
    }
}
