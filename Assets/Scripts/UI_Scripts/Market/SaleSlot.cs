using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaleSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem))
        {
            //창이 비어있을 때
            if (!hasItem)
            {
                newItem.previousSlot.hasItem = false;
                newItem.previousSlot.currentItem = null;
                itemCount = newItem.previousSlot.itemCount;
                newItem.previousSlot.itemCount = 0;
            }
            else//창이 차있을 때
            {
                currentItem.transform.SetParent(currentItem.previousSlot.transform);
                currentItem.rect.position = currentItem.previousSlot.rect.position;
                newItem.previousSlot.currentItem = currentItem;
                (newItem.previousSlot.itemCount, itemCount) = (itemCount, newItem.previousSlot.itemCount);
            }
            newItem.transform.SetParent(transform);
            newItem.rect.position = rect.position;
            currentItem = newItem;
            hasItem = true;
        }
    }
}
