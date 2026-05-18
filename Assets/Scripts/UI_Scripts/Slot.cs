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

        StringBuilder tooltipBuilder = new StringBuilder();


        //아이템 이름 및 설명
        if (!string.IsNullOrEmpty(slotData.itemData.description))
        {
            tooltipBuilder.AppendLine($"<color=#FFD700><b>{slotData.itemData.itemName}</b></color>");
            tooltipBuilder.AppendLine(slotData.itemData.description);
        }

        //아이템 스펙
        StringBuilder statsBuilder = new StringBuilder();


        if (slotData.itemData is EquipmentItemData)                 //장비 아이템
        {
            ItemSpec stats = slotData.itemSpec;

            if (stats.attackPower > 0) statsBuilder.AppendLine($"공격력: +{stats.attackPower}");
            if (stats.defense > 0) statsBuilder.AppendLine($"방어력: +{stats.defense}");
            if (stats.maxHp > 0) statsBuilder.AppendLine($"체력: +{stats.maxHp}");
        }
        else if (slotData.itemData is ConsumptionItemData consData) //소비 아이템
        {
            if (consData.healAmount > 0) statsBuilder.AppendLine($"회복량: {consData.healAmount}");
            if (consData.duration > 0) statsBuilder.AppendLine($"지속시간: {consData.duration}초");
            if (consData.coolTime > 0) statsBuilder.AppendLine($"쿨타임: {consData.coolTime}초");
        }

        if (statsBuilder.Length > 0)
        {
            if (tooltipBuilder.Length > 0) tooltipBuilder.AppendLine();
            tooltipBuilder.Append(statsBuilder.ToString().TrimEnd());
        }

        //기타 아이템
        if (tooltipBuilder.Length == 0)
        {
            tooltipBuilder.Append("");
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
