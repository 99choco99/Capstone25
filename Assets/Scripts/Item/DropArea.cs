using UnityEngine;
using UnityEngine.EventSystems;

public class DropArea : MonoBehaviour,IDropHandler
{
    PlayerSetting playerData;
    [SerializeField] GameObject DropItems; //드롭 아이템 모음
    OwnedItem selectedItem;  //인벤토리에서 선택한 아이템
    [SerializeField] Transform player;
    private void Start()
    {
        playerData = GetComponentInParent<PlayerSetting>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent<OwnedItem>(out selectedItem))
        {
            if(selectedItem.currentSlot.itemCount <= 0)
            {
                selectedItem.currentSlot.currentItem = null;
                selectedItem.currentSlot.hasItem = false;
                InventoryManager.instance.Inventory[selectedItem.data.type].EmptySlots.Add(selectedItem.currentSlot.slotIndex);
            }

            //GameObject dropItem = Instantiate(selectedItem.data.DropStatePrefab, DropItems.transform);
            Vector3 dropPos = player.transform.position + player.forward * 1.0f;
            dropPos += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            //dropItem.transform.position = dropPos;
            Destroy(selectedItem.gameObject);
        }

        if (eventData.pointerDrag.TryGetComponent(out EquipmentItem item) && item.currentSlot.slotType == SlotType.Profile) {
            item.TakeOff(playerData);
        }
    }
}
