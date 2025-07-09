using UnityEngine;

public class DropItem : Item, IInteractable
{
    public void Interact(Transform player)
    {
        Slot slot = InventoryManager.instance.FindEmptySlot(data.type);
        slot.currentItem = Instantiate(data.OwnedStatePrefab, slot.transform).GetComponent<OwnedItem>();
        slot.itemCount += data.count;
        Destroy(gameObject);
    }
}
