using UnityEngine;

public class DropItem : Item, IInteractable
{
    public string InteractionPrompt => gameObject.name;
    public void Interact(Player player)
    {
        //SlotData slot = player.SlotDict.FindEmptySlot(data.type);

        //slot.itemCount += data.count;
        Destroy(gameObject);
    }
}
