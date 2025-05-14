using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

public class QuickSlot : Slot
{
    public override void OnDrop(PointerEventData eventData)
    {
        eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem);
        if ((int)slotType == (int)newItem.data.type)
        {
            base.OnDrop(eventData);
        }
    }
}
