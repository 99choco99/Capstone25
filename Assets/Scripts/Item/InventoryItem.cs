using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItem: Item, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler,IPointerExitHandler
{

    RectTransform rect;
    CanvasGroup canvasGroup;
    Transform canvas;
    public Slot previousSlot;
    Inventory inventory;

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().transform;
        canvasGroup = GetComponent<CanvasGroup>();
        inventory = GetComponentInParent<Inventory>();
    }
    public virtual void Apply(PlayerData player)
    {

    }

    public virtual EquipmentType GetEquipmentType()
    {
        return EquipmentType.None;
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

    public void OnEndDrag(PointerEventData eventData)
    {
        if (transform.parent == canvas)
        {
            transform.SetParent(previousSlot.transform);
            rect.position = previousSlot.GetComponent<RectTransform>().position;
        }

        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inventory.ItemDescritpion.transform.position = transform.position + Vector3.down * 50;
        inventory.ItemDescritpion.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventory.ItemDescritpion.gameObject.SetActive(false);
    }
}
