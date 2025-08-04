using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OwnedItem: Item, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler,IPointerExitHandler
{
    CanvasGroup canvasGroup;
    protected Transform canvas;

    public RectTransform rect;
    public Image icon;                    //아이템 아이콘
         
    GameObject ItemDescription;           //아이템 설명 박스
    TextMeshProUGUI ItemDescriptionText;  //아이템 설명

    public Slot currentSlot;             //현재 슬롯

    public void Awake()
    {
        ItemDescription = InventoryManager.instance.ItemDescription;
        icon = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().transform;
        canvasGroup = GetComponent<CanvasGroup>();
        ItemDescriptionText = ItemDescription.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        currentSlot = transform.parent.GetComponent<Slot>();

        transform.SetParent(canvas);
        transform.SetAsLastSibling();

        SetAlphaValue(0.6f);
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
            transform.SetParent(currentSlot.transform);
            rect.position = currentSlot.GetComponent<RectTransform>().position;
        }

        SetAlphaValue(1.0f);
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        ItemDescriptionText.text = data.script;
        ItemDescription.transform.position = transform.position + Vector3.down * 50;
        ItemDescription.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemDescription.SetActive(false);
    }

    public void SetAlphaValue(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
}
