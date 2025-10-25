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
    public Image image;
         
    [SerializeField] private TextMeshProUGUI countText; //현재 아이템 개수 표기


    public Slot currentSlot;             //현재 슬롯
    private MarketInventoryUI rootWindow;

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        countText = GetComponentInChildren<TextMeshProUGUI>();

        currentSlot = GetComponentInParent<Slot>();
        rootWindow = GetComponentInParent<MarketInventoryUI>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        currentSlot = transform.parent.GetComponent<Slot>();
        canvas = GetComponentInParent<Canvas>().transform;

        transform.SetParent(canvas);
        transform.SetAsLastSibling();

        SetAlphaValue(0.6f);
        canvasGroup.blocksRaycasts = false;

        if (currentSlot is ProfileSlot profileSlot)
        {
            Player player = GetComponentInParent<Player>();
            player.Equipment.Unequip(profileSlot.GetEquipmentSlotType());
        }
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
            if (currentSlot is ProfileSlot profileSlot)
            {
                Player player = GetComponentInParent<Player>();
                player.Equipment.Equip(profileSlot.GetEquipmentSlotType(), currentSlot.slotData.itemSpec);
            }
        }

        SetAlphaValue(1.0f);
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        rootWindow.ShowTooltip(data.script, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rootWindow.HideTooltip();
    }

    public void SetAlphaValue(float alpha)
    {
        canvasGroup.alpha = alpha;
    }


    public void UpdateCountUI(int count)
    {
        if (countText == null) return;
        countText.text = count >= 1 ? count.ToString() : "";
    }

    private void OnDestroy()
    {
        image.sprite = null;

        currentSlot = null;
    }
}
