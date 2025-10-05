
using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileSlot : Slot
{
    [SerializeField] EquipmentType EquipmentSlotType;


    override public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem))
        {
            if (newItem.data.type == SlotType.Equipment && newItem.data.equipmentType == EquipmentSlotType)
            {
                EquipmentManager.instance.Equip(EquipmentSlotType, newItem.currentSlot.slotData.itemSpec);
                base.OnDrop(eventData);
            }
        }

    }

    public EquipmentType GetEquipmentSlotType()
    {
        return EquipmentSlotType;
    }

}
