using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static APIManager;

public class DataManager
{
    public static readonly DataManager Instance = new DataManager();

    [Header("서버 데이터")]
    public PlayerData Server_PlayerData;
    public List<QuestProgress> Server_QuestProgress;
    public InventoryData Server_InventoryData;

    public event Action OnSave;

    public int GetMaxExpForLevel(int level)
    {
        return level * 10;
    }


}
