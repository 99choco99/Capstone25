using System;
using System.Collections.Generic;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other, Profile, Quick, Sale, Buy}
public class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public SlotType slotType;
    [SerializeField] protected Image image;
    [SerializeField] protected RectTransform rect;
    public event Action<Slot> OnChangedSlot;

    public OwnedItem currentItem;  // 현재 창을 차지하고있는 아이템
    public bool hasItem;           // 현재 아이템을 가지고 있는지
    public int slotIndex;          // 슬롯 번호
    public int itemCount;          // 아이템 개수

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        image = GetComponent<Image>();
        if (transform.childCount > 0)
        {
            hasItem = true;
            currentItem = transform.GetComponentInChildren<OwnedItem>();
            UpdateUI();
        }
    }

    private void OnEnable()
    {

    }
    // 슬롯 아이템 개수 UI 
    public void UpdateUI()
    {
        if (currentItem != null)
        {
            currentItem.UpdateCountUI(itemCount);
        }
    }

    //드랍을 했을 때
    public virtual void OnDrop(PointerEventData eventData)
    {
        OwnedItem draggedItem = eventData.pointerDrag?.GetComponent<OwnedItem>();

        if (draggedItem == null) { return; }

        Slot draggedItemSlot = draggedItem.currentSlot;

        if (hasItem)
        {
            SwapItem(draggedItem, draggedItemSlot);
        }
        else
        {
            MoveItemToEmptySlot(draggedItem, draggedItemSlot);
        }

    }


    //아이템을 빈슬롯으로 옮길때
    public void MoveItemToEmptySlot(OwnedItem draggedItem , Slot fromSlot)
    {
        SetItem(draggedItem, draggedItem.currentSlot.itemCount);

        fromSlot.Clear();

        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }
        UpdateItemPosition(draggedItem, transform, rect.position);

    }

    //아이템이 있는 슬롯끼리 교환
    void SwapItem(OwnedItem draggedItem, Slot sourceSlot)
    {
        //아이템 위치 교환
        UpdateItemPosition(currentItem, sourceSlot.transform, sourceSlot.rect.position);
        UpdateItemPosition(draggedItem, transform, rect.position);

        //현재 아이템과 개수를 교환
        (sourceSlot.currentItem, currentItem) = (currentItem, sourceSlot.currentItem);
        (sourceSlot.itemCount, itemCount) = (itemCount, sourceSlot.itemCount);

        //아이템의 슬롯 업데이트
        currentItem.currentSlot = this;
        sourceSlot.currentItem.currentSlot = sourceSlot;

        hasItem = true;
        sourceSlot.hasItem = true;

        sourceSlot.UpdateUI();
        UpdateUI();
    }

    // 아이템의 부모슬롯과 위치를 설정
    void UpdateItemPosition(OwnedItem item, Transform newSlotTransform = null, Vector3 newPosition = default)
    {
        item.transform.SetParent(newSlotTransform ?? transform);
        item.rect.position = newPosition == default ? rect.position : newPosition;
    }


    //현재 슬롯에 아이템 설정
    public void SetItem(OwnedItem item, int itemCount)
    {
        currentItem = item;
        this.itemCount = itemCount;
        currentItem.currentSlot = this;
        hasItem = true;
        UpdateUI();
        OnChangedSlot?.Invoke(this);
    }


    //해당 슬롯칸 비우기
    public void Clear()
    {
        currentItem = null;
        hasItem = false;
        itemCount = 0;
        UpdateUI();
        OnChangedSlot?.Invoke(this);
    }


    public void OnPointerEnter(PointerEventData eventData) => image.color = Color.yellow;
    public void OnPointerExit(PointerEventData eventData) => image.color = Color.white;
}
