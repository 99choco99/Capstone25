using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem) && (int)slotType == (int)newItem.data.type)
        {
            base.OnDrop(eventData);
            if (!hasItem)
            {
                InventoryManager.instance.Inventory[newItem.data.type].Item2.Remove(SlotIndex);
                InventoryManager.instance.Inventory[newItem.data.type].Item2.Add(newItem.previousSlot.SlotIndex, newItem.previousSlot.SlotIndex);
            }
        }
    }

}

