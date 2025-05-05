using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentItemData", menuName = "Scriptable Objects/EquipmentItemData")]
public class EquipmentItemData : ScriptableObject
{
    public EquipmentType type;
    public float damage;
    public float defense;
    public float speed;
    public float hp;
}
