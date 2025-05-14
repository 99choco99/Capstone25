using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlot : Slot
{
    public EquipmentType EquipmentType;

    override public void OnDrop(PointerEventData eventData)
    {
        eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem);

        if (newItem.data.type == ItemType.Equipment && newItem.data.equipmentType == EquipmentType)
        {
            newItem.Apply(playerData);
            base.OnDrop(eventData);
        }
    }
}
