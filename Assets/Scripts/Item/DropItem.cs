using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class DropItem : Item, IInteractable
{
    public void Interact(Transform player)
    {
        ItemSlot slot = InventoryManager.instance.FindEmptySlot(data.type);
        slot.currentItem = Instantiate(data.OwnedStatePrefab, slot.transform).GetComponent<OwnedItem>();
        Destroy(gameObject);
    }
}
