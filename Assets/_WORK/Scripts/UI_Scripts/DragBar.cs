using UnityEngine;
using UnityEngine.EventSystems;

public class DragBar : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {

    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.parent.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {

    }
}
