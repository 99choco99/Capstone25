using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public enum EquipmentType { None, helmet, top, bottom, shoes, sword, shield }
public class EquipmentItem : OwnedItem
{
    public override void Apply(PlayerData player)
    {
        player.damage += data.damage;
        player.maxHp += data.hp;
        player.defense += data.defense;
    }

    public void takeOff(PlayerData player)
    {

    }

}
