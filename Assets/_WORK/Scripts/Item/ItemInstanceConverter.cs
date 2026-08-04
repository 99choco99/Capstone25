using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

public class ItemInstanceConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ItemInstance);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        int id = jo["templateId"].Value<int>();
        ItemBase baseData = ItemManager.Instance.GetItem(id);
        if (baseData == null) { return null; }

        ItemInstance item = ItemFactory.CreateInstance(baseData);
        if (item == null) { return null; }

        serializer.Populate(jo.CreateReader(), item);
        return item;
    }

    public override bool CanWrite => false;
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
