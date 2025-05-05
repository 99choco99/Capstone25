using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        eventData.pointerDrag.TryGetComponent<InventoryItem>(out newItem);
        if((int)slotType == (int)newItem.itemType)
        {
            base.OnDrop(eventData);
        }
    }
}
