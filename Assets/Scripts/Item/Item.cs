using UnityEngine;
public enum ItemType { Equipment, Consumption, Other }
public abstract class Item : MonoBehaviour
{
    public ItemType itemType;
}
