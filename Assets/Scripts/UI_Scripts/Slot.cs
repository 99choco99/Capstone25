using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other, Profile, Quick, Sale, Buy}
public class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public SlotType slotType;
    protected Image image;
    public RectTransform rect;
    public OwnedItem currentItem;  // 현재 창을 차지하고있는 아이템
    protected OwnedItem newItem;   // 새롭게 창을 차지할 아이템
    public bool hasItem;
    public int slotIndex;
    public int itemCount;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        if (newItem != null)
        {
            //창이 비어있을 때
            if (!hasItem)
            {
                newItem.previousSlot.hasItem = false;
                newItem.previousSlot.currentItem = null;
                itemCount = newItem.previousSlot.itemCount;
                newItem.previousSlot.itemCount = 0;
            }
            else//창이 차있을 때
            {
                currentItem.transform.SetParent(newItem.previousSlot.transform);
                currentItem.rect.position = newItem.previousSlot.rect.position;
                newItem.previousSlot.currentItem = currentItem;
                (newItem.previousSlot.itemCount, itemCount) = (itemCount, newItem.previousSlot.itemCount);
            }
            newItem.transform.SetParent(transform);
            newItem.rect.position = rect.position;
            currentItem = newItem;
            currentItem.previousSlot = this;
            hasItem = true;
        }
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
