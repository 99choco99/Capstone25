using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MarketInventory : MonoBehaviour
{
    [SerializeField] Transform EquipmentSlots;
    [SerializeField] Transform ConsumptionSlots;
    [SerializeField] Transform OtherSlots;

    private void Start()
    {
        InventoryManager.instance.GetItemSlot(SlotType.Equipment, EquipmentSlots);
        InventoryManager.instance.GetItemSlot(SlotType.Consumption, ConsumptionSlots);
        InventoryManager.instance.GetItemSlot(SlotType.Other, OtherSlots);
    }
}
