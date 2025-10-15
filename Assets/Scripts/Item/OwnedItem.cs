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
         
    GameObject ItemDescription;           //아이템 설명 박스
    TextMeshProUGUI ItemDescriptionText;  //아이템 설명
    [SerializeField] private TextMeshProUGUI countText; //현재 아이템 개수 표기


    public Slot currentSlot;             //현재 슬롯

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        countText = GetComponentInChildren<TextMeshProUGUI>();

        currentSlot = GetComponentInParent<Slot>();


    }

    private void OnEnable()
    {
        if (InventoryManager.instance == null)
        {
            Debug.LogError("InventoryManager.instance가 null입니다.");
            return;
        }
        ItemDescription = InventoryManager.instance.ItemDescription;
        ItemDescriptionText = ItemDescription.GetComponentInChildren<TextMeshProUGUI>(true);
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
            EquipmentManager.instance.Unequip(profileSlot.GetEquipmentSlotType());
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
                EquipmentManager.instance.Equip(profileSlot.GetEquipmentSlotType(), currentSlot.slotData.itemSpec);
            }
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
