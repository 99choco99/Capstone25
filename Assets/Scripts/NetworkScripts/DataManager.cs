using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static APIManager;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public PlayerData playerData { get; private set; }

    public event Action OnSave;

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
        InvokeRepeating("AutoSaveData", 10f, 10f);
    }
    

    public void Save()
    {
        OnSave?.Invoke();
    }

    public void LoadPlayerData(PlayerData data)
    {
        playerData = data;
    }

    //플레이어 데이터 자동 저장
    private void AutoSaveData()
    {
        Save();
        APIManager.Instance.PlayerData.RequestSavePlayerData(playerData);
    }

    public int GetMaxExpForLevel(int level)
    {
        return 1;
    }


}
