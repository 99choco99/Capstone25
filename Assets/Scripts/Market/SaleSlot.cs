using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaleSlot : Slot
{
    public Image itemImage;

    public override void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem = eventData.pointerDrag?.GetComponent<InventoryItemUI>();

        if (draggedItem == null) { return; }

        SlotData originalSlot = draggedItem.ParentSlot.slotData;

        slotData.itemData = originalSlot.itemData;
        slotData.itemSpec = originalSlot.itemSpec;
        slotData.itemId = originalSlot.itemId;
        slotData.itemCount = originalSlot.itemCount;

        //원본 슬롯의 위치를 정확히 저장
        slotData.slotType = originalSlot.slotType;
        slotData.slotIndex = originalSlot.slotIndex;

        //itemImage.sprite = draggedItem.
    }
}
