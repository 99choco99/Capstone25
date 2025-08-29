using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    ScrollRect ScrollRect;
    public GameObject ItemDescription;
    public Transform EquipmentInventory;
    public Transform ConsumptionInventory;
    public Transform OtherInventory;

    public Dictionary<SlotType, InventoryData> Inventory;


    public static InventoryManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        ScrollRect = GetComponent<ScrollRect>();

        //슬롯타입, 전체슬롯, 빈슬롯 정보를 초기화
        Inventory = new ();
        Init(SlotType.Equipment, EquipmentInventory);
        Init(SlotType.Consumption, ConsumptionInventory);
        Init(SlotType.Other, OtherInventory);
        transform.parent.gameObject.SetActive(false);
    }

    public void SetInvenType(RectTransform InventoryType)
    {
        ScrollRect.content = InventoryType;
    }

    void Init(SlotType type, Transform InventoryTransform)
    {
        List<Slot> slots = new(InventoryTransform.GetComponentsInChildren<Slot>());
        Inventory.Add(type, new InventoryData(slots));

        foreach (Slot slot in slots)
        {
            slot.OnChangedSlot += HandleSlotChanged;
        }
    }


    //슬롯에서 변경이 일어날 경우
    private void HandleSlotChanged(Slot slot)
    {
        InventoryData data = Inventory[slot.slotType];

        if (slot.hasItem)
        {
            data.EmptySlots.Remove(slot.slotIndex);
        }
        else
        {
            data.EmptySlots.Add(slot.slotIndex);
        }
    }


    //슬롯의 정보를 업데이트
    public void UpdateSlot(Slot slot, int delta)
    {
        InventoryData data = Inventory[slot.slotType];
        int index = slot.slotIndex;

        if (slot.currentItem == null)
        {
            Debug.LogWarning("아이템이 없는 슬롯입니다.");
            slot.Clear();
            return;
        }

        data.Slots[index].itemCount += delta;

        if (data.Slots[index].itemCount <= 0)
        {
            // 수량이 0 이하 슬롯 비우기
            Destroy(data.Slots[index].currentItem.gameObject);
            data.Slots[index].Clear();
        }
        else
        {
            // UI 갱신
            data.Slots[index].UpdateUI();
        }
    }

    //비어있는 인벤토리 슬롯 찾아서 반환
    public Slot FindEmptySlot(SlotType type)
    {
        if(Inventory[type].EmptySlots.Count > 0)
        {
            int emptyIndex = Inventory[type].EmptySlots.Min;
            Inventory[type].EmptySlots.Remove(emptyIndex);
            Inventory[type].Slots[emptyIndex].hasItem = true;

            return Inventory[type].Slots[emptyIndex];
        }
        Debug.Log("인벤토리 공간 없음");
        return null;
    }



    public class InventoryData
    {
        public List<Slot> Slots;
        public SortedSet<int> EmptySlots;

        public InventoryData(List<Slot> slots) {
            Slots = slots;
            for (int i = 0; i < Slots.Count; i++) {
                slots[i].slotIndex = i;
            }
            EmptySlots = new SortedSet<int>(slots.FindAll(s => !s.hasItem).ConvertAll(s => s.slotIndex));
        }
    }
}
