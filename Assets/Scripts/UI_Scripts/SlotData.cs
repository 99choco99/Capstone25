using Newtonsoft.Json;
using System;


[Serializable]
public class SlotData
{

    public SlotType slotType;
    public int slotIndex;
    public int? itemId;
    public ItemSpec itemSpec;

    [NonSerialized]
    [JsonIgnore]
    public ItemData itemData;


    public int itemCount;
    public bool hasItem => itemData != null;

    public void Clear()
    {
        itemId = 0;
        itemCount = 0;
        itemData = null;
        itemSpec = null; ;
    }
}