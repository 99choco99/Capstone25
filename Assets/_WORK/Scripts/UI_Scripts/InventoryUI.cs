using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : UIBase
{
    private InventoryManager inventory;

    [Header("프리팹 설정")]
    [SerializeField] private GameObject slotPrefab;

    [System.Serializable]
    public struct SlotLayoutGroup
    {
        public SlotType type;
        public Transform parentTransform;
    }

    [Header("슬롯 레이아웃 그룹 설정")]
    [SerializeField] private List<SlotLayoutGroup> layoutGroups;

    [Header("기타 UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI goldText;

    private Dictionary<SlotType, List<Slot>> uiSlots = new();
    private bool isInitialized = false;
    private Dictionary<SlotType, Transform> parentFolderDict = new();

    public override void Init()
    {
        parentFolderDict.Clear();
        foreach (var group in layoutGroups)
        {
            parentFolderDict[group.type] = group.parentTransform;
        }
    }

    public override void SetUp(Player IocalPlayer)
    {
        InventoryManager Inventory = IocalPlayer.Inventory;
        if (inventory != null)
        {
            inventory.OnSlotDataChanged -= UpdateSlotUI;
        }

        inventory = Inventory;
        inventory.OnSlotDataChanged += UpdateSlotUI;

        InitializeAllSlots();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnSlotDataChanged -= UpdateSlotUI;
        }
    }

    private void InitializeAllSlots()
    {
        if (isInitialized) return;
        foreach(var pair in inventory.SlotDict)
        {
            SlotType type = pair.Key;
            int count = pair.Value.Count;

            if (!parentFolderDict.TryGetValue(type, out Transform parentFolder)) { continue; }

            uiSlots[type] = new List<Slot>();

            for (int i = 0; i < count; i++)
            {
                GameObject slotObject = Instantiate(slotPrefab, parentFolder);
                Slot slot = slotObject.GetComponent<Slot>();
                slot.slotData = inventory.SlotDict[type][i];

                slot.OnDropRequest += OnDropHandler;

                uiSlots[type].Add(slot);

                UpdateSlotUI(type, i);
            }
        }

        isInitialized = true;
    }

    private void UpdateSlotUI(SlotType type, int index)
    {
        if (!uiSlots.ContainsKey(type)) { return; }
        Slot uiSlot = uiSlots[type][index];
        SlotData slotData = inventory.SlotDict[type][index];

        if (slotData.hasItem)
        {
            uiSlot.itemUI.gameObject.SetActive(true);

            //uiSlot.itemUI.image.sprite = Resources.Load<Sprite>();
            uiSlot.itemUI.UpdateCountUI(slotData.itemCount);
        }
        else
        {
            uiSlot.itemUI.image.sprite = null;
            uiSlot.itemUI.gameObject.SetActive(false);
        }
    }


    private void OnDropHandler(Slot draggedSlot, Slot droppedSlot)
    {
        if (draggedSlot == droppedSlot) { return; }

        inventory.RequestMoveItem(
            draggedSlot.slotData.slotType, draggedSlot.slotData.slotIndex,
            droppedSlot.slotData.slotType, droppedSlot.slotData.slotIndex
        );
    }

    private void UpdateGoldUI(int gold)
    {
        goldText.text = $"{gold} Gold";
    }

}