using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DialogueAPI
{
    private MonoBehaviour coroutineRunner;

    //대화 내용 가져오기
    public event Action<Dialogue[]> OnGetDialogue;

    public DialogueAPI(MonoBehaviour runner)
    {
        coroutineRunner = runner;
    }


    IEnumerator GetDialogueData()
    {
        string url = $"{APIConstants.BASE_API_URL}/dialogue";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Dialogue[] data = JsonConvert.DeserializeObject<Dialogue[]>(webRequest.downloadHandler.text);
                    OnGetDialogue?.Invoke(data);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("역직렬화 오류" + ex.Message);
                }
            }
            else
            {
                Debug.LogError("대화문 가져오기 실패" + webRequest.error);
            }

        }
    }


    public void RequestGetDialogue() => coroutineRunner.StartCoroutine(GetDialogueData());
}
