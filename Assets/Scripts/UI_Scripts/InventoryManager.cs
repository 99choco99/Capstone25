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
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
        ScrollRect = GetComponent<ScrollRect>();
        Inventory = new Dictionary<SlotType, Tuple<List<Slot>, SortedList<int,int>>>();
        Init(SlotType.Equipment, EquipmentInventory);
        Init(SlotType.Consumption, ConsumptionInventory);
        Init(SlotType.Other, OtherInventory);
        transform.parent.gameObject.SetActive(false);
    }

    public void ShowInvenType(RectTransform InventoryType)
    {
        ScrollRect.content = InventoryType;
    }

    void Init(SlotType type, Transform InventoryTransform)
    {
        List<Slot> itemSlots = new(InventoryTransform.GetComponentsInChildren<Slot>());
        SortedList<int,int> emptySlots = new();
        for(int i = 0; i < itemSlots.Count; i++)
        {
            itemSlots[i].SlotIndex = i;
            if (!itemSlots[i].hasItem)
            {
                emptySlots.Add(i,i);
            }
        }
        Tuple<List<Slot>, SortedList<int,int>> InventoryTuple = new(itemSlots, emptySlots);
        Inventory.Add(type, InventoryTuple);
    }

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
}
