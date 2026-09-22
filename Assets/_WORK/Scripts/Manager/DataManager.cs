using Newtonsoft.Json;
using System;
using UnityEngine;

public class DataManager
{
    public static readonly DataManager Instance = new DataManager();

    [Header("현재 게임 데이터")]
    public PlayerData PlayerData;

    public const string SaveKey = "25Capstone.Save";

    public event Action OnSave;

    public int GetMaxExpForLevel(int level)
    {
        return level * 10;
    }

    /// <summary>PlayerPrefs를 읽습니다. 저장 기록이 없거나 Player가 null이면 기본 데이터를 만듭니다.</summary>
    public void LoadLocalData()
    {
        LocalSaveData data = PlayerPrefs.HasKey(SaveKey)
            ? JsonConvert.DeserializeObject<LocalSaveData>(PlayerPrefs.GetString(SaveKey))
            : null;

        if (data?.Player == null)
        {
            PlayerData = new PlayerData
            {
                level = 1,
                maxHp = 100f,
                currentHp = 100f,
                maxPosture = 100f,
                attackPower = 1f
            };
            return;
        }

        if (data.Player.level < 1)
        {
            throw new JsonSerializationException("로컬 저장 데이터를 읽을 수 없습니다. 기존 저장 기록은 유지합니다.");
        }

        PlayerData = data.Player;
    }

    /// <summary>씬을 떠나거나 저장하기 전에 현재 플레이어의 수치를 가져옵니다.</summary>
    public void CapturePlayerData()
    {
        Player player = Player.LocalPlayer;
        if (player == null) return;

        PlayerStats stats = player.Stats;
        PlayerData.level = stats.Level;
        PlayerData.exp = stats.Exp;
        PlayerData.abilityPoint = stats.AbilityPoint;
        PlayerData.maxHp = stats.MaxHp.GetBaseValue();
        PlayerData.currentHp = stats.CurrentHp;
        PlayerData.maxPosture = stats.MaxPosture.GetBaseValue();
        PlayerData.attackPower = stats.AttackPower.GetBaseValue();
    }

    /// <summary>현재 데이터를 JSON 문자열로 묶어 PlayerPrefs에 저장합니다.</summary>
    public void SaveLocalData()
    {
        // 빈 데이터로 기존 정상 저장을 덮어쓰지 않습니다.
        if (PlayerData == null || PlayerData.level < 1)
            throw new InvalidOperationException("저장할 플레이어 데이터가 없습니다. 기존 저장 기록은 유지합니다.");

        LocalSaveData data = new() { Player = PlayerData };
        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        OnSave?.Invoke();
    }

    private class LocalSaveData
    {
        public PlayerData Player;
    }


}
