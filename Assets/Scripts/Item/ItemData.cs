using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class ItemSpec
{
    public float damage;
    public float defense;
    public float speed;
    public float hp;

}

[CreateAssetMenu(fileName = "ItemSpec", menuName = "Scriptable Objects/ItemSpec")]
public class ItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public string script;
    public int count; //아이템 드랍될때 사용
    public Sprite icon;
    public SlotType type;
    public EquipmentType equipmentType;
    public ItemSpec spec;

}
