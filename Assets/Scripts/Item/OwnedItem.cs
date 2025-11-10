using System.Text;
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

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        countText = GetComponentInChildren<TextMeshProUGUI>();

        currentSlot = GetComponentInParent<Slot>();
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
        if (data == null) return;

        // 2. 스탯(ItemSpec)을 가져옵니다.
        ItemSpec stats = currentSlot.slotData.itemSpec;

        // 3. StringBuilder를 사용해 스탯 문자열을 만듭니다.
        StringBuilder statsBuilder = new StringBuilder();

        // 0보다 큰 스탯만 툴팁에 추가합니다.
        if (stats.damage > 0) statsBuilder.AppendLine($"공격력: {stats.damage}");
        if (stats.defense > 0) statsBuilder.AppendLine($"방어력: {stats.defense}");
        if (stats.speed > 0) statsBuilder.AppendLine($"속도: {stats.speed}");
        if (stats.hp > 0) statsBuilder.AppendLine($"체력: {stats.hp}");
        if (stats.duration > 0) statsBuilder.AppendLine($"지속시간: {stats.duration}초");
        if (stats.coolTime > 0) statsBuilder.AppendLine($"쿨타임: {stats.coolTime}초");

        string tooltipText = "";

        // 표시할 스탯이 하나라도 있다면
        if (statsBuilder.Length > 0)
        {
            // 설명과 스탯 사이에 공백 한 줄 추가
            tooltipText += statsBuilder.ToString().TrimEnd(); // 마지막 줄바꿈 제거
        }

        // 5. 완성된 툴팁 텍스트로 툴팁 표시
        PlayerUIManager.instance.ShowTooltip(tooltipText, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerUIManager.instance.HideTooltip();
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
