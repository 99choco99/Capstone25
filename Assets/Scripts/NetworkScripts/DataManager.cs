using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SocketManager;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public PlayerData playerData;

    public event Action OnSavePlayerData;

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
    }
    

    public void SavePlayerData()
    {
        OnSavePlayerData?.Invoke();
    }

    public void LoadPlayerData(PlayerData data)
    {
        playerData = data;
    }


}
