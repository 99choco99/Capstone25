using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Dictionary<SlotType, List<SlotData>> Inventory;


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

        //슬롯타입, 전체슬롯, 빈슬롯 정보를 초기화
        Inventory = new ();
        const int slotCount = 36;
        Init(SlotType.Equipment, slotCount);
        Init(SlotType.Consumption, slotCount);
        Init(SlotType.Other, slotCount);
    }

    void Init(SlotType type, int count)
    {
        List<SlotData> slots = new List<SlotData>();
        for (int i = 0; i < count; i++)
        {
            slots.Add(new SlotData { slotIndex = i });
        }
        Inventory.Add(type, slots);
        InventoryEvents.OnInventoryDataInitialized?.Invoke(type, count);
    }


    //아이템이 있는 슬롯끼리 교환
    public void SwapItem(SlotData sourceData, SlotData destinationData)
    {

        // 데이터 교환
        (sourceData.currentItemData, destinationData.currentItemData) = (destinationData.currentItemData, sourceData.currentItemData);
        (sourceData.itemCount, destinationData.itemCount) = (destinationData.itemCount, sourceData.itemCount);

        // 변경된 두 슬롯에 대해 UI 업데이트를 요청합니다.
        InventoryEvents.OnSlotDataChanged?.Invoke(GetSlotType(sourceData), sourceData.slotIndex);
        InventoryEvents.OnSlotDataChanged?.Invoke(GetSlotType(destinationData), destinationData.slotIndex);
    }

    public void MoveToEmptySlot(SlotData sourceData, SlotData destinationData)
    {
        
    }

    public void RegisterItemToMarket(SlotData sourceData, int saleCount)
    {
        sourceData.itemCount -= saleCount;

        if (sourceData.itemCount <= 0)
        {
            sourceData.currentItemData = null;
            sourceData.itemCount = 0;
        }

        InventoryEvents.OnSlotDataChanged?.Invoke(GetSlotType(sourceData), sourceData.slotIndex);
    }

    // 아이템을 구매하거나 추가하는 로직
    public void AddPurchasedItem(BuyItemResponse response)
    {
        ItemData data = ItemManager.Instance.GetItem(response.ItemId);
        SlotData emptySlotData = FindEmptySlot(data.type);

        if (emptySlotData == null)
        {
            Debug.LogWarning("인벤토리 공간 없음");
            return;
        }

        // 데이터만 변경하고, UI는 건드리지 않습니다.
        emptySlotData.currentItemData = data;
        emptySlotData.currentItemData.spec = response.spec;
        emptySlotData.itemCount = response.purchasedItemCount;

        // 데이터 변경이 완료되었음을 UI에 알립니다.
        InventoryEvents.OnSlotDataChanged?.Invoke(data.type, emptySlotData.slotIndex);
    }

    //비어있는 인벤토리 슬롯 찾아서 반환
    public SlotData FindEmptySlot(SlotType type)
    {
        return Inventory[type].Find(s => !s.hasItem);
    }

    //슬롯 타입 반환
    public SlotType GetSlotType(SlotData slotData)
    {
        return slotData.slotType;
    }

    void Clear()
    {
        
    }

}
