using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    PlayerData playerData;
    private void Start()
    {
        playerData = GetComponentInParent<PlayerData>();
    }
    public override void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent(out EquipmentItem item) && item.previousSlot.slotType == SlotType.Profile)
        {
            item.TakeOff(playerData);
        }
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem) && slotType == newItem.data.type)
        {
            base.OnDrop(eventData);
            if (!hasItem)
            {
                InventoryManager.instance.Inventory[newItem.data.type].Item2.Remove(slotIndex);
                InventoryManager.instance.Inventory[newItem.data.type].Item2.Add(newItem.previousSlot.slotIndex, newItem.previousSlot.slotIndex);
            }
        }
    }

}

