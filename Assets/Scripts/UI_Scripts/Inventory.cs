using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    ScrollRect ScrollRect;
    public Image ItemDescritpion;
    InventoryItem currentItem;

    private void Awake()
    {
        ScrollRect = GetComponent<ScrollRect>();
    }

    public void ShowInvenType(RectTransform InventoryType)
    {
        ScrollRect.content = InventoryType;
    }
}
