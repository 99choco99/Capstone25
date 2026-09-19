using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public static readonly DataManager Instance = new DataManager();

    [Header("현재 게임 데이터")]
    public PlayerData PlayerData;
    public InventoryData InventoryData;

    public const string SaveKey = "25Capstone.Save";

    public event Action OnSave;

    public int GetMaxExpForLevel(int level)
    {
        return level * 10;
    }

    /// <summary>PlayerPrefs를 읽습니다. 저장 기록이 없는 첫 실행에서만 새 게임 데이터를 만듭니다.</summary>
    public void LoadLocalData()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            PlayerData = new PlayerData
            {
                level = 1,
                maxHp = 100f,
                currentHp = 100f,
                maxPosture = 100f,
                attackPower = 1f
            };
            InventoryData = new InventoryData { inventory = Array.Empty<SlotData>() };
            return;
        }

        LocalSaveData data = JsonConvert.DeserializeObject<LocalSaveData>(
            PlayerPrefs.GetString(SaveKey), new ItemInstanceConverter());
        if (data?.Player == null || data.Inventory?.inventory == null
            || data.Player.level < 1)
        {
            throw new JsonSerializationException("로컬 저장 데이터를 읽을 수 없습니다. 기존 저장 기록은 유지합니다.");
        }

        PlayerData = data.Player;
        InventoryData = data.Inventory;
    }

    /// <summary>씬을 떠나거나 저장하기 전에 현재 플레이어의 수치와 인벤토리를 가져옵니다.</summary>
    public void CapturePlayerData()
    {
        Player player = Player.LocalPlayer;
        if (player == null) return;

        PlayerStats stats = player.Stats;
        PlayerData.level = stats.Level;
        PlayerData.exp = stats.Exp;
        PlayerData.abilityPoint = stats.AbilityPoint;
        // 장비 보너스는 인벤토리를 불러올 때 적용하므로 기본 능력치만 저장합니다.
        PlayerData.maxHp = stats.MaxHp.GetBaseValue();
        PlayerData.currentHp = stats.CurrentHp;
        PlayerData.maxPosture = stats.MaxPosture.GetBaseValue();
        PlayerData.attackPower = stats.AttackPower.GetBaseValue();

        List<SlotData> slots = new();
        foreach (List<SlotData> slotList in player.Inventory.SlotDict.Values)
        {
            foreach (SlotData slot in slotList)
            {
                // 빈 슬롯은 InventoryManager가 생성하므로 아이템이 있는 슬롯만 보관합니다.
                if (slot.hasItem) slots.Add(slot);
            }
        }
        InventoryData = new InventoryData { inventory = slots.ToArray() };
    }

    /// <summary>현재 데이터를 JSON 문자열로 묶어 PlayerPrefs에 저장합니다.</summary>
    public void SaveLocalData()
    {
        LocalSaveData data = new() { Player = PlayerData, Inventory = InventoryData };
        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        OnSave?.Invoke();
    }

    private class LocalSaveData
    {
        public PlayerData Player;
        public InventoryData Inventory;
    }


}
