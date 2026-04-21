using Newtonsoft.Json;
using System;


public enum SlotType { Equipment, Consumption, Other, Profile, Quick }

[Serializable]
public class SlotData
{

    public SlotType slotType;
    public int slotIndex;

    public string itemUID;

    public int? itemId;
    public int itemCount;

    public ItemSpec itemSpec;


    //아이템 기본 데이터
    [NonSerialized]
    [JsonIgnore]
    public ItemBase itemData;



    public bool hasItem => itemData != null;

    public void Clear()
    {
        itemId = null;
        itemUID = null;
        itemCount = 0;
        itemData = null;
        itemSpec = default(ItemSpec);
    }
}
