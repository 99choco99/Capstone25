using Newtonsoft.Json;
// UniversalGraph v1 게임 연결 코드 보관본
using System;
using System.Collections;
using System.Collections.Generic;
using UniversalGraph;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class QuestAPI
{
    private string userId;

    public QuestAPI(string userId)
    {
        this.userId = userId;
    }

    //퀘스트 데이터 가져오기
    public async Awaitable<List<QuestProgress>> GetQuestData()
    {
        string url = $"{APIConstants.BASE_API_URL}/quest/{userId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<QuestProgress>>(webRequest.downloadHandler.text);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("역직렬화 오류"+ ex.Message);
                    return null;
                }

            }
            else
            {
                Debug.LogError("퀘스트 가져오기 실패" + webRequest.error);
                return null;
            }
        }
    }

    //퀘스트 진행사항 저장하기
    public async Awaitable<bool> SaveQuestStatus(List<QuestProgress> status)
    {
        string url = $"{APIConstants.BASE_API_URL}/quest/{userId}";
        string json = JsonConvert.SerializeObject(status);

        using (UnityWebRequest  webRequest = new UnityWebRequest(url, "POST")) 
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("퀘스트 저장 성공");
                return true;
            }
            else
            {
                Debug.LogError("퀘스트 저장 실패");
                return false;
            }
        
        }
    }

}


