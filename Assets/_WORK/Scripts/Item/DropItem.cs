using UnityEngine;

public class DropItem : Item
{
    public string InteractionPrompt => gameObject.name;
    public void Interact(Player player)
    {
        //SlotData slot = entitiy.SlotDict.FindEmptySlot(data.type);

        //slot.itemCount += data.currentAmount;
        Destroy(gameObject);
    }
}
