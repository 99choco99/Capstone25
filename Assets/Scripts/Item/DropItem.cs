using UnityEngine;

public class DropItem : Item, IInteractable
{
    public void Interact(PlayerController player)
    {
        SlotData slot = InventoryManager.instance.FindEmptySlot(data.type);

        slot.itemCount += data.count;
        Destroy(gameObject);
    }
}
