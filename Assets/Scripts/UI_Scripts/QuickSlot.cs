using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

public class QuickSlot : Slot
{

    public void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    public override void OnDrop(PointerEventData eventData)
    {
        base.OnDrop(eventData);
    }
}
