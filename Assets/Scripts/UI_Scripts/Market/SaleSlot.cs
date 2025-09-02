using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaleSlot : Slot
{
    public Image itemImage;

    public override void OnDrop(PointerEventData eventData)
    {
        OwnedItem draggedItem = eventData.pointerDrag?.GetComponent<OwnedItem>();

        if (draggedItem == null) { return; }
        slotData.itemData = draggedItem.data;
        slotData.itemSpec = slotData.itemData.spec;
        slotData.itemId = slotData.itemData.id;
        itemImage.sprite = slotData.itemData.icon;
        slotData.itemCount = draggedItem.currentSlot.slotData.itemCount;
    }
}
