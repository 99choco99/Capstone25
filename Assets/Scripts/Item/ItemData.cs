using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class ItemSpec
{
    public float damage;
    public float defense;
    public float speed;
    public float hp;
    public float duration;      // 지속시간
    public float coolTime;      // 쿨타임
}

[CreateAssetMenu(fileName = "ItemSpec", menuName = "Scriptable Objects/ItemSpec")]
public class ItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public string script;
    public Sprite icon;
    public SlotType type;
    public EquipmentType equipmentType;
    public ItemSpec baseStats;

}
