
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;



public class QuestManager : MonoBehaviour
{
    Player player;
    QuestAPI questAPI;

    //퀘스트id, 퀘스트데이터
    public Dictionary<int, QuestTemplate> QuestTemplates = new();
    //퀘스트id, 퀘스트 상태
    public Dictionary<int, QuestProgress> playerQuestProgress = new();

    //퀘스트 상태 변경
    public event Action<QuestTemplate, QuestProgress> OnQuestStatusChanged;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        OnQuestStatusChanged = null;
        player.Stats.OnLevelUp += UnlockQuests;
        OnQuestStatusChanged += SaveQuestStatus;
    }


    private void OnDestroy()
    {
        OnQuestStatusChanged -= SaveQuestStatus;
        if (player.Stats != null)
        {
            player.Stats.OnLevelUp -= UnlockQuests;
        }
        OnQuestStatusChanged = null;
    }

    public void init(QuestAPI API)
    {
        questAPI = API;
    }


    void Initialize(QuestTemplate[] questData, QuestProgress[] questProgress)
    {
        QuestTemplates.Clear();
        playerQuestProgress.Clear();

        if (questData != null)
        {

            foreach (QuestTemplate quest in questData)
            {
                if (!QuestTemplates.ContainsKey(quest.questID))
                {
                    QuestTemplates.Add(quest.questID, quest);
                }

                if (!playerQuestProgress.ContainsKey(quest.questID))
                {
                    playerQuestProgress.Add(quest.questID, new QuestProgress(quest));
                }
            }
        }

        if (questProgress != null)
        {
            foreach (QuestProgress progress in questProgress)
            {
                if (playerQuestProgress.ContainsKey(progress.questId))
                {
                    playerQuestProgress[progress.questId] = progress;
                }
            }
        }


        UnlockQuests();
    }


    //퀘스트 상태 가져오기
    public QuestProgress GetQuestStatus(int questId) => playerQuestProgress.GetValueOrDefault(questId);
    //퀘스트 원본 데이터 가져오기
    public QuestTemplate GetQuestData(int questId) => QuestTemplates.GetValueOrDefault(questId);
    //퀘스트 전체 상태 가져오기
    public List<QuestProgress> GetAllStatuses() => playerQuestProgress.Values.ToList();

    //퀘스트 상태 서버에 저장하기
    public void SaveQuestStatus(QuestTemplate data, QuestProgress status)
    {
    }


    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if (playerQuestProgress.TryGetValue(questId, out var status))
        {
            status.state = QuestState.InProgress;
            OnQuestStatusChanged?.Invoke(GetQuestData(questId), status);
        }
    }

    
    //퀘스트 완료 검증
    public void TurnInQuest(int questId)
    {
        if (playerQuestProgress.TryGetValue(questId, out var status) && status.state == QuestState.Complete)
        {
            var questDef = GetQuestData(questId);
            var currentStep = questDef.steps[status.currentStepIndex];


            // 보상 지급
            GiveReward(currentStep.rewards);

            // 다음 단계가 있는지 확인
            if (status.currentStepIndex + 1 < questDef.steps.Count)
            {
                // 다음 단계로 이동
                status.currentStepIndex++;
                status.state = QuestState.InProgress; // 다음 단계가 있으므로 다시 진행 중 상태로
            }
            else
            {
                // 모든 단계가 끝났다면 최종 완료
                status.state = QuestState.TurnedIn;
            }
            OnQuestStatusChanged?.Invoke(questDef, status);
        }
    }


    //레벨 따라 퀘스트 해금
    public void UnlockQuests()
    {
        foreach(var quest in QuestTemplates.Values)
        {
            var status = GetQuestStatus(quest.questID);
            if(player.Stats.Level >= quest.requiredLevel && status.state == QuestState.Locked)
            {
                status.state = QuestState.Ready;
                OnQuestStatusChanged?.Invoke(quest, status);
            }
        }

    }




    public void ReportEnemyKilled(int enemyId)
    {
        UpdateMissionProgress(ObjectiveType.Kill, enemyId, 1);
    }
    public void ReportTalkToNPC(int npcId)
    {
        UpdateMissionProgress(ObjectiveType.TalkTo, npcId, 1);
    }

    private void HandleCollectItem(int itemId, int amount)
    {
        UpdateMissionProgress(ObjectiveType.Collect, itemId, amount);
    }


    //퀘스트 현재 진행 상태 업데이트.
    public void UpdateMissionProgress(ObjectiveType type, int targetId, int amount)
    {

    }



    private void GiveReward(QuestReward reward)
    {
        if (reward == null) return;

        player.Stats.AddExp(reward.exp);
        player.Stats.AddGold(reward.gold);
        DataManager.Instance.Inventory.AddItem(reward.itemId);

        DataManager.Instance.SaveData();
        SoundManager.Instance.PlaySFX("missionComplete");
    }
}
