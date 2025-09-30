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
    public Sprite icon;
    public SlotType type;
    public EquipmentType equipmentType;
    //public ItemSpec DefaultSpec;

}
