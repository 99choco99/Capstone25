using System;
using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other, Profile, Quick, Sale, Buy}
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
        if (slotData != null && slotData.hasItem)
        {
            OwnedItem ownedItem = transform.GetComponentInChildren<OwnedItem>();

            if (ownedItem != null)
            {
                slotData.currentItemData = ownedItem.data;
                ownedItem.image.sprite = slotData.currentItemData.icon;
                ownedItem.currentSlot = this;
                ownedItem.UpdateCountUI(slotData.itemCount);
            }
        }
        else
        {
            // 슬롯 데이터가 비어있으면 UI를 비활성화
            // 자식 오브젝트(아이템 아이콘)를 모두 제거합니다.
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
