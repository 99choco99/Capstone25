using UnityEngine;


public enum EquipmentType { None, helmet, top, bottom, shoes, sword, shield }
public class EquipmentItem : InventoryItem
{
    public EquipmentItemData data;
    public override void Apply(PlayerData player)
    {
        player.damage += data.damage;
        player.maxHp += data.hp;
        player.defense += data.defense;
    }

    public void takeOff(PlayerData player)
    {

    }

    public override EquipmentType GetEquipmentType()
    {
        return data.type;
    }
}
