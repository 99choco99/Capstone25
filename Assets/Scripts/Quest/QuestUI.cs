using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    [SerializeField] GameObject questPrefab;                    //퀘스트 양식
    [SerializeField] Transform content;                         //퀘스트 목록
    [SerializeField] TextMeshProUGUI questNameText;             //선택된 퀘스트 이름
    [SerializeField] TextMeshProUGUI questMainScriptText;       //선택된 퀘스트의 메인 설명
    [SerializeField] TextMeshProUGUI queststepDescriptionText;  //선택된 퀘스트 단계별 설명
    [SerializeField] private Button acceptButton;               //퀘스트 선택 버튼

    private Dictionary<int, QuestUIItem> questUIItems = new Dictionary<int, QuestUIItem>();
    private int? selectedQuestId = null;

    private void OnEnable()
    {

        // 퀘스트 이벤트 구독
        QuestManager.Instance.OnQuestStatusChanged += HandleQuestStatusChanged;

        InitializedList();
    }

    private void OnDisable()
    {
        // 구독 해제
        QuestManager.Instance.OnQuestStatusChanged -= HandleQuestStatusChanged;
        selectedQuestId = null;
    }

    private void InitializedList()
    {
        foreach(Transform child in content)
        {
            Destroy(child.gameObject);
        }
        questUIItems.Clear();

        List<QuestStatus> questStatuses = QuestManager.Instance.GetAllStatuses();
        foreach(QuestStatus status in questStatuses)
        {
            if(status.state != QuestState.locked)
            {
                var data = QuestManager.Instance.GetQuestData(status.questId);
                UpdateQuest(data, status);
            }
        }

    }


    //퀘스트 상태 변경시 발생하는 함수
    private void HandleQuestStatusChanged(QuestData data, QuestStatus status)
    {
        if (status.state != QuestState.locked)
        {
            UpdateQuest(data, status);
        }
        if (selectedQuestId.HasValue && selectedQuestId.Value == data.questID)
        {
            ShowQuestInfo(data.questID);
        }
    }

    //퀘스트 상태 업데이트
    public void UpdateQuest(QuestData data, QuestStatus status)
    {
        if (questUIItems.TryGetValue(data.questID, out var uiItem))
        {
            uiItem.UpdateUI(status);
        }
        else
        {
            GameObject newQuestUI = Instantiate(questPrefab, content);
            QuestUIItem newUiItem = newQuestUI.GetComponent<QuestUIItem>();

            newUiItem.Initialize(data,status, OnQuestItemSelected);
            questUIItems.Add(data.questID, newUiItem);
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
        QuestData data = QuestManager.Instance.GetQuestData(questId);
        QuestStatus status = QuestManager.Instance.GetQuestStatus(questId);

        if(data == null || status == null) { return; }

        questNameText.text = data.questName;

        questMainScriptText.text = data.script; // 전체 퀘스트 설명

        if(status.currentStepIndex < data.steps.Count)
        {
            var currentStep = data.steps[status.currentStepIndex];
            var stepScript = currentStep.stepDescription + "\n";

            for (int i = 0; i < currentStep.missions.Count; i++) {
                var mission = currentStep.missions[i];
                var missionKey = status.currentStepIndex * 100 + i; 
                if(mission.type == MissionType.TalkTo) { continue; }
                int currentAmount = status.MissionProgress.ContainsKey(missionKey) ? status.MissionProgress[missionKey] : 0;
                // 예: "고블린 처치 (2/5)"
                stepScript += $"- {mission.missionScript} ({currentAmount} / {mission.requiredAmount})\n";
            }
            queststepDescriptionText.text = stepScript;
        }

    }

    public void OnSelectButton()
    {
        if(selectedQuestId != null)
        {
            QuestManager.Instance.StartQuest(selectedQuestId.Value);
        }
    }

    public void RemoveQuestUI(int questId)
    {
        if (questUIItems.ContainsKey(questId))
        {
            questUIItems.Remove(questId);
            questMainScriptText.text = "";
            queststepDescriptionText.text = "";
            questNameText.text = "";
            Destroy(questUIItems[questId]);
        }
    }
}
