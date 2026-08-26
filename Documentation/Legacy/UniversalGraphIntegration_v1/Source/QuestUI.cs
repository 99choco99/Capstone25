using System;
// UniversalGraph v1 게임 연결 코드 보관본
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UniversalGraph;

public class QuestUI : UIBase
{
    PlayerQuestController controller;

    [SerializeField] GameObject questPrefab;                    //퀘스트 양식
    [SerializeField] Transform content;                         //퀘스트 목록
    [SerializeField] TextMeshProUGUI questNameText;             //선택된 퀘스트 이름
    [SerializeField] TextMeshProUGUI questDescriptionText;       //선택된 퀘스트의 메인 설명
    [SerializeField] TextMeshProUGUI questObjectiveText;        //선택된 퀘스트 단계별 설명
    [SerializeField] private Button abandonButton;               //퀘스트 포기 버튼

    private Dictionary<int, QuestUIItem> questUIItems = new Dictionary<int, QuestUIItem>();
    private int? selectedQuestId = null;

    public override void Init()
    {
        ClearQuestDetails();
    }

    public override void SetUp(Player localPlayer)
    {
        PlayerQuestController quest = localPlayer.Quest;
        if (controller != null)
        {
            controller.OnQuestStatusChanged -= HandleQuestStatusChanged;
        }

        this.controller = quest;
        if (this.controller != null)
        {
            this.controller.OnQuestStatusChanged += HandleQuestStatusChanged;
        }

        abandonButton.onClick.RemoveAllListeners();
        abandonButton.onClick.AddListener(() =>
        {
            if (selectedQuestId.HasValue)
            {
                controller.AbandonQuest(selectedQuestId.Value);
            }
        });

        InitializedList();
    }

    private void OnDestroy()
    {
        // 구독 해제
        if(controller != null)
        {
            controller.OnQuestStatusChanged -= HandleQuestStatusChanged;
        }
    }

    private void InitializedList()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        questUIItems.Clear();

        List<QuestProgress> questStatuses = controller.GetAllStatuses();
        foreach(QuestProgress status in questStatuses)
        {
            if(status.state != QuestState.Locked)
            {
                var data = QuestManager.Instance.GetQuestTemplate(status.questId);
                UpdateQuest(data, status);
            }
        }

    }


    //퀘스트 상태 변경시 발생하는 함수
    private void HandleQuestStatusChanged(UniversalGraph.QuestContainer data, QuestProgress status)
    {
        if (status.state != QuestState.Locked)
        {
            UpdateQuest(data, status);
        }
        if (selectedQuestId.HasValue && selectedQuestId.Value == data.id)
        {
            ShowQuestInfo(data.id);
        }
    }

    //퀘스트 상태 업데이트
    public void UpdateQuest(UniversalGraph.QuestContainer data, QuestProgress status)
    {
        if (questUIItems.TryGetValue(data.id, out var uiItem))
        {
            uiItem.UpdateUI(status);
        }
        else
        {
            GameObject newQuestUI = Instantiate(questPrefab, content);
            QuestUIItem newUiItem = newQuestUI.GetComponent<QuestUIItem>();

            newUiItem.Initialize(data,status, OnQuestItemSelected);
            questUIItems.Add(data.id, newUiItem);
        }
    }


    // 퀘스트 목록의 항목이 클릭되었을 때 호출될 콜백 함수
    public void OnQuestItemSelected(int questId)
    {
        selectedQuestId = questId;
        ShowQuestInfo(questId);
    }

    //퀘스트 정보 표시
    public void ShowQuestInfo(int questId)
    {
        UniversalGraph.QuestContainer data = QuestManager.Instance.GetQuestTemplate(questId);
        QuestProgress status = controller.GetQuestStatus(questId);

        if(data == null || status == null) { return; }

        //제목 및 메인 설명
        questNameText.text = data.questName;
        questDescriptionText.text = data.description;

        //목표 진행도
        string objectiveStr = "";

        if(status.state >= QuestState.InProgress)
        {
            objectiveStr = QuestEngineAPI.GetCurrentObjectiveText(controller, questId);
        }else if (status.state == QuestState.Ready)
        {
            objectiveStr = "- 해당 NPC를 찾아가 퀘스트를 수락하세요.";
        }

        questObjectiveText.text = objectiveStr;
        abandonButton.interactable = (status.state == QuestState.InProgress || status.state == QuestState.CanComplete);
    }

    public void ClearQuestDetails()
    {
        questDescriptionText.text = "";
        questObjectiveText.text = "";
        questNameText.text = "";
    }
}



