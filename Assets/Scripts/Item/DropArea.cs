using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropArea : MonoBehaviour,IDropHandler
{
    [SerializeField] GameObject DropItems; //드롭 아이템 모음
    OwnedItem selectedItem;  //인벤토리에서 선택한 아이템
    [SerializeField] Transform player;
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent<OwnedItem>(out selectedItem))
        {
            selectedItem.previousSlot.hasItem = false;
            selectedItem.previousSlot.currentItem = null;
            if (!InventoryManager.instance.Inventory[selectedItem.data.type].Item2.ContainsKey(selectedItem.previousSlot.SlotIndex))
            {
                InventoryManager.instance.Inventory[selectedItem.data.type].Item2.Add(selectedItem.previousSlot.SlotIndex, selectedItem.previousSlot.SlotIndex);
            }

            GameObject dropItem = Instantiate(selectedItem.data.DropStatePrefab, DropItems.transform);
            dropItem.transform.position = player.transform.position;
            Destroy(selectedItem.gameObject);
        }
    }
}
