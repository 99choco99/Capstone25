using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : UIBase
{
    [SerializeField] private InventoryManager Inventory;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform equipmentParent;
    [SerializeField] private Transform consumptionParent;
    [SerializeField] private Transform otherParent;
    [SerializeField] private Transform ProfileParent;
    [SerializeField] private Transform QuickParent;
    [SerializeField] private TextMeshProUGUI goldText;

    private Dictionary<SlotType, List<Slot>> uiSlots = new();

    private void Awake()
    {
        Inventory.OnInventoryDataInitialized += InitUI;
        Inventory.OnSlotDataChanged += UpdateSlotUI;
        //PlayerStats.OnLocalPlayerGoldChanged += UpdateGoldUI;
    }

    private void OnDestroy()
    {
        Inventory.OnInventoryDataInitialized -= InitUI;
        Inventory.OnSlotDataChanged -= UpdateSlotUI;
        //PlayerStats.OnLocalPlayerGoldChanged -= UpdateGoldUI;
    }

    private void InitUI(SlotType type, int count)
    {
        Transform parent = GetParentForType(type);
        if (parent == null) { return; }

        uiSlots[type] = new List<Slot>();

        if (type == SlotType.Profile || type == SlotType.Quick)
        {
            Slot[] existingSlots = parent.GetComponentsInChildren<Slot>(true);

            for (int i = 0; i < existingSlots.Length; i++)
            {
                if (i >= count) break;

                Slot slot = existingSlots[i];
                slot.slotData = Inventory.Inventory[type][i];

                slot.OnDropRequest += OnDropHandler;

                uiSlots[type].Add(slot);


            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                GameObject slotObject = Instantiate(slotPrefab, parent);
                Slot slot = slotObject.GetComponent<Slot>();

                slot.slotData = Inventory.Inventory[type][i];

                slot.OnDropRequest += OnDropHandler;

                uiSlots[type].Add(slot);
            }
        }
    }

    private void OnDropHandler(Slot droppedSlot, PointerEventData eventData)
    {
        OwnedItem draggedItemUI = eventData.pointerDrag?.GetComponent<OwnedItem>();
        if (draggedItemUI == null) { return; }
        Slot draggedSlot = draggedItemUI?.currentSlot;
        if (droppedSlot == draggedSlot) { return; }

        if (droppedSlot.slotData.hasItem)
        {
            if(draggedSlot.slotData.itemId == droppedSlot.slotData.itemId)
            {
            Inventory.MergeItems(
                draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
                droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex);
            }
            else
            {
                Inventory.SwapItems(
                    draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
                    droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex);
            }
        }
        else
        {
            Inventory.MoveToEmptySlot(
                draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
                droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex);
        }

        if (draggedItemUI != null)
        {
            Destroy(draggedItemUI.gameObject);
        }

    }

    private void UpdateSlotUI(SlotType type, int index)
    {
        if(!uiSlots.ContainsKey(type)) { return; }
        Slot uiSlot = uiSlots[type][index];
        SlotData slotData = Inventory.Inventory[type][index];

        if (slotData.hasItem)
        {
            // 1. 슬롯의 자식 중에서 기존 아이템 UI를 찾기
            OwnedItem ownedItem = uiSlot.GetComponentInChildren<OwnedItem>();

            // 2. 아이템 UI가 없다면 새로 생성
            if (ownedItem == null)
            {
                GameObject itemObject = Instantiate(itemPrefab, uiSlot.transform);
                ownedItem = itemObject.GetComponent<OwnedItem>();
            }
            if (ownedItem != null)
            {

                ownedItem.data = slotData.itemData;
                ownedItem.image.sprite = slotData.itemData.icon;
                ownedItem.currentSlot = uiSlot;
                ownedItem.currentSlot.slotData = slotData;
                ownedItem.UpdateCountUI(slotData.itemCount);
            }
        }
        else
        {
            foreach (Transform child in uiSlot.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void UpdateGoldUI(int gold)
    {
        goldText.text = $"{gold} Gold";
    }

    private Transform GetParentForType(SlotType type)
    {
        Transform uiParent = type switch
        {
            SlotType.Equipment => equipmentParent,
            SlotType.Consumption => consumptionParent,
            SlotType.Other => otherParent,
            SlotType.Profile => ProfileParent,
            SlotType.Quick => QuickParent,
            _ => null
        };

        return uiParent;
    }

}