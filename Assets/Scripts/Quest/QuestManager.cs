using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR.Haptics;


public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    //퀘스트id, 퀘스트데이터
    public Dictionary<int, QuestData> QuestData = new();
    //퀘스트id, 퀘스트 상태
    public Dictionary<int, QuestStatus> playerQuestState = new();

    public event Action<QuestData, QuestStatus> OnQuestStatusChanged;
    public event Action<int?> OnQuestSelected;


    PlayerStats playerStats;
    private int? currentQuestId = null;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        playerStats = GetComponentInParent<PlayerStats>();  

        APIEvents.OnGetQuestData += PopulateQuestData;


        APIManager.Instance.Quest.RequestGetQuestData();
    }

    private void Start()
    {
        playerStats.OnLevelUp += UnlockQuests;
        EnemyStats.OnEnemyDied += HandleEnemyKilled;
        OnQuestStatusChanged += SaveQuestStatus;
    }

    private void OnDestroy()
    {
        EnemyStats.OnEnemyDied -= HandleEnemyKilled;
        playerStats.OnLevelUp -= UnlockQuests;
        APIEvents.OnGetQuestData -= PopulateQuestData;
        OnQuestStatusChanged -= SaveQuestStatus;
    }

    void PopulateQuestData(QuestData[] questData, QuestStatus[] questProgress)
    {
        QuestData.Clear();
        playerQuestState.Clear();
        if (questProgress != null)
        {
            foreach (QuestStatus progress in questProgress)
            {
                // 아직 등록되지 않은 경우에만 추가 (혹시 모를 중복 데이터 방지)
                if (!playerQuestState.ContainsKey(progress.questId))
                {
                    playerQuestState.Add(progress.questId, progress);
                }

                // 진행 중인 퀘스트 ID 설정
                if (progress.state == QuestState.running)
                {
                    currentQuestId = progress.questId;
                    OnQuestSelected?.Invoke(currentQuestId);
                }
            }
        }
        foreach (QuestData quest in questData)
        {
            if (!QuestData.ContainsKey(quest.questID))
            {
                QuestData.Add(quest.questID, quest);
            }
            if (!playerQuestState.ContainsKey(quest.questID))
            {
                playerQuestState.Add(quest.questID, new QuestStatus(quest));
            }
        }


        UnlockQuests();
    }

    //퀘스트 서버에 저장하기
    public void SaveQuestStatus(QuestData data, QuestStatus status)
    {
        APIManager.Instance.Quest.RequestSaveQuestStatus(status);
    }



    //퀘스트 상태 가져오기
    public QuestStatus GetQuestStatus(int questId) => playerQuestState.GetValueOrDefault(questId);

    //퀘스트 대화 정보 가져오기
    public string GetDialogueKey()
    {
        if(currentQuestId == null) { return null; }
        var status = GetQuestStatus(currentQuestId.Value);
        var data = GetQuestData(currentQuestId.Value);

        return data.steps[status.currentStepIndex].dialogueKey;
    }
    //퀘스트 원본 데이터 가져오기
    public QuestData GetQuestData(int questId) => QuestData.GetValueOrDefault(questId);
    //퀘스트 전체 상태 가져오기
    public List<QuestStatus> GetAllStatuses() => playerQuestState.Values.ToList();

    //퀘스트 지정
    public void SetCurrentQuest(int? questId)
    {
        if(currentQuestId == questId) { return; }
        currentQuestId = questId;
        OnQuestSelected?.Invoke(questId);
    }


    //레벨 따라 퀘스트 해금
    public void UnlockQuests()
    {
        foreach(var quest in QuestData.Values)
        {
            var status = GetQuestStatus(quest.questID);
            if(playerStats.Level >= quest.requiredLevel && status.state == QuestState.locked)
            {
                status.state = QuestState.ready;
                OnQuestStatusChanged?.Invoke(quest, status);
            }
        }

    }


    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if(currentQuestId.HasValue && currentQuestId.Value != questId)
        {
            if (playerQuestState.TryGetValue(currentQuestId.Value, out var preQuest))
            {
                preQuest.state = QuestState.running;
                OnQuestStatusChanged?.Invoke(GetQuestData(currentQuestId.Value), preQuest);
            }
        }

        if(playerQuestState.TryGetValue(questId, out var newQuest))
        {
            newQuest.state = QuestState.focused;
            SetCurrentQuest(questId);
            OnQuestStatusChanged?.Invoke(GetQuestData(questId), newQuest);
        }
    }

    private void HandleEnemyKilled(int enemyId)
    {
        UpdateMissionProgress(MissionType.Kill, enemyId, 1);
    }
    public void ReportTalkToNPC(int npcId)
    {
        UpdateMissionProgress(MissionType.TalkTo, npcId, 1);
    }

    private void HandleCollectItem(int itemId, int amount)
    {
        //UpdateMissionProgress();
    }


    //퀘스트 조건을 만족했는지 확인
    public void UpdateMissionProgress(MissionType type, int targetId, int amount)
    {
        if(currentQuestId == null) { return; }

        var questStatus = playerQuestState[currentQuestId.Value];
        if (questStatus.state != QuestState.focused) { return; }


        var questData = QuestData[currentQuestId.Value];
        var currentStep = questData.steps[questStatus.currentStepIndex];
        bool IsProgress = false;

        for (int i = 0; i < currentStep.missions.Count; i++)
        {
            var mission = currentStep.missions[i];
            if(mission.type == type && mission.targetId == targetId)
            {
                var missionKey = questStatus.currentStepIndex * 100 + i;
                if (questStatus.MissionProgress[missionKey] < mission.requiredAmount)
                {
                    questStatus.MissionProgress[missionKey] += amount;
                    IsProgress = true;
                }
            }
        }
        if (IsProgress)
        {
            bool isComplete = true;
            for (int i = 0; i < currentStep.missions.Count; i++)
            {
                int MissionKey = questStatus.currentStepIndex * 100 + i;
                if (questStatus.MissionProgress[MissionKey] < currentStep.missions[i].requiredAmount)
                {
                    isComplete = false;
                    break;
                }
            }

            // 모든 목표가 완료되었다면
            if (isComplete)
            {
                // 마지막 단계였는지 확인
                if (questStatus.currentStepIndex >= questData.steps.Count - 1)
                {
                    questStatus.state = QuestState.complete; // 퀘스트 전체 완료
                    GetReward(currentQuestId.Value);
                }
                else
                {
                    questStatus.currentStepIndex++; // 다음 단계로
                }
                OnQuestStatusChanged?.Invoke(questData, questStatus);
            }
        }

    }

    public void GetReward(int questId)
    {
        if (playerQuestState.TryGetValue(questId, out var status) && status.state == QuestState.complete) 
        {
            currentQuestId = null;
        }
        //골드 추가 QuestDataList[questId].rewards[QuestDataList[questId].questStep].exp
        //아이템 추가
    }


}
