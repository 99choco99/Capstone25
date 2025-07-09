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

    public Dictionary<SlotType, Tuple<List<Slot>, SortedList<int,int>>> Inventory;


    public static InventoryManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        ScrollRect = GetComponent<ScrollRect>();
        //슬롯타입, 전체슬롯, 빈슬롯
        Inventory = new Dictionary<SlotType, Tuple<List<Slot>, SortedList<int,int>>>();
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
        List<Slot> itemSlots = new(InventoryTransform.GetComponentsInChildren<Slot>());
        SortedList<int,int> emptySlots = new();
        for(int i = 0; i < itemSlots.Count; i++)
        {
            itemSlots[i].slotIndex = i;
            if (!itemSlots[i].hasItem)
            {
                emptySlots.Add(i,i);
            }
        }
        Tuple<List<Slot>, SortedList<int,int>> InventoryTuple = new(itemSlots, emptySlots);
        Inventory.Add(type, InventoryTuple);
    }

    //비어있는 인벤토리 슬롯 찾아서 반환
    public Slot FindEmptySlot(SlotType type)
    {
        if(Inventory[type].Item2.Count > 0)
        {
            int emptyIndex = Inventory[type].Item2.Keys[0];
            Inventory[type].Item2.Remove(emptyIndex);
            Inventory[type].Item1[emptyIndex].hasItem = true;
            return Inventory[type].Item1[emptyIndex];
        }
        Debug.Log("인벤토리 공간 없음");
        return null;
    }

    //현재 아이템 가져오기
    public void GetItemSlot(SlotType type, Transform InventorySlots)
    {
        List<Slot> targetSlots = new(InventorySlots.GetComponentsInChildren<Slot>());
        List<Slot> itemSlots = Inventory[type].Item1;
        for(int i = 0; i< itemSlots.Count; i++)
        {
            if (itemSlots[i].hasItem)
            {
                targetSlots[i].currentItem = itemSlots[i].currentItem;
                Instantiate(targetSlots[i].currentItem.gameObject, targetSlots[i].transform);
                targetSlots[i].itemCount = itemSlots[i].itemCount;
                targetSlots[i].hasItem = true;
            }
            targetSlots[i].slotIndex = i;
        }
    }

    public void GetSingleItemSlot(SlotType type, Slot currentSlot ,Slot targetSlot)
    {
        targetSlot.currentItem = currentSlot.currentItem;
        Instantiate(targetSlot.currentItem.gameObject, targetSlot.transform);
        targetSlot.itemCount = currentSlot.itemCount;
        targetSlot.hasItem = true;
    }
}
