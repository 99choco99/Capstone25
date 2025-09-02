using NUnit.Framework;
using System.Collections.Generic;

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
        if (Items.ContainsKey(id))
        {
            return Items[id];
        }
        else
        {
            Debug.Log("존재하지 않는 아이템");
            return null;
        }

    }
}
