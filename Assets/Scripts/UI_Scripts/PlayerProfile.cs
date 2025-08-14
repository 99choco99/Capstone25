using System;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    enum Ability { damage,hp,speed,defense};
    PlayerSetting player;
    public int AbilityPoint;
    [SerializeField] TextMeshProUGUI abilityText;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI damageText;
    [SerializeField] TextMeshProUGUI defenseText;

    private void Start()
    {
        player = GetComponentInParent<PlayerSetting>();
        player.OnStatsChanged += UpdateUI;
        UpdateUI();
    }


    public void UpdateUI()
    {
        abilityText.text = $"Point : { AbilityPoint}";
        damageText.text = player.damage + "(+" + player.D_damage + ")";
        healthText.text = player.maxHp + "(+" + player.D_health + ")";
        speedText.text = player.speed + "(+" + player.D_speed + ")";
        defenseText.text = player.defense + "(+" + player.D_defense + ")";
    }

    public void UpAbility(int selectIndex)
    {
        if (AbilityPoint <= 0) { return; }
        Ability selectAbility = (Ability)selectIndex;
        AbilityPoint--;
        switch (selectAbility) {
            case Ability.damage:
                player.damage++;
                break;
            case Ability.speed:
                player.speed++;
                break;
            case Ability.defense:
                player.defense++;
                break;
            case Ability.hp:
                player.maxHp++;
                break;
        }
        UpdateUI();
    }
}
