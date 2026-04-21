using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    Player player;
    public bool isInit;
    public const int slotCount = 36;
    public Dictionary<SlotType, List<SlotData>> Inventory;

    // 특정 슬롯의 데이터가 변경되었음
    public event Action<SlotType, int> OnSlotDataChanged;
    // 인벤토리 데이터 초기화가 완료되었음
    public event Action<SlotType, int> OnInventoryDataInitialized;
    // 퀵슬롯 아이템 사용 완료
    public event Action<ItemSpec> OnQuickSlotUsed;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }

        //슬롯타입, 전체슬롯, 빈슬롯 정보를 초기화
        Inventory = new();

        //슬롯들 초기화
        Init(SlotType.Equipment, slotCount);
        Init(SlotType.Consumption, slotCount);
        Init(SlotType.Other, slotCount);
        Init(SlotType.Profile, 6);
        Init(SlotType.Quick, 1);

        isInit = true;
    }

    private void OnDestroy()
    {
        if (!player.IsLocalPlayer) { return; }
        OnSlotDataChanged -= SaveInventory;
    }


    void Init(SlotType type, int count)
    {
        List<SlotData> slots = new List<SlotData>();
        for (int i = 0; i < count; i++)
        {
            slots.Add(new SlotData {
                slotType = type,
                slotIndex = i
            });
        }
        Inventory.Add(type, slots);
        OnInventoryDataInitialized?.Invoke(type, count);
    }

    //인벤토리 불러오기
    public void LoadInventory(InventoryData response)
    {
        if(response?.inventory == null) { return; }
        foreach(SlotData data in response.inventory)
        {
            ItemBase baseItemData = ItemManager.Instance.GetItem(data.itemId);
            Inventory[data.slotType][data.slotIndex].itemId = data.itemId;
            Inventory[data.slotType][data.slotIndex].itemCount = data.itemCount;
            Inventory[data.slotType][data.slotIndex].itemData = baseItemData;
            Inventory[data.slotType][data.slotIndex].itemSpec = data.itemSpec;


            if (data.slotType == SlotType.Profile)
            {
                EquipmentType equipmentType = (EquipmentType)data.slotIndex;
                player.Equipment.Equip(equipmentType, data.itemSpec);
            }
            OnSlotDataChanged?.Invoke(data.slotType, data.slotIndex);
        }
        
    }

    public void SaveInventory(SlotType type, int slotIndex)
    {
        //player.localAPI.Inventory.RequestSaveInventory(Inventory[type][slotIndex]);
    }


    //아이템이 있는 슬롯끼리 교환
    public void SwapItems(SlotType sourceSlotType, int sourceSlotIndex, SlotType destinationSlotType, int destinationSlotIndex)
    {
        SlotData sourceData = Inventory[sourceSlotType][sourceSlotIndex];
        SlotData destinationData = Inventory[destinationSlotType][destinationSlotIndex];

        // 데이터 교환
        (sourceData.itemId, destinationData.itemId) = (destinationData.itemId, sourceData.itemId);
        (sourceData.itemCount, destinationData.itemCount) = (destinationData.itemCount, sourceData.itemCount);
        (sourceData.itemData, destinationData.itemData) = (destinationData.itemData, sourceData.itemData);
        (sourceData.itemSpec, destinationData.itemSpec) = (destinationData.itemSpec, sourceData.itemSpec);

        // 변경된 두 슬롯에 대해 UI 업데이트를 요청합니다.
        OnSlotDataChanged?.Invoke(sourceSlotType, sourceSlotIndex);
        OnSlotDataChanged?.Invoke(destinationSlotType, destinationSlotIndex);
    }

    // 아이템을 빈 슬롯으로 이동
    public void MoveToEmptySlot(SlotType sourceSlotType, int sourceSlotIndex, SlotType destinationSlotType, int destinationSlotIndex)
    {
        SlotData sourceData = Inventory[sourceSlotType][sourceSlotIndex];
        SlotData destinationData = Inventory[destinationSlotType][destinationSlotIndex];

        // 소스 데이터를 목적지 슬롯으로 복사
        destinationData.itemId = sourceData.itemId;
        destinationData.itemCount = sourceData.itemCount;
        destinationData.itemData = sourceData.itemData;
        destinationData.itemSpec = sourceData.itemSpec;

        // 소스 슬롯 초기화       
        sourceData.Clear();

        // 변경된 두 슬롯에 대해 UI 업데이트를 요청합니다.
        OnSlotDataChanged?.Invoke(sourceSlotType, sourceSlotIndex);
        OnSlotDataChanged?.Invoke(destinationSlotType, destinationSlotIndex);
    }

    public void MergeItems(SlotType sourceSlotType, int sourceSlotIndex, SlotType destinationSlotType, int destinationSlotIndex)
    {
        SlotData sourceData = Inventory[sourceSlotType][sourceSlotIndex];
        SlotData destinationData = Inventory[destinationSlotType][destinationSlotIndex];

        if (sourceData.itemData == null || destinationData.itemData == null || sourceData.itemId != destinationData.itemId)
        {
            return;
        }


        //  Equipment나 Profile이면 스택 불가능
        SlotType itemType = destinationData.itemData.type;
        if (itemType == SlotType.Equipment || itemType == SlotType.Profile)
        {
            // 스택 불가능 아이템이면 대신 스왑을 실행
            SwapItems(sourceSlotType, sourceSlotIndex, destinationSlotType, destinationSlotIndex);
            return;
        }

        int maxStackSize = 99;
        int availableSpace = maxStackSize - destinationData.itemCount;

        if (availableSpace <= 0)
        {
            // 꽉 찼으면 스왑을 실행합니다.
            SwapItems(sourceSlotType, sourceSlotIndex, destinationSlotType, destinationSlotIndex);
            return;
        }

        int amountToMove = Mathf.Min(availableSpace, sourceData.itemCount);



        // 소스 데이터를 목적지 슬롯으로 복사
        destinationData.itemCount += amountToMove;
        sourceData.itemCount -= amountToMove;

        if (sourceData.itemCount <= 0)
        {
            sourceData.Clear();
        }

        Inventory[sourceSlotType][sourceSlotIndex] = sourceData;
        Inventory[destinationSlotType][destinationSlotIndex] = destinationData;

        OnSlotDataChanged?.Invoke(sourceSlotType, sourceSlotIndex);
        OnSlotDataChanged?.Invoke(destinationSlotType, destinationSlotIndex);
    }


    //마켓에 아이템을 등록했을 때
    public void RegisterItemToMarket(SlotData saleSlotData, int saleCount)
    {
        SlotType originalSlotType = saleSlotData.itemData.type;
        int originalSlotIndex = saleSlotData.slotIndex;

        SlotData originalSlotData = Inventory[originalSlotType][originalSlotIndex];

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


        Inventory[emptySlotData.slotType][emptySlotData.slotIndex] = emptySlotData;


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
        emptySlotData.itemSpec = data.baseStats;
        emptySlotData.itemCount = 1;


        Inventory[emptySlotData.slotType][emptySlotData.slotIndex] = emptySlotData;
        OnSlotDataChanged?.Invoke(data.type, emptySlotData.slotIndex);
    }


    public void RequestUseQuickSlotItem()
    {
        SlotData quickSlotData = Inventory[SlotType.Quick][0];

        if (!quickSlotData.hasItem || quickSlotData.itemData.type != SlotType.Consumption)
        {
            Debug.Log("퀵슬롯에 소비 아이템이 없습니다.");
            return;
        }

        ItemSpec spec = quickSlotData.itemData.baseStats;

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
        return Inventory[type].Find(s => !s.hasItem);
    }

    //슬롯 타입 반환
    public SlotType GetSlotType(SlotData slotData)
    {
        return slotData.slotType;
    }

    public SlotData GetSlotData(SlotData slotData)
    {
        return Inventory[slotData.slotType][slotData.slotIndex];
    }

}
