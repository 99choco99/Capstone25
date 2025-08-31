using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    [SerializeField] GameObject questPrefab;
    [SerializeField] Transform content;
    [SerializeField] TextMeshProUGUI questNameText;
    [SerializeField] TextMeshProUGUI questGuideText;

    private Dictionary<string, GameObject> questUIList = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        // 퀘스트 이벤트 구독
        QuestEvents.OnQuestUnlocked += AddNewQuest;
    }

    private void OnDisable()
    {
        // 구독 해제
        QuestEvents.OnQuestUnlocked -= AddNewQuest;
    }


    public void AddNewQuest(QuestData data)
    {
        // 이미 존재하는 퀘스트인지 확인
        if (questUIList.ContainsKey(data.questName)) return;

        // 프리팹 생성 및 데이터 설정
        GameObject newQuestUI = Instantiate(questPrefab, content);

        // UI 아이템 스크립트에 데이터 전달
        QuestUIItem uiItem = newQuestUI.GetComponent<QuestUIItem>();
        if (uiItem != null)
        {
            uiItem.Initialize(data);
            uiItem.button.onClick.AddListener(() => ShowQuestInfo(data));
        }

        // 딕셔너리에 추가
        questUIList.Add(data.questName, newQuestUI);
    }

    public void ShowQuestInfo(QuestData data)
    {
        questGuideText.text = data.script;
        questNameText.text = data.questName;
    }

    public void RemoveQuest(string questName)
    {
        if (questUIList.ContainsKey(questName))
        {
            Destroy(questUIList[questName]);
            questUIList.Remove(questName);
            questGuideText.text = "";
            questNameText.text = "";
        }
    }
}
