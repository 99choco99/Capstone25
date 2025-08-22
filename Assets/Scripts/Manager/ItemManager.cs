using NUnit.Framework;
using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] List<ItemData> ItemDatas;
    Dictionary<int, ItemData> Items;

    public static ItemManager Instance;
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else { 
            Destroy(Instance);
            return;
        }
        Items = new Dictionary<int, ItemData>();
        foreach (ItemData data in ItemDatas)
        {
            Items[data.id] = data;
        }

    }

    public ItemData GetItem(int id)
    {
        return Items[id];
    }
}
