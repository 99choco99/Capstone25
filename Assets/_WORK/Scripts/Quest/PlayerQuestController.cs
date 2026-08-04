using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerQuestController
{
    private int playerLevel = 0;
    public Dictionary<int, QuestProgress> QuestProgress { get; private set; } = new();

    //퀘스트 상태 변경
    public event Action<QuestTemplate, QuestProgress> OnQuestStatusChanged;
    public Func<int, bool> CheckInventorySpace;
    public Action OnRewardFailed_InventoryFull;
    public event Action<int, int> OnItemRewardEarned;
    public event Action<int, int> OnStatRewardEarned; // exp, gold


    public void LoadQuestData(List<QuestProgress> questProgress)
    {
        QuestProgress.Clear();

        foreach (var template in QuestManager.Instance.QuestTemplates.Values)
        {
            QuestProgress[template.id] = new();
        }

        if (questProgress != null)
        {
            foreach (QuestProgress progress in questProgress)
            {
                if (QuestProgress.ContainsKey(progress.questId))
                {
                    QuestProgress[progress.questId] = progress;
                }
            }
        }
    }




    //퀘스트 상태 서버에 저장하기
    public void SaveQuestStatus(QuestTemplate data, QuestProgress status)
    {
    }


    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if (QuestProgress.TryGetValue(questId, out var status))
        {
            if (status.state != QuestState.Ready) { return; }
            status.state = QuestState.InProgress;
            CheckQuestObjectives(questId);
            OnQuestStatusChanged?.Invoke(QuestManager.Instance.GetQuestTemplate(questId), status);
        }
    }

    //퀘스트 포기
    public void AbandonQuest(int questId)
    {
        if (!QuestProgress.TryGetValue(questId, out var status)) { return; }
        if (status.state == QuestState.InProgress || status.state == QuestState.CanComplete)
        {

            QuestTemplate template = QuestManager.Instance.GetQuestTemplate(questId);
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
        if (QuestProgress.TryGetValue(questId, out var status) && status.state == QuestState.CanComplete)
        {
            QuestTemplate template = QuestManager.Instance.GetQuestTemplate(questId);
            if (CheckInventorySpace != null && !CheckInventorySpace.Invoke(template.reward.itemId))
            {
                OnRewardFailed_InventoryFull?.Invoke();
                return;
            }

            // 보상 지급
            status.state = QuestState.TurnedIn;
            GiveReward(template.id);
            OnQuestStatusChanged?.Invoke(template, status);
            CheckAndUnlockQuests(playerLevel);
        }
    }

    //NPC가 가진 퀘스트 가져오기
    public List<QuestDialogueContext> GetCurrentQuestContextForNPC(int npcID)
    {
        List<QuestDialogueContext> validContexts = new List<QuestDialogueContext>();

        foreach (var status in QuestProgress.Values)
        {
            if (status.state == QuestState.Locked || status.state == QuestState.TurnedIn) { continue; }

            QuestDialogueContext currentContext = new();
            QuestTemplate template = QuestManager.Instance.GetQuestTemplate(status.questId);

            if (template.startNPCId != npcID && template.turnInNPCId != npcID) { continue; }

            if (status.state == QuestState.Ready)
            {
                if (template.startNPCId == npcID)
                {
                    currentContext.questTitle = template.questName;
                    currentContext.prefix = "[시작 가능] ";
                    currentContext.dialogueKey = $"QUEST_{template.id}_NPC_{npcID}_START";
                    currentContext.onSubmitAction = () => StartQuest(template.id);
                }
            }
            else if (status.state == QuestState.CanComplete)
            {
                if (template.turnInNPCId == npcID)
                {
                    currentContext.questTitle = template.questName;
                    currentContext.prefix = "[완료] ";
                    currentContext.dialogueKey = $"QUEST_{template.id}_NPC_{npcID}_COMPLETE";
                    currentContext.onSubmitAction = () => TurnInQuest(template.id);
                }
                else if (template.startNPCId == npcID)
                {
                    currentContext.questTitle = template.questName;
                    currentContext.prefix = "[진행 중] ";
                    currentContext.dialogueKey = currentContext.dialogueKey = $"QUEST_{template.id}_NPC_{npcID}_INPROGRESS";
                    currentContext.onSubmitAction = null;
                }
            }
            else if (status.state == QuestState.InProgress)
            {
                if (template.startNPCId == npcID)
                {
                    currentContext.questTitle = template.questName;
                    currentContext.prefix = "[진행 중] ";
                    currentContext.dialogueKey = $"QUEST_{template.id}_NPC_{npcID}_INPROGRESS";
                    currentContext.onSubmitAction = null;
                }
            }
            validContexts.Add(currentContext);
        }
        return validContexts;
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
        foreach (var progress in QuestProgress.Values.Where(p => p.state == QuestState.InProgress))
        {
            QuestTemplate template = QuestManager.Instance.GetQuestTemplate(progress.questId);
            if (template == null) { return; }

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
        if (!QuestProgress.TryGetValue(questId, out var status) || status.state != QuestState.InProgress) return;
        QuestTemplate template = QuestManager.Instance.GetQuestTemplate(questId);
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
    public void CheckAndUnlockQuests(int playerLevel)
    {
        foreach (var quest in QuestManager.Instance.QuestTemplates.Values)
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

    //보상 지급
    private void GiveReward(int questId)
    {
        QuestTemplate template = QuestManager.Instance.GetQuestTemplate(questId);
        if (template == null || template.reward == null) return;

        OnStatRewardEarned?.Invoke(template.reward.exp, template.reward.gold);
        if (template.reward.itemId > 0)
        {
            OnItemRewardEarned?.Invoke(template.reward.itemId, template.reward.amount);
        }
        SoundManager.Instance.PlaySFX("missionComplete");
    }
    public void SyncPlayerLevel(int level)
    {
        playerLevel = level;
        CheckAndUnlockQuests(playerLevel);
    }


    //================편의성 함수===================

    //퀘스트 전체 상태 가져오기
    public List<QuestProgress> GetAllStatuses() => QuestProgress.Values.ToList();
    //퀘스트 상태 가져오기
    public QuestProgress GetQuestStatus(int questId) => QuestProgress.GetValueOrDefault(questId);

}
