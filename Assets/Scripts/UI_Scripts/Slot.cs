using System;
using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



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

    //드랍을 했을 때
    public virtual void OnDrop(PointerEventData eventData)
    {
        OnDropRequest?.Invoke(this, eventData);
    }


    public void OnPointerEnter(PointerEventData eventData) => image.color = Color.yellow;
    public void OnPointerExit(PointerEventData eventData) => image.color = Color.white;
}
