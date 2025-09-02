using System;
using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other, Profile, Quick}
public class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public SlotData slotData;

    [SerializeField] protected Image image;
    [SerializeField] protected RectTransform rect;

    public event Action<Slot, PointerEventData> OnDropRequest;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    // 슬롯 아이템 개수 UI 
    public void UpdateUI()
    {
        if (slotData != null)
        {
            OwnedItem ownedItem = transform.GetComponentInChildren<OwnedItem>();

            if (ownedItem != null)
            {
                ownedItem.data = slotData.itemData;
                ownedItem.image.sprite = slotData.itemData.icon;
                ownedItem.currentSlot = this;
                ownedItem.UpdateCountUI(slotData.itemCount);
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    //드랍을 했을 때
    public virtual void OnDrop(PointerEventData eventData)
    {
        OnDropRequest?.Invoke(this, eventData);
    }


    public void OnPointerEnter(PointerEventData eventData) => image.color = Color.yellow;
    public void OnPointerExit(PointerEventData eventData) => image.color = Color.white;
}
