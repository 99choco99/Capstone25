using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlot : Slot
{
    public EquipmentType equipmentSlotType;

    override public void OnDrop(PointerEventData eventData)
    {
        eventData.pointerDrag.TryGetComponent<InventoryItem>(out newItem);

        if (newItem.itemType == ItemType.Equipment && slotType == SlotType.Equipment)
        {
            if ((int)newItem.GetEquipmentType() != (int)equipmentSlotType) { return; }
            newItem.Apply(playerData);
            base.OnDrop(eventData);
        }
    }
}
