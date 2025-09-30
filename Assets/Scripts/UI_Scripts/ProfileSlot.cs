using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileSlot : Slot
{
    [SerializeField] EquipmentType EquipmentSlotType;

    private void Start()
    {
        OnDropRequest += OnDropHandler;
    }

    private void OnDestroy()
    {
        OnDropRequest -= OnDropHandler;
    }

    override public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem))
        {
            if (newItem.data.type == SlotType.Equipment && newItem.data.equipmentType == EquipmentSlotType)
            {
                EquipmentManager.instance.Equip(EquipmentSlotType, newItem.spec);
                base.OnDrop(eventData);
            }
        }

    }

    private void OnDropHandler(Slot droppedSlot, PointerEventData eventData)
    {
        OwnedItem draggedItemUI = eventData.pointerDrag?.GetComponent<OwnedItem>();
        Slot draggedSlot = draggedItemUI?.currentSlot;
        if (droppedSlot == draggedSlot) { return; }
        if (droppedSlot.slotData.hasItem)
        {
            InventoryManager.instance.SwapItems(
                draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
                droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex);
        }
        else
        {
            InventoryManager.instance.MoveToEmptySlot(
                draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
                droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex);
        }
        if (draggedItemUI != null)
        {
            Destroy(draggedItemUI.gameObject);
        }

    }


}
