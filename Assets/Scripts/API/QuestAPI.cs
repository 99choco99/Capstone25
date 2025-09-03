using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class QuestAPI
{
    private MonoBehaviour coroutineRunner;
    private string userId;

    // 생성자를 통해 MonoBehaviour 인스턴스를 주입받습니다.
    public QuestAPI(MonoBehaviour runner, string userId)
    {
        coroutineRunner = runner;
        this.userId = userId;
    }

    IEnumerator GetQuestData()
    {
        string url = $"{APIConstants.BASE_API_URL}/quest/{userId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            Debug.Log("퀘스트 가져오기 시도");
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    QuestDataList response = JsonConvert.DeserializeObject<QuestDataList>(webRequest.downloadHandler.text);
                    QuestData[] questDataArray = response.quests;

                    APIEvents.OnGetQuestData?.Invoke(questDataArray);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("역직렬화 오류"+ ex.Message);
                }

            }
            else
            {
                Debug.LogError("퀘스트 가져오기 실패" + webRequest.error);
            }
        }
    }




    public void RequestGetQuestData()
    {
        coroutineRunner.StartCoroutine(GetQuestData());
    }

}

[System.Serializable]
public class QuestDataList
{
    public QuestData[] quests;
}
