using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OwnedItem: Item, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler,IPointerExitHandler
{
    RectTransform rect;
    CanvasGroup canvasGroup;
    protected Transform canvas;
    TextMeshProUGUI ItemDescriptionText;
    public Slot previousSlot;
    protected InventoryManager inventory;

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().transform;
        canvasGroup = GetComponent<CanvasGroup>();
        inventory = GetComponentInParent<InventoryManager>();
        ItemDescriptionText = inventory.ItemDescription.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        previousSlot = transform.parent.GetComponent<Slot>();

        transform.SetParent(canvas);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (transform.parent == canvas)
        {
            transform.SetParent(previousSlot.transform);
            rect.position = previousSlot.GetComponent<RectTransform>().position;
        }

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        ItemDescriptionText.text = data.script;
        inventory.ItemDescription.transform.position = transform.position + Vector3.down * 50;
        inventory.ItemDescription.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventory.ItemDescription.SetActive(false);
    }
}
