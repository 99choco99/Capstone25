using UnityEngine;
using UnityEngine.UI;


[CreateAssetMenu(fileName = "ItemSpec", menuName = "Scriptable Objects/ItemSpec")]
public class ItemBase : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public SlotType type;

    [TextArea(3, 5)] 
    public string description;

}
[System.Serializable]
public struct ItemSpec
{
    public float attackPower;
    public float defense;
    public float maxHp;
    public float posture;
}

public enum EquipmentType { Helmet, Top, Bottom, Shoes, Gloves, Accessory }
[CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Item/EquipmentItem")]
public class EquipmentItemData : ItemBase
{
    public EquipmentType equipmentType; // Helmet, Top, Weapon 등
    public ItemSpec baseStats;
}

[CreateAssetMenu(fileName = "Consumption", menuName = "Scriptable Objects/Item/ConsumptionItem")]
public class ConsumptionItemData : ItemBase
{
    public float healAmount;    // 회복량
    public float duration;      // 지속시간
    public float coolTime;      // 쿨타임
}
