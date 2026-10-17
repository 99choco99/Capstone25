using Newtonsoft.Json;
using System;
using UnityEngine;

public class DataManager
{
    public static readonly DataManager Instance = new DataManager();

    [Header("현재 게임 데이터")]
    public PlayerData PlayerData;

    public const string SaveKey = "25Capstone.Save";

    private string validatedSaveJson;
    private bool cachedSaveValid;

    public bool HasLocalSave => PlayerPrefs.HasKey(SaveKey);
    public bool HasSavedGame
    {
        get
        {
            if (!HasLocalSave)
                return false;

            string json = PlayerPrefs.GetString(SaveKey);
            if (json != validatedSaveJson)
            {
                // 메뉴 갱신에서는 같은 저장의 유효성만 재사용하고, 실제 불러오기는 새로 읽습니다.
                cachedSaveValid = TryParseSavedPlayer(json, out _);
                validatedSaveJson = json;
            }

            return cachedSaveValid;
        }
    }

    /// <summary>이어하기는 정상적인 기존 기록만 불러오며, 읽기 실패 시 저장을 보존합니다.</summary>
    public void LoadSavedGame()
    {
        if (!TryReadSavedPlayer(out PlayerData data))
            throw new JsonSerializationException("로컬 저장 데이터를 읽을 수 없습니다. 기존 저장 기록은 유지합니다.");

        PlayerData = data;
    }

    /// <summary>새 게임의 진행 기록만 초기화합니다. 음량과 감도 설정은 유지합니다.</summary>
    public void ResetLocalData()
    {
        PlayerData initialData = CreateInitialPlayerData();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        PlayerData = initialData;
    }

    private static PlayerData CreateInitialPlayerData()
    {
        PlayerGrowthSettings settings = PlayerGrowthSettings.Current;
        PlayerData initialData = new()
        {
            level = settings.InitialLevel,
            maxHp = settings.InitialHealth,
            currentHp = settings.InitialHealth,
            maxPosture = settings.InitialPosture,
            attackPower = settings.InitialAttackPower
        };
        if (!IsValidPlayerData(initialData))
            throw new InvalidOperationException("초기 플레이어 설정이 올바르지 않습니다. 기존 저장 기록은 유지합니다.");

        return initialData;
    }

    private bool TryReadSavedPlayer(out PlayerData player)
    {
        player = null;
        if (!HasLocalSave)
            return false;

        return TryParseSavedPlayer(PlayerPrefs.GetString(SaveKey), out player);
    }

    private static bool TryParseSavedPlayer(string json, out PlayerData player)
    {
        player = null;
        try
        {
            PlayerData data = JsonConvert.DeserializeObject<LocalSaveData>(json)?.Player;
            if (!IsValidPlayerData(data))
                return false;

            player = data;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsValidPlayerData(PlayerData data)
    {
        return data != null && data.level >= 1 && data.exp >= 0 && data.abilityPoint >= 0
            && data.maxHp > 0f && data.maxPosture > 0f && data.attackPower > 0f
            && IsValidStat(data.maxHp) && IsValidStat(data.currentHp)
            && IsValidStat(data.maxPosture) && IsValidStat(data.attackPower);
    }

    private static bool IsValidStat(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>씬을 떠나거나 저장하기 전에 현재 플레이어의 수치를 가져옵니다.</summary>
    public void CapturePlayerData()
    {
        Player player = Player.LocalPlayer;
        if (player == null)
            return;

        PlayerStats stats = player.Stats;
        PlayerData.level = stats.Level;
        PlayerData.exp = stats.Exp;
        PlayerData.abilityPoint = stats.AbilityPoint;
        PlayerData.maxHp = stats.MaxHp.GetValue();
        PlayerData.currentHp = stats.CurrentHp;
        PlayerData.maxPosture = stats.MaxPosture.GetValue();
        PlayerData.attackPower = stats.AttackPower.GetValue();
    }

    /// <summary>현재 데이터를 JSON 문자열로 묶어 PlayerPrefs에 저장합니다.</summary>
    public void SaveLocalData()
    {
        // 빈 데이터로 기존 정상 저장을 덮어쓰지 않습니다.
        if (!IsValidPlayerData(PlayerData))
            throw new InvalidOperationException("저장할 플레이어 데이터가 없습니다. 기존 저장 기록은 유지합니다.");

        LocalSaveData data = new() { Player = PlayerData };
        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private class LocalSaveData
    {
        public PlayerData Player;
    }
}
