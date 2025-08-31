using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform equipmentParent;
    [SerializeField] private Transform consumptionParent;
    [SerializeField] private Transform otherParent;

    private Dictionary<SlotType, List<Slot>> uiSlots = new();

    private void OnEnable()
    {
        InventoryEvents.OnInventoryDataInitialized += InitUI;
        InventoryEvents.OnSlotDataChanged += UpdateSlotUI;
    }

    private void OnDisable()
    {
        InventoryEvents.OnInventoryDataInitialized -= InitUI;
        InventoryEvents.OnSlotDataChanged -= UpdateSlotUI;
    }

    private void InitUI(SlotType type, int count)
    {
        Transform parent = GetParentForType(type);
        if (parent == null) return;

        uiSlots[type] = new List<Slot>();
        for (int i = 0; i < count; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, parent);
            Slot slot = slotObject.GetComponent<Slot>();
            slot.slotData.slotType = type;
            slot.slotData = InventoryManager.instance.Inventory[type][i];

            slot.OnDropRequest += OnDropHandler;

            uiSlots[type].Add(slot);
        }
    }

    private void OnDropHandler(Slot droppedSlot, PointerEventData eventData)
    {
        Slot draggedSlot = eventData.pointerDrag?.GetComponent<OwnedItem>().currentSlot;

        InventoryManager.instance.SwapItem(draggedSlot.slotData, droppedSlot.slotData);

    }

    private void UpdateSlotUI(SlotType type, int index)
    {
        Slot uiSlot = uiSlots[type][index];
        uiSlot.UpdateUI();
    }

    private Transform GetParentForType(SlotType type)
    {
        Transform uiParent = type switch
        {
            SlotType.Equipment => equipmentParent,
            SlotType.Consumption => consumptionParent,
            SlotType.Other => otherParent,
            _ => null
        };

        return uiParent;
    }
}