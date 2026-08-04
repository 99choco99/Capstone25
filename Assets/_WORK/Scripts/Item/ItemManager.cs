using Newtonsoft.Json;
using System.Collections.Generic;

using UnityEngine;

public class ItemManager
{
    public static ItemManager Instance { get; private set; }


    private Dictionary<int, ItemBase> itemsRegistry;

    private ItemManager()
    {
        itemsRegistry = new Dictionary<int, ItemBase>();
    }

    public static void Init(string jsonString)
    {
        if (Instance != null) { return; }
        Instance = new ItemManager();
        Instance.LoadData(jsonString);
    }

    void LoadData(string jsonString)
    {
        itemsRegistry.Clear();
        List<ItemBase> parsedItems = JsonConvert.DeserializeObject<List<ItemBase>>(jsonString,new ItemBaseConverter());

        if (parsedItems == null) return;

        foreach (var item in parsedItems)
        {
            itemsRegistry[item.id] = item;
        }
    }

    public ItemBase GetItem(int? id)
    {
        if (id.HasValue && itemsRegistry.TryGetValue(id.Value, out var item))
        {
            return item;
        }
        return null;
    }
}
