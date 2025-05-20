using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent(out EquipmentItem item) && item.previousSlot.slotType == SlotType.Profile)
        {
            item.TakeOff(item.previousSlot.playerData);
        }
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem) && slotType == newItem.data.type)
        {
            base.OnDrop(eventData);
            SortedList<int,int> emptyList = InventoryManager.instance.Inventory[newItem.data.type].Item2;
            if (!hasItem)
            {
                emptyList.Remove(SlotIndex);
                emptyList.Add(newItem.previousSlot.SlotIndex, newItem.previousSlot.SlotIndex);
            }
        }
    }

}

