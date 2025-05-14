using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public GameObject DropStatePrefab;
    public GameObject OwnedStatePrefab;
    public ItemType type;
    public EquipmentType equipmentType;

    public float damage;
    public float defense;
    public float speed;
    public float hp;

    public string script;
}
