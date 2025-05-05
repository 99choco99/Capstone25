using UnityEngine;

public class DropItem : Item, IInteractable
{
    ItemType type;

    public void Interact(Transform player)
    {
        Debug.Log("드롭된 아이템");
    }
}
