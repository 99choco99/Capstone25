using UnityEngine;

public class DropItem : Item, IInteractable
{
    public void Interact(Transform player)
    {
        Slot slot = InventoryManager.instance.FindEmptySlot(data.type);

        slot.itemCount += data.count;
        Destroy(gameObject);
    }
}
