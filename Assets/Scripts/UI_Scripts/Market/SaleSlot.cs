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
        currentItem = draggedItem;
        hasItem = true;
        itemImage.sprite = draggedItem.data.icon;
        itemCount = draggedItem.currentSlot.itemCount;
    }
}
