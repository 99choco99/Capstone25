using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public enum EquipmentType { None, helmet, top, bottom, shoes, sword, shield }
public class EquipmentItem : OwnedItem
{
    public void Equip(PlayerStats player)
    {
        player.ApplyStatChanges();
    }

    public void TakeOff(PlayerStats player)
    {
        player.ApplyStatChanges();
    }
}
