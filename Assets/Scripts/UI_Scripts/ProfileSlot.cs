
using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileSlot : Slot
{
    Player player;
    [SerializeField] EquipmentType EquipmentSlotType;
    [SerializeField] EquipmentManager Equipment;


    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    override public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem))
        {
            if (newItem.data.type == SlotType.Equipment && newItem.data.equipmentType == EquipmentSlotType)
            {
                Equipment.Equip(EquipmentSlotType, newItem.currentSlot.slotData.itemSpec);
                base.OnDrop(eventData);
            }
        }

    }

    public EquipmentType GetEquipmentSlotType()
    {
        return EquipmentSlotType;
    }

}
