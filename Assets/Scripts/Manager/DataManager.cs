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
    public QuestData Server_QuestData;
    public Dictionary<string, List<DialogueLine>> Server_DialogueData;
    public InventoryData Server_InventoryData;

    [SerializeField, Tooltip("연속 저장 방지 쿨타임 (초)")]
    private float saveCooldown = 3f;

    public event Action OnSave;
    public event Action OnPlayerRegistered;

    public int GetMaxExpForLevel(int level)
    {
        return level * 10;
    }


}
