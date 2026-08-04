using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemBaseConverter : JsonConverter
{
    public static readonly Dictionary<SlotType, Func<ItemBase>> baseCreators = new()
    {
        { SlotType.Equipment, () => new EquipmentBaseData() },
        { SlotType.Consumption, () => new ConsumptionBaseData() },
        { SlotType.Other, () => new OtherBaseData() }
    };


    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ItemBase);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        int type = jo["type"].Value<int>();
        SlotType slotType = (SlotType)type;

        ItemBase baseData;
        if(baseCreators.TryGetValue(slotType, out var creatorFunc))
        {
            baseData = creatorFunc();
        }
        else
        {
            baseData = new ItemBase();
        }

        return baseData;
    }

    public override bool CanWrite => false;
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
