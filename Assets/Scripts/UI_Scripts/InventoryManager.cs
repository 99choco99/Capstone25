using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager
{
    Player player;
    public const int slotCount = 36;
    public Dictionary<SlotType, List<SlotData>> SlotDict{get;private set;}

    // 특정 슬롯의 데이터가 변경되었음
    public event Action<SlotType, int> OnSlotDataChanged;
    // 인벤토리 데이터 초기화가 완료되었음
    public event Action<SlotType, int> OnInventoryDataInitialized;
    // 퀵슬롯 아이템 사용 완료
    public event Action<ItemSpec> OnQuickSlotUsed;
    // 장비 변경 이벤트
    public event Action<List<EquipmentItemData>> OnEquipmentChanged;

    public InventoryManager(Player player)
    {
        this.player = player;
        //슬롯타입, 전체슬롯, 빈슬롯 정보를 초기화
        SlotDict = new();

        //슬롯들 초기화
        SlotInit(SlotType.Equipment, slotCount);
        SlotInit(SlotType.Consumption, slotCount);
        SlotInit(SlotType.Other, slotCount);
        SlotInit(SlotType.Profile, 6);
        SlotInit(SlotType.Quick, 1);
    }
    void SlotInit(SlotType type, int count)
    {
        List<SlotData> slots = new List<SlotData>();
        for (int i = 0; i < count; i++)
        {
            slots.Add(new SlotData
            {
                slotType = type,
                slotIndex = i
            });
        }
        SlotDict.Add(type, slots);
        OnInventoryDataInitialized?.Invoke(type, count);
    }



    //인벤토리 불러오기
    public void LoadInventory(InventoryData response)
    {
        if(response?.inventory == null) { return; }
        foreach(SlotData data in response.inventory)
        {
            ItemBase baseItemData = ItemManager.Instance.GetItem(data.itemId);
            SlotDict[data.slotType][data.slotIndex].itemId = data.itemId;
            SlotDict[data.slotType][data.slotIndex].itemCount = data.itemCount;
            SlotDict[data.slotType][data.slotIndex].itemData = baseItemData;
            SlotDict[data.slotType][data.slotIndex].itemSpec = data.itemSpec;


            if (data.slotType == SlotType.Profile)
            {
                EquipmentType equipmentType = (EquipmentType)data.slotIndex;
                //Equipment.Equip(equipmentType, data.itemSpec);
            }
            OnSlotDataChanged?.Invoke(data.slotType, data.slotIndex);
        }
        
    }


    //=====================UI에서 접근=======================

    public void RequestMoveItem(SlotType srcType, int srcIdx, SlotType dstType, int dstIdx) 
    {
        SlotData srcData = SlotDict[srcType][srcIdx];
        SlotData dstData = SlotDict[dstType][dstIdx];


        if(!srcData.hasItem) { return; }

        if (!IsValidMove(srcData, dstData)) {  return; }    

        if (!dstData.hasItem) MoveToEmptySlot(srcData, dstData);
        else if (srcData.itemId == dstData.itemId && IsStackable(dstData)) MergeItems(srcData, dstData);
        else SwapItems(srcData, dstData);

        if (srcType == SlotType.Profile || dstType == SlotType.Profile)
        {
            NotifyEquipmentChanged();
        }

    }

    //===============인벤토리 로직========================

    //아이템이 있는 슬롯끼리 교환
    private void SwapItems(SlotData srcData, SlotData dstData)
    {
        // 데이터 교환
        (srcData.itemId, dstData.itemId) = (dstData.itemId, srcData.itemId);
        (srcData.itemCount, dstData.itemCount) = (dstData.itemCount, srcData.itemCount);
        (srcData.itemData, dstData.itemData) = (dstData.itemData, srcData.itemData);
        (srcData.itemSpec, dstData.itemSpec) = (dstData.itemSpec, srcData.itemSpec);

        // 변경된 두 슬롯에 대해 UI 업데이트를 요청합니다.
        OnSlotDataChanged?.Invoke(srcData.slotType, srcData.slotIndex);
        OnSlotDataChanged?.Invoke(dstData.slotType, dstData.slotIndex);
    }

    // 아이템을 빈 슬롯으로 이동
    private void MoveToEmptySlot(SlotData srcData, SlotData dstData)
    {
        // 소스 데이터를 목적지 슬롯으로 복사
        dstData.itemId = srcData.itemId;
        dstData.itemCount = srcData.itemCount;
        dstData.itemData = srcData.itemData;
        dstData.itemSpec = srcData.itemSpec;

        // 소스 슬롯 초기화       
        srcData.Clear();

        // 변경된 두 슬롯에 대해 UI 업데이트를 요청
        OnSlotDataChanged?.Invoke(srcData.slotType, srcData.slotIndex);
        OnSlotDataChanged?.Invoke(dstData.slotType, dstData.slotIndex);
    }

    //같은 종류의 아이템 합치기
    private void MergeItems(SlotData srcData, SlotData dstData)
    {
        int maxStackSize = 99;
        int availableSpace = maxStackSize - dstData.itemCount;

        if (availableSpace <= 0)
        {
            // 꽉 찼으면 스왑을 실행
            SwapItems(srcData, dstData);
            return;
        }

        int amountToMove = Mathf.Min(availableSpace, srcData.itemCount);

        dstData.itemCount += amountToMove;
        srcData.itemCount -= amountToMove;

        if (srcData.itemCount <= 0)
        {
            srcData.Clear();
        }

        OnSlotDataChanged?.Invoke(srcData.slotType, srcData.slotIndex);
        OnSlotDataChanged?.Invoke(dstData.slotType, dstData.slotIndex);
    }


    private void NotifyEquipmentChanged()
    {
        List<EquipmentItemData> equipmentItems = new List<EquipmentItemData>();

        foreach (SlotData slot in SlotDict[SlotType.Profile])
        {
            if(slot.hasItem && slot.itemData is EquipmentItemData equipData)
            {
                equipmentItems.Add(equipData);
            }
        }

        OnEquipmentChanged?.Invoke(equipmentItems);
    }

    private bool IsStackable(SlotData data)
    {
        return data.itemData.type != SlotType.Equipment && data.itemData.type != SlotType.Profile;
    }


    private bool IsValidMove(SlotData srcData,SlotData dstData)
    {
        //같은 놈일때
        if(srcData.slotType == dstData.slotType && srcData.slotIndex == dstData.slotIndex) { return false; }

        //다른 슬롯 타입으로 이동하려 할 때
        if(dstData.slotType == SlotType.Consumption || dstData.slotType == SlotType.Other)
        {
            if (srcData.itemData.type != dstData.slotType) { return false; }
        }
        
        //아이템을 장착할 때
        if(dstData.slotType == SlotType.Profile)
        {
            if(srcData.itemData is not EquipmentItemData equipData) { return false; } // 장비아이템인지?
            if ((int)equipData.equipmentType != dstData.slotIndex) { return false; } //그 부위가 올바른 곳인지?
        }

        //아이템을 벗을 때
        if(srcData.slotType == SlotType.Profile)
        {
            if(dstData.slotType != SlotType.Equipment) { return false; }
        }

        //퀵슬롯 일 때
        if(dstData.slotType == SlotType.Quick)
        {
            if (dstData.itemData.type != SlotType.Consumption) return false; // 소비아이템인지?
        }

        return true;
    }

    //===================================================

    //마켓에 아이템을 등록했을 때
    public void RegisterItemToMarket(SlotData saleSlotData, int saleCount)
    {
        SlotType originalSlotType = saleSlotData.itemData.type;
        int originalSlotIndex = saleSlotData.slotIndex;

        SlotData originalSlotData = SlotDict[originalSlotType][originalSlotIndex];

        originalSlotData.itemCount -= saleCount;

        if (originalSlotData.itemCount <= 0)
        {
            originalSlotData.Clear();
        }

        OnSlotDataChanged?.Invoke(originalSlotType, originalSlotIndex);
    }


    //마켓에 아이템을 취소했을 때
    public void ReturnItemFromMarket(CancelRegistResponse response)
    {

    }

    // 아이템을 구매하는 로직
    public void AddPurchasedItem(BuyItemResponse response)
    {
        ItemBase data = ItemManager.Instance.GetItem(response.ItemId);
        SlotData emptySlotData = FindEmptySlot(data.type);

        if (emptySlotData == null)
        {
            Debug.LogWarning("인벤토리 공간 없음");
            return;
        }

        emptySlotData.itemData = data;
        emptySlotData.itemId = response.ItemId;
        emptySlotData.itemSpec = response.spec;
        emptySlotData.itemCount = response.purchasedItemCount;


        SlotDict[emptySlotData.slotType][emptySlotData.slotIndex] = emptySlotData;


        player.Stats.SetGold(response.gold);
        OnSlotDataChanged?.Invoke(data.type, emptySlotData.slotIndex);
    }

    //아이템 획득
    public void AddItem(int? ItemId)
    {
        ItemBase data = ItemManager.Instance.GetItem(ItemId);
        if(data == null) { return; }
        SlotData emptySlotData = FindEmptySlot(data.type);

        if (emptySlotData == null)
        {
            Debug.LogWarning("인벤토리 공간 없음");
            return;
        }

        emptySlotData.itemData = data;
        emptySlotData.itemId = ItemId;
        emptySlotData.itemCount = 1;


        SlotDict[emptySlotData.slotType][emptySlotData.slotIndex] = emptySlotData;
        OnSlotDataChanged?.Invoke(data.type, emptySlotData.slotIndex);
    }


    public void RequestUseQuickSlotItem()
    {
        SlotData quickSlotData = SlotDict[SlotType.Quick][0];

        if (!quickSlotData.hasItem || quickSlotData.itemData.type != SlotType.Consumption)
        {
            Debug.Log("퀵슬롯에 소비 아이템이 없습니다.");
            return;
        }

        ItemSpec spec = quickSlotData.itemSpec;

        quickSlotData.itemCount--;

        if (quickSlotData.itemCount <= 0)
        {
            quickSlotData.Clear(); // 슬롯 비우기
        }

        OnSlotDataChanged?.Invoke(SlotType.Quick, 0);

        OnQuickSlotUsed?.Invoke(spec); //퀵슬롯 쿨타임
    }


    //비어있는 인벤토리 슬롯 찾아서 반환
    public SlotData FindEmptySlot(SlotType type)
    {
        return SlotDict[type].Find(s => !s.hasItem);
    }

    //슬롯 타입 반환
    public SlotType GetSlotType(SlotData slotData)
    {
        return slotData.slotType;
    }

    public SlotData GetSlotData(SlotData slotData)
    {
        return SlotDict[slotData.slotType][slotData.slotIndex];
    }

}
