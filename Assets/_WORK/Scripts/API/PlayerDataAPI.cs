using Newtonsoft.Json;
using SocketIOClient.Messages;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


//플레이어 데이터를 관리하는 클래스.
public class PlayerDataAPI
{
    private readonly string userId;

    public PlayerDataAPI(string userId) { this.userId = userId; }

    // 플레이어 데이터 요청
    public async Awaitable<PlayerData> LoadPlayerData()
    {
        string url = $"{APIConstants.BASE_API_URL}/playerData/{userId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = webRequest.downloadHandler.text;
                    Debug.Log(jsonText);
                    return JsonConvert.DeserializeObject<PlayerData>(jsonText); 
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                    return null;
                }
            }
            else
            {
                Debug.LogError($"플레이어 데이터 로드 실패: {webRequest.error}");
                return null;
            }
        }
    }

    //플레이어 데이터 저장
     public async Awaitable<bool> SavePlayerData(PlayerData data)
    {
        string url = $"{APIConstants.BASE_API_URL}/playerData/{userId}";
        string json = JsonConvert.SerializeObject(data);

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Client {userId} : 데이터 저장 완료");
                return true;
            }
            else
            {
                Debug.LogError("데이터 저장 실패: " + webRequest.error);
                return false;
            }
        }

    }

}
