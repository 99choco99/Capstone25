
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;



public class QuestManager
{
    private int playerLevel = 1;


    //퀘스트id, 퀘스트 데이터
    public Dictionary<int, QuestTemplate> QuestTemplates { get; private set; }
    //퀘스트id, 퀘스트 상태
    public Dictionary<int, QuestProgress> PlayerQuestProgress { get; private set; }
    //퀘스트id, 퀘스트 보상
    public Dictionary<int, QuestReward> RewardTemplates { get; private set; }


    //퀘스트 상태 변경
    public event Action<QuestTemplate, QuestProgress> OnQuestStatusChanged;
    public event Action<int> OnItemRewardEarned;
    public event Action<int, int> OnStatRewardEarned; // exp, gold

    public QuestManager(int initialLevel)
    {
        this.playerLevel = initialLevel;

        QuestTemplates = new Dictionary<int, QuestTemplate>();
        PlayerQuestProgress = new Dictionary<int, QuestProgress>();
        RewardTemplates = new Dictionary<int, QuestReward>();
    }

    public void SyncPlayerLevel(int level)
    {
        playerLevel = level;
        CheckAndUnlockQuests();
    }

    public void LoadQuestData(QuestTemplate[] questData, QuestProgress[] questProgress, QuestReward[] rewardData)
    {
        QuestTemplates.Clear();
        PlayerQuestProgress.Clear();
        RewardTemplates.Clear();

        if (rewardData != null)
        {
            foreach (QuestReward reward in rewardData)
            {
                RewardTemplates[reward.Id] = reward;
            }
        }

        if (questData != null)
        {

            foreach (QuestTemplate template in questData)
            {
                QuestTemplates[template.id] = template;
                PlayerQuestProgress[template.id] = new QuestProgress(template);
            }
        }

        if (questProgress != null)
        {
            foreach (QuestProgress progress in questProgress)
            {
                if (PlayerQuestProgress.ContainsKey(progress.questId))
                {
                    PlayerQuestProgress[progress.questId] = progress;
                }
            }
        }

        CheckAndUnlockQuests();
    }


    //퀘스트 상태 가져오기
    public QuestProgress GetQuestStatus(int questId) => PlayerQuestProgress.GetValueOrDefault(questId);
    //퀘스트 원본 데이터 가져오기
    public QuestTemplate GetQuestData(int questId) => QuestTemplates.GetValueOrDefault(questId);
    //퀘스트 전체 상태 가져오기
    public List<QuestProgress> GetAllStatuses() => PlayerQuestProgress.Values.ToList();

    //퀘스트 상태 서버에 저장하기
    public void SaveQuestStatus(QuestTemplate data, QuestProgress status)
    {
    }


    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if (PlayerQuestProgress.TryGetValue(questId, out var status))
        {
            if (status.state != QuestState.Ready) { return; }
            status.state = QuestState.InProgress;
            CheckQuestObjectives(questId);
            OnQuestStatusChanged?.Invoke(GetQuestData(questId), status);
        }
    }

    //퀘스트 포기
    public void AbandonQuest(int questId)
    {
        if(!PlayerQuestProgress.TryGetValue(questId,out var status)) { return; }
        if (status.state == QuestState.InProgress || status.state == QuestState.CanComplete){

            QuestTemplate template = GetQuestData(questId);
            if (template == null) return;

            status.state = QuestState.Ready;

            if (status.objectiveProgresses != null)
            {
                for (int i = 0; i < status.objectiveProgresses.Length; i++)
                {
                    status.objectiveProgresses[i] = 0;
                }
            }
            OnQuestStatusChanged?.Invoke(template, status);
        }
    }
    
    //퀘스트 완료 검증
    public void TurnInQuest(int questId)
    {
        if (PlayerQuestProgress.TryGetValue(questId, out var status) && status.state == QuestState.CanComplete)
        {
            QuestTemplate template = GetQuestData(questId);
            status.state = QuestState.TurnedIn;

            // 보상 지급
            GiveReward(template.rewardId);
            OnQuestStatusChanged?.Invoke(template, status);
            CheckAndUnlockQuests();
        }
    }



    public void ReportEnemyKilled(int enemyId)
    {
        UpdateObjectiveProgress(ObjectiveType.Kill, enemyId, 1);
    }
    public void ReportTalkToNPC(int npcId)
    {
        UpdateObjectiveProgress(ObjectiveType.TalkTo, npcId, 1);
    }
    private void HandleCollectItem(int itemId, int amount)
    {
        UpdateObjectiveProgress(ObjectiveType.Collect, itemId, amount);
    }


    //퀘스트 현재 진행 상태 업데이트.
    public void UpdateObjectiveProgress(ObjectiveType type, int targetId, int amount)
    {
        foreach(var progress in PlayerQuestProgress.Values.Where(p => p.state == QuestState.InProgress))
        {
            QuestTemplate template = GetQuestData(progress.questId);
            if(template == null) { return; }

            bool isDirty = false;

            for (int i = 0; i < template.objectives.Count; i++)
            {
                var obj = template.objectives[i];
                if (obj.type == type && obj.targetId == targetId)
                {
                    progress.objectiveProgresses[i] = Mathf.Min(obj.requiredAmount, progress.objectiveProgresses[i] + amount);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                CheckQuestObjectives(progress.questId);
            }
        }
    }

    // 목표 수량 달성 검증기
    private void CheckQuestObjectives(int questId)
    {
        if (!PlayerQuestProgress.TryGetValue(questId, out var status) || status.state != QuestState.InProgress) return;
        QuestTemplate template = GetQuestData(questId);
        if (template == null) return;

        bool allObjectivesMet = true;

        for (int i = 0; i < template.objectives.Count; i++)
        {
            if (status.objectiveProgresses[i] < template.objectives[i].requiredAmount)
            {
                allObjectivesMet = false;
                break;
            }
        }

        if (allObjectivesMet)
        {
            status.state = QuestState.CanComplete;
            OnQuestStatusChanged?.Invoke(template, status);
        }
    }


    //퀘스트 해금
    public void CheckAndUnlockQuests()
    {
        foreach (var quest in QuestTemplates.Values)
        {
            var status = GetQuestStatus(quest.id);
            if (status.state != QuestState.Locked) { continue; }

            if (playerLevel < quest.requiredLevel) continue;

            bool isPrerequisiteMet = true;

            if (quest.prerequisiteQuestIds != null)
            {
                foreach (int id in quest.prerequisiteQuestIds)
                {
                    if (GetQuestStatus(id).state != QuestState.TurnedIn)
                    {
                        isPrerequisiteMet = false;
                        break;
                    }
                }
            }


            if (isPrerequisiteMet)
            {
                status.state = QuestState.Ready;
                OnQuestStatusChanged?.Invoke(quest, status);
            }
        }

    }


    private void GiveReward(int rewardId)
    {
        if(!RewardTemplates.TryGetValue(rewardId, out var reward)) { return; }


        OnStatRewardEarned?.Invoke(reward.exp, reward.gold);
        if (reward.itemId > 0)
        {
            OnItemRewardEarned?.Invoke(reward.itemId);
        }
        SoundManager.Instance.PlaySFX("missionComplete");
    }
}
