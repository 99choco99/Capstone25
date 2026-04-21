using NUnit.Framework;
using System.Collections.Generic;

using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] List<ItemBase> ItemDatas;
    Dictionary<int?, ItemBase> Items;

    public static ItemManager Instance;
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { 
            Destroy(gameObject);
            return;
        }
        Items = new Dictionary<int?, ItemBase>();
        foreach (ItemBase data in ItemDatas)
        {
            Items[data.id] = data;
        }

    }

    public ItemBase GetItem(int? id)
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

    public int? RandomItem()
    {
        if (ItemDatas == null || ItemDatas.Count == 0)
        {
            Debug.LogWarning("아이템 데이터가 없습니다.");
            return null;
        }

        // 10% 확률로 ItemDatas 리스트에서 랜덤한 인덱스 선택
        if(Random.value <= 0.1f)
        {
            int randomIndex = Random.Range(0, ItemDatas.Count);
            return ItemDatas[randomIndex].id;
        }
        else
        {
            return null;
        }


    } 
}
