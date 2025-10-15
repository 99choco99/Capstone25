using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {

        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem) && slotData.slotType == newItem.data.type)
        {
            base.OnDrop(eventData);
        }
    }

}

