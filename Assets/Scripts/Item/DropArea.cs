using UnityEngine;
using UnityEngine.EventSystems;

public class DropArea : MonoBehaviour,IDropHandler
{
    [SerializeField] GameObject DropItems; //드롭 아이템 모음
    DropItem droppedItem;  
    InventoryItem selectedItem;  //인벤토리에서 선택한 아이템
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent<InventoryItem>(out selectedItem))
        {
            Instantiate(droppedItem, droppedItem.transform);
            Destroy(selectedItem);
        }
    }

}
