using Newtonsoft.Json;
using System;


public enum SlotType { Equipment, Consumption, Other, Profile, Quick }

[Serializable]
public class SlotData
{

    public SlotType slotType;
    public int slotIndex;
    public int itemCount;

    public ItemInstance itemData;

    public bool hasItem => itemData != null;

    public void Clear()
    {
        itemCount = 0;
        itemData = null;
    }
}
