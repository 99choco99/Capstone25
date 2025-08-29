using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

using static SocketManager;

public class MarketInventory : MonoBehaviour
{

    [SerializeField] Transform EquipmentSlots;
    [SerializeField] Transform ConsumptionSlots;
    [SerializeField] Transform OtherSlots;
    [SerializeField] GameObject ItemContainer;



    private void OnEnable()
    {
        SocketManager.Instance.OnBuyItemSuccess += UpdateInventory;
        GetInventoryAll(SlotType.Equipment);
        GetInventoryAll(SlotType.Consumption);
        GetInventoryAll(SlotType.Other);
    }
    void OnDisable()
    {
        SocketManager.Instance.OnBuyItemSuccess -= UpdateInventory;
    }

    public void UpdateInventory(BuyItemResponse response)
    {
        if (response.success)
        {
            ItemSpec spec = response.spec;
            ItemData data = ItemManager.Instance.GetItem(response.ItemId);

            Slot emptySlot = InventoryManager.instance.FindEmptySlot(data.type);
            if (emptySlot == null)
            {
                Debug.LogWarning("빈 슬롯이 없습니다.");
                return;
            }
            GameObject obj = Instantiate(ItemContainer, emptySlot.transform);
            OwnedItem item = obj.GetComponent<OwnedItem>();

            item.data = data;
            item.data.spec = spec;
            if (item.image == null)
                item.image = obj.GetComponent<Image>();

            item.image.sprite = item.data.icon;

            emptySlot.SetItem(item, response.purchasedItemCount);
            UpdatePurchasedItem(emptySlot);
        }
    }


    //플레이어의 인벤토리 정보를 모두 가져옴
    public void GetInventoryAll(SlotType type)
    {
        List<Slot> playerSlots = InventoryManager.instance.Inventory[type].Slots;

        Transform uiParent = type switch
        {
            SlotType.Equipment => EquipmentSlots,
            SlotType.Consumption => ConsumptionSlots,
            SlotType.Other => OtherSlots,
            _ => null
        };
        if(uiParent == null) { Debug.Log("존재하지 않는 인벤토리 타입"); return; }

        List<Slot> uiSlots = new(uiParent.GetComponentsInChildren<Slot>());

        for (int i = 0; i < playerSlots.Count; i++) { 
            Slot pSlot = playerSlots[i];
            Slot uSlot = uiSlots[i];
            uSlot.slotIndex = i;
            if (pSlot.hasItem)
            {
                if (uSlot.currentItem == null)
                {
                    GameObject obj = Instantiate(ItemContainer, uSlot.transform);
                    uSlot.currentItem = obj.GetComponent<OwnedItem>();
                }
                uSlot.currentItem.data = pSlot.currentItem.data;
                uSlot.currentItem.image.sprite = uSlot.currentItem.data.icon;
                uSlot.itemCount = pSlot.itemCount;
                uSlot.hasItem = true;
                uSlot.currentItem.gameObject.SetActive(true);
                uSlot.UpdateUI();
            }
            else
            {
                if (uSlot.currentItem != null)
                    uSlot.currentItem.gameObject.SetActive(false);
                uSlot.hasItem = false;
                uSlot.itemCount = 0;
            }
        }
    }

    void UpdatePurchasedItem(Slot slot)
    {
        Transform uiParent = slot.slotType switch
        {
            SlotType.Equipment => EquipmentSlots,
            SlotType.Consumption => ConsumptionSlots,
            SlotType.Other => OtherSlots,
            _ => null
        };
        if (uiParent == null) { Debug.Log("존재하지 않는 인벤토리 타입"); return; }

        Slot uSlot = uiParent.GetChild(slot.slotIndex).GetComponent<Slot>();


        if (slot.hasItem)
        {
            if (uSlot.currentItem == null)
            {
                GameObject obj = Instantiate(ItemContainer, uSlot.transform);
                uSlot.currentItem = obj.GetComponent<OwnedItem>();
            }
            uSlot.currentItem.data = slot.currentItem.data;
            uSlot.currentItem.image.sprite = uSlot.currentItem.data.icon;
            uSlot.itemCount = slot.itemCount;
            uSlot.hasItem = true;
            uSlot.currentItem.gameObject.SetActive(true);
            uSlot.UpdateUI();
        }
        else
        {
            if (uSlot.currentItem != null)
                uSlot.currentItem.gameObject.SetActive(false);
            uSlot.hasItem = false;
            uSlot.itemCount = 0;
        }
    }


}
