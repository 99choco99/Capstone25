using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileSlot : Slot
{
    PlayerData playerData;
    public EquipmentType EquipmentType;
    public PlayerProfile PlayerProfile;
    EquipmentItem currentEquippedItem;

    private void Start()
    {
        playerData = GetComponentInParent<PlayerData>();
        PlayerProfile = GetComponentInParent<PlayerProfile>();
    }
    override public void OnDrop(PointerEventData eventData)
    {
        eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem);
        if (newItem == null) { return; }
        if (newItem.data.type == SlotType.Equipment && newItem.data.equipmentType == EquipmentType)
        {
            EquipmentItem Item = (EquipmentItem)newItem;
            currentEquippedItem = (EquipmentItem)currentItem;
            if (currentEquippedItem != null) { currentEquippedItem.TakeOff(playerData); }
            Item.Equip(playerData);
            currentEquippedItem = Item;
            PlayerProfile.UpdateUI();
            base.OnDrop(eventData);
        }
    }

}
