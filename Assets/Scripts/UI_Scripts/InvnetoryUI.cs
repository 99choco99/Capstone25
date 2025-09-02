using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    bool isInit = false;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform equipmentParent;
    [SerializeField] private Transform consumptionParent;
    [SerializeField] private Transform otherParent;

    private Dictionary<SlotType, List<Slot>> uiSlots = new();

    private void Awake()
    {
        InventoryEvents.OnInventoryDataInitialized += InitUI;
        InventoryEvents.OnSlotDataChanged += UpdateSlotUI;

    }

    private void OnEnable()
    {


        //// 인벤토리가 이미 초기화되었는지 확인 후 수동으로 초기화 호출
        //if (InventoryManager.instance != null && InventoryManager.instance.isInit)
        //{
        //    foreach (var pair in InventoryManager.instance.Inventory)
        //    {
        //        InitUI(pair.Key, pair.Value.Count);
        //        for (int i = 0; i < pair.Value.Count; i++)
        //        {
        //            UpdateSlotUI(pair.Key, i);
        //        }
        //    }
        //}
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
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

            slot.slotData = InventoryManager.instance.Inventory[type][i];

            slot.OnDropRequest += OnDropHandler;

            uiSlots[type].Add(slot);
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
            if (draggedItemUI != null)
            {
                Destroy(draggedItemUI.gameObject);
            }
        }

    }

    private void UpdateSlotUI(SlotType type, int index)
    {
        Slot uiSlot = uiSlots[type][index];
        SlotData slotData = InventoryManager.instance.Inventory[type][index];

        if (slotData.hasItem)
        {
            // 1. 슬롯의 자식 중에서 기존 아이템 UI를 찾습니다.
            OwnedItem ownedItem = uiSlot.GetComponentInChildren<OwnedItem>();

            // 2. 아이템 UI가 없다면 새로 생성합니다.
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
                ownedItem.UpdateCountUI(slotData.itemCount);
            }
        }
        else
        {
            foreach (Transform child in uiSlot.transform)
            {
                Destroy(child.gameObject);
                slotData = default;
            }
        }
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