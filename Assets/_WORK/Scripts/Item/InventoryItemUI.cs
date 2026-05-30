using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI: MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler,IPointerExitHandler
{
    
    protected Transform canvasTransform;

    [SerializeField] private RectTransform rect;
    [SerializeField] public Image image;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI countText; //현재 아이템 개수 표기


    public Slot ParentSlot{ get; private set; }

    public void Awake()
    {
        canvasTransform = GetComponentInParent<Canvas>().transform;
        ParentSlot = GetComponentInParent<Slot>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(canvasTransform);
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
        if (transform.parent == canvasTransform)
        {
            transform.SetParent(ParentSlot.transform);
            rect.position = ParentSlot.GetComponent<RectTransform>().position;
        }

        SetAlphaValue(1.0f);
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        ParentSlot?.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ParentSlot?.OnPointerExit(eventData);
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
        ParentSlot = null;
    }
}
