using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum SlotType { Equipment,Consumption,Other}
public abstract class Slot : MonoBehaviour, IPointerEnterHandler, IDropHandler, IPointerExitHandler
{
    public PlayerData playerData;
    public SlotType slotType;
    protected Image image;
    protected RectTransform rect;
    protected InventoryItem currentItem;  // 현재 창을 차지하고있는 아이템
    protected InventoryItem newItem;   // 새롭게 창을 차지할 아이템
    protected bool hasItem;

    private void Awake()
    {
        playerData = GetComponentInParent<PlayerData>();
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        if (newItem != null)
        {
            //창이 비어있을 때
            if (!hasItem)
            {
                newItem.previousSlot.hasItem = false;
            }
            else if (hasItem && currentItem)//창이 차있을 때
            {
                currentItem.transform.SetParent(newItem.previousSlot.transform);
                currentItem.transform.GetComponent<RectTransform>().position = newItem.previousSlot.rect.position;
                newItem.previousSlot.currentItem = currentItem;
            }
            newItem.transform.SetParent(transform);
            newItem.GetComponent<RectTransform>().position = rect.position;
            currentItem = newItem;
            currentItem.previousSlot = this;
            hasItem = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = Color.white;
    }
}
