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
        slotData.currentItemData = draggedItem.data;
        itemImage.sprite = draggedItem.data.icon;
        slotData.itemCount = draggedItem.currentSlot.slotData.itemCount;
    }
}
