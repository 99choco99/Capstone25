using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaleSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        OwnedItem DraggedItem = eventData.pointerDrag.GetComponent<OwnedItem>();
        if(DraggedItem != null)
        {
            OwnedItem newItem = Instantiate(DraggedItem, transform);
            currentItem = DraggedItem;
            newItem.rect.position = rect.position;
            newItem.SetAlphaValue(1.0f);
        }
    }
}
