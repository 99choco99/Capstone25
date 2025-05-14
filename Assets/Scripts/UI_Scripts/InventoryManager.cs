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

    public Dictionary<ItemType, Tuple<List<ItemSlot>, SortedList<int,int>>> Inventory;


    public static InventoryManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
        ScrollRect = GetComponent<ScrollRect>();
        Inventory = new Dictionary<ItemType, Tuple<List<ItemSlot>, SortedList<int,int>>>();
        Init(ItemType.Equipment, EquipmentInventory);
        Init(ItemType.Consumption, ConsumptionInventory);
        Init(ItemType.Other, OtherInventory);
    }

    public void ShowInvenType(RectTransform InventoryType)
    {
        ScrollRect.content = InventoryType;
    }

    void Init(ItemType type, Transform InventoryTransform)
    {
        List<ItemSlot> itemSlots = new(InventoryTransform.GetComponentsInChildren<ItemSlot>());
        SortedList<int,int> emptySlots = new();
        for(int i = 0; i < itemSlots.Count; i++)
        {
            itemSlots[i].SlotIndex = i;
            if (!itemSlots[i].hasItem)
            {
                emptySlots.Add(i,i);
            }
        }
        Tuple<List<ItemSlot>, SortedList<int,int>> InventoryTuple = new(itemSlots, emptySlots);
        Inventory.Add(type, InventoryTuple);
    }

    public ItemSlot FindEmptySlot(ItemType type)
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
}
