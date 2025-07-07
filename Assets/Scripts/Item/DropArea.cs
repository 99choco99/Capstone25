using UnityEngine;
using UnityEngine.EventSystems;

public class DropArea : MonoBehaviour,IDropHandler
{
    PlayerData playerData;
    [SerializeField] GameObject DropItems; //드롭 아이템 모음
    OwnedItem selectedItem;  //인벤토리에서 선택한 아이템
    [SerializeField] Transform player;
    private void Start()
    {
        playerData = GetComponentInParent<PlayerData>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent<OwnedItem>(out selectedItem))
        {
            selectedItem.previousSlot.hasItem = false;
            selectedItem.previousSlot.currentItem = null;
            if (!InventoryManager.instance.Inventory[selectedItem.data.type].Item2.ContainsKey(selectedItem.previousSlot.slotIndex))
            {
                InventoryManager.instance.Inventory[selectedItem.data.type].Item2.Add(selectedItem.previousSlot.slotIndex, selectedItem.previousSlot.slotIndex);
            }

            GameObject dropItem = Instantiate(selectedItem.data.DropStatePrefab, DropItems.transform);
            dropItem.transform.position = player.transform.position;
            Destroy(selectedItem.gameObject);
        }
        if (eventData.pointerDrag.TryGetComponent(out EquipmentItem item) && item.previousSlot.slotType == SlotType.Profile) {
            item.TakeOff(playerData);
        }
    }
}
