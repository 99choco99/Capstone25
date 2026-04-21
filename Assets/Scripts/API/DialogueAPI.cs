using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DialogueAPI
{

    public async Awaitable<Dictionary<string, List<DialogueLine>>> GetDialogueData()
    {
        string url = $"{APIConstants.BASE_API_URL}/dialogue";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = webRequest.downloadHandler.text;
                    return JsonConvert.DeserializeObject<Dictionary<string, List<DialogueLine>>>(jsonText);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("역직렬화 오류" + ex.Message);
                    return null;
                }
            }
            else
            {
                Debug.LogError("대화문 가져오기 실패" + webRequest.error);
                return null;
            }
        }
    }
}
