using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Networking;

public class InventoryAPI
{
    private string userId;

    public InventoryAPI(string userId)
    {
        this.userId = userId;
    }

    public async Awaitable<InventoryData> GetInventoryItem()
    {
        string url = $"{APIConstants.BASE_API_URL}/playerData/inventory/{userId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();
            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = webRequest.downloadHandler.text;

                    return JsonConvert.DeserializeObject<InventoryData>(jsonResponse);
                }catch(JsonException ex)
                {
                    Debug.Log("역직렬화 오류"  + ex.Message);
                    return null;
                }

            }
            return null;
        }

    }

    public async Awaitable<bool> SaveInventoryItem(SlotData slotData)
    {
        string url = $"{APIConstants.BASE_API_URL}/playerData/inventory/{userId}";

        string jsonData = JsonConvert.SerializeObject(slotData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if(webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                return false;
            }
            return true;
        }
    }

}

public class InventoryData{
    public SlotData[] inventory;
}

