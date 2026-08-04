using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public SlotData slotData;

    [SerializeField] protected Image image;
    [SerializeField] public InventoryItemUI itemUI;// 아이템 UI를 빠르게 찾기 위한 캐싱

    public event Action<Slot, Slot> OnDropRequest;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (itemUI == null) itemUI = GetComponentInChildren<InventoryItemUI>(true);
    }

    //드랍을 했을 때
    public virtual void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent(out InventoryItemUI draggedItemUI))
        {
            OnDropRequest?.Invoke(draggedItemUI.ParentSlot, this);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = Color.yellow;

        if (slotData == null || !slotData.hasItem) return;

        StringBuilder tooltipBuilder = new();
        ItemBase baseData = slotData.itemData.BaseData;

        //아이템 이름 및 설명
        if (!string.IsNullOrEmpty(baseData.description))
        {
            tooltipBuilder.AppendLine($"<color=#FFD700><b>{baseData.itemName}</b></color>");
            tooltipBuilder.AppendLine(baseData.description);
        }

        string instanceStats = slotData.itemData.GetToolTipText();


        if (!string.IsNullOrEmpty(instanceStats))
        {
            tooltipBuilder.AppendLine();
            tooltipBuilder.Append(instanceStats);
        }
        // 최종 렌더링
        TooltipManager.Instance.ShowTooltip(tooltipBuilder.ToString().TrimEnd(), transform.position);
    }
    public void OnPointerExit(PointerEventData eventData) 
    {
        image.color = Color.white;
        TooltipManager.Instance.HideTooltip();
    }
}
