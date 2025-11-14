using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PublicAPIManager;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public PlayerData playerData;
    public bool canSave = true;
    public event Action OnSave;
    public event Action OnPlayerRegistered;

    public Player Player { get; private set; }
    public InventoryManager Inventory { get; private set; }
    public PlayerStats Stats { get; private set; }
    public LocalAPIManager LocalAPI { get; private set; }



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


    public void Register(Player localPlayer)
    {
        if (Player != null)
        {
            Unregister();
        }
        if (localPlayer == null)
        {
            Debug.LogError("로컬 플레이어 없음", this);
            return;
        }

        Player = localPlayer;
        Inventory = localPlayer.Inventory;
        Stats = localPlayer.Stats;
        LocalAPI = localPlayer.localAPI;

        if (Inventory == null || Stats == null || LocalAPI == null)
        {
            Debug.LogError("플레이어 참조 문제 발생", localPlayer);
        }
        OnPlayerRegistered?.Invoke();
    }

    public void Unregister()
    {
        // 모든 참조를 null로 설정
        Player = null;
        Inventory = null;
        Stats = null;
        LocalAPI = null;

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
