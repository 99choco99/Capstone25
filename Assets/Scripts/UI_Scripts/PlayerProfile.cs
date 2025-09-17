using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    [SerializeField] Player player;
    PlayerStats playerStats;

    [Header("UI ÄÄÆ÷³ÍÆ®")]
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private Button damageUpButton;
    [SerializeField] private Button healthUpButton;
    [SerializeField] private Button defesnseUpbutton;

    private void Start()
    {
        player = GetComponentInParent<Player>();
        playerStats = player.Stats;
        player.Stats.OnStatsChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (player.Stats != null)
        {
            playerStats.OnStatsChanged -= UpdateUI;
        }
    }


    public void UpdateUI()
    {
        abilityText.text = $"Point : {playerStats.AbilityPoint}";
        damageText.text = $"{playerStats.damage} (+ {playerStats.bonusDamage})";
        healthText.text = playerStats.maxHp + "(+" + playerStats.bonusmaxHp + ")";
        defenseText.text = playerStats.maxPosture + "(+" + playerStats.bonusDefense + ")";

        bool canUpgrade = playerStats.AbilityPoint > 0;
        damageUpButton.interactable = canUpgrade;
        healthUpButton.interactable = canUpgrade;
        defesnseUpbutton.interactable = canUpgrade;
    }

}
