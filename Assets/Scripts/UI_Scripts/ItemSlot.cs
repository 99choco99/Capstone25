using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    PlayerStats playerData;
    private void Start()
    {
        playerData = GetComponentInParent<PlayerStats>();
    }
    public override void OnDrop(PointerEventData eventData)
    {
        
        if (eventData.pointerDrag.TryGetComponent(out EquipmentItem item) && item.currentSlot.slotData.slotType == SlotType.Profile)
        {
            item.TakeOff(playerData);
        }
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem) && slotData.slotType == newItem.data.type)
        {
            base.OnDrop(eventData);
        }
    }

}

