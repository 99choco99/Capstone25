using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
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
        PlayerStats.OnLocalPlayerStatsChanged += UpdateUI;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        PlayerStats.OnLocalPlayerStatsChanged -= UpdateUI;
    }


    public void UpdateUI(PlayerStats playerStats)
    {
        abilityText.text = $"Point : {playerStats.AbilityPoint}";
        string damageBonus = playerStats.bonusDamage > 0 ? $" (+ {playerStats.bonusDamage})" : "";
        damageText.text = $"{playerStats.damage}{damageBonus}";

        string healthBonus = playerStats.bonusMaxHp > 0 ? $" (+ {playerStats.bonusMaxHp})" : "";
        healthText.text = $"{playerStats.maxHp}{healthBonus}";

        string defenseBonus = playerStats.bonusDefense > 0 ? $" (+ {playerStats.bonusDefense})" : "";
        defenseText.text = $"{playerStats.maxPosture}{defenseBonus}";

        bool canUpgrade = playerStats.AbilityPoint > 0;
        damageUpButton.interactable = canUpgrade;
        healthUpButton.interactable = canUpgrade;
        defesnseUpbutton.interactable = canUpgrade;
    }

}
