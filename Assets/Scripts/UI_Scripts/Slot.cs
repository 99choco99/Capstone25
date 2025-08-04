using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other, Profile, Quick, Sale, Buy}
public class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public SlotType slotType;
    [SerializeField] protected Image image;
    [SerializeField] protected RectTransform rect;


    public OwnedItem currentItem;  // 현재 창을 차지하고있는 아이템
    public bool hasItem;           // 현재 아이템을 가지고 있는지
    public int slotIndex;          // 슬롯 번호
    public int itemCount;          // 아이템 개수

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        OwnedItem draggedItem = eventData.pointerDrag.gameObject.GetComponent<OwnedItem>();

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


    void MoveItemToEmptySlot(OwnedItem draggedItem , Slot fromSlot)
    {

        itemCount = draggedItem.currentSlot.itemCount;
        SetItem(draggedItem);

        fromSlot.currentItem = null;
        fromSlot.hasItem = false;
        fromSlot.itemCount = 0;

        UpdateItemPosition(draggedItem, transform, rect.position);
    }

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
    }

    // 아이템의 부모슬롯과 위치를 설정
    void UpdateItemPosition(OwnedItem item, Transform newSlotTransform = null, Vector3 newPosition = default)
    {
        item.transform.SetParent(newSlotTransform ?? transform);
        item.transform.position = newPosition == default ? rect.position : newPosition;
    }

    public void SetItem(OwnedItem item)
    {
        currentItem = item;
        currentItem.currentSlot = this;
        hasItem = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = Color.white;
    }
}
