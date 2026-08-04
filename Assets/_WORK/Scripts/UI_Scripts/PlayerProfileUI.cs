using System;
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


    public override void SetUp(Player localPlayer)
    {
        PlayerStats Stats = localPlayer.Stats;
        healthUpButton.onClick.RemoveAllListeners();
        postureUpbutton.onClick.RemoveAllListeners();
        if (attackPowerUpButton != null)
            attackPowerUpButton.onClick.RemoveAllListeners();

        if (stats != null)
        {
            stats.OnStatsChanged -= UpdateUI;
        }

        stats = Stats;
        stats.OnStatsChanged += UpdateUI;

        healthUpButton.onClick.AddListener(() => { stats.UpAbility(PlayerStatType.Health); });
        postureUpbutton.onClick.AddListener(() => stats.UpAbility(PlayerStatType.MaxPosture));
        if (attackPowerUpButton != null)
            attackPowerUpButton.onClick.AddListener(() => stats.UpAbility(PlayerStatType.AttackPower));

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

        RefreshStatText(healthText, playerStats.MaxHp);
        RefreshStatText(postureText, playerStats.MaxPosture);
        RefreshStatText(attackPowerText, playerStats.AttackPower);

        bool canUpgrade = playerStats.AbilityPoint > 0;
        healthUpButton.interactable = canUpgrade;
        postureUpbutton.interactable = canUpgrade;
        if (attackPowerUpButton != null)
            attackPowerUpButton.interactable = canUpgrade;
    }


    private void RefreshStatText(TextMeshProUGUI textUI, Stat stat)
    {
        if (textUI == null || stat == null) return;
        float bonus = stat.GetValue() - stat.GetBaseValue();
        string bonusStr = bonus > 0 ? $" (+ {bonus})" : "";
        textUI.text = $"{stat.GetValue()}{bonusStr}";
    }
}
