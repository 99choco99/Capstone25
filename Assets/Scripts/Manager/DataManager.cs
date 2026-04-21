using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static APIManager;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public PlayerData Server_PlayerData;
    public QuestData Server_QuestData;
    public Dictionary<string, List<DialogueLine>> Server_DialogueData;
    public InventoryData Server_InventoryData;


    public bool canSave = true;
    public event Action OnSave;
    public event Action OnPlayerRegistered;

    public Player Player { get; private set; }
    public InventoryManager Inventory { get; private set; }
    public PlayerStats Stats { get; private set; }



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        InvokeRepeating("AutoSaveData", 10.0f, 10.0f);
    }

    //플레이어 데이터 자동 저장
    private void AutoSaveData()
    {
        if (!canSave || Stats == null || Stats.dead) { return; }
        OnSave?.Invoke();
    }

    public void SaveData()
    {
        if (Stats == null || Stats.dead) { return; }
        OnSave?.Invoke();
        StartCoroutine(StopSaveCoroutine());
    }

    IEnumerator StopSaveCoroutine()
    {
        canSave = false;
        yield return new WaitForSeconds(5f);
        canSave = true;
    }

    public int GetMaxExpForLevel(int level)
    {
        return level * 10;
    }


}
