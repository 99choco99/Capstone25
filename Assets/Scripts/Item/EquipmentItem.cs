using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public enum EquipmentType { None, helmet, top, bottom, shoes, sword, shield }
public class EquipmentItem : OwnedItem
{
    public void Equip(PlayerSetting player)
    {
        player.ApplyStatChanges(data.damage, data.hp, data.defense, data.speed);
    }

    public void TakeOff(PlayerSetting player)
    {
        player.ApplyStatChanges(-data.damage, -data.hp, -data.defense, -data.speed);
    }

}
