using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MarketInventoryUI : MonoBehaviour
{
    private Player player;

    [SerializeField] private InventoryManager Inventory;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform equipmentParent;
    [SerializeField] private Transform consumptionParent;
    [SerializeField] private Transform otherParent;
    [SerializeField] private TextMeshProUGUI goldText;

    private Dictionary<SlotType, List<Slot>> uiSlots = new();

    private void Awake()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("MarketInventoryUI: DataManager.Instance가 없습니다!");
            return;
        }
        DataManager.Instance.OnPlayerRegistered += InitializePlayerSubscriptions;

        player = DataManager.Instance.Player;
    }

    // OnPlayerRegistered 이벤트가 발생했을 때(Player가 준비됐을 때) 호출될 메서드
    private void InitializePlayerSubscriptions()
    {
        if (player != null)
        {
            // 이전 Player의 이벤트에서 구독을 해제
            Inventory.OnInventoryDataInitialized -= InitUI;
            Inventory.OnSlotDataChanged -= UpdateSlotUI;
            PlayerStats.OnLocalPlayerGoldChanged -= UpdateGoldUI;
        }
        // 이전 씬에서 만든 모든 슬롯 UI를 파괴
        foreach (var slotList in uiSlots.Values)
        {
            foreach (Slot slot in slotList)
            {
                // 씬 이동으로 이미 파괴되었을 수 있으므로, null이 아닌지 확인
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
        }
        // 슬롯 딕셔너리를 깨끗하게 비웁니다.
        uiSlots.Clear();

        player = DataManager.Instance.Player;

        if (player == null || Inventory == null || player.Stats == null)
        {
            Debug.LogError("MarketInventoryUI: Player 등록 이벤트가 발생했으나 참조가 여전히 null입니다.");
            return;
        }

        Inventory.OnInventoryDataInitialized += InitUI;
        Inventory.OnSlotDataChanged += UpdateSlotUI;
        PlayerStats.OnLocalPlayerGoldChanged += UpdateGoldUI;
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            Inventory.OnInventoryDataInitialized -= InitUI;
            Inventory.OnSlotDataChanged -= UpdateSlotUI;
            PlayerStats.OnLocalPlayerGoldChanged -= UpdateGoldUI;
        }
    }

    private void InitUI(SlotType type, int count)
    {
        Transform parent = GetParentForType(type);
        if (parent == null) { return; }

        uiSlots[type] = new List<Slot>();

        for (int i = 0; i < count; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, parent);
            Slot slot = slotObject.GetComponent<Slot>();

            slot.slotData = Inventory.Inventory[type][i];

            slot.OnDropRequest += OnDropHandler;

            uiSlots[type].Add(slot);
        }

    }

    private void OnDropHandler(Slot droppedSlot, PointerEventData eventData)
    {
        OwnedItem draggedItemUI = eventData.pointerDrag?.GetComponent<OwnedItem>();
        if(draggedItemUI == null) { return; }
        draggedItemUI.transform.SetParent(draggedItemUI.currentSlot.transform);
        draggedItemUI.transform.localPosition = Vector3.zero;
    }

    private void UpdateSlotUI(SlotType type, int index)
    {
        if (!uiSlots.ContainsKey(type)) { return; }
        Slot uiSlot = uiSlots[type][index];
        SlotData slotData = Inventory.Inventory[type][index];

        if (slotData.hasItem)
        {
            // 기존 아이템 UI를 찾기
            OwnedItem ownedItem = uiSlot.GetComponentInChildren<OwnedItem>();

            // 아이템 UI가 없다면 새로 생성
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
                ownedItem.currentSlot.slotData = slotData;
                ownedItem.UpdateCountUI(slotData.itemCount);
            }
        }
        else
        {
            foreach (Transform child in uiSlot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        
    }

    private void UpdateGoldUI(int gold)
    {
        goldText.text = $"{gold} Gold";
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
