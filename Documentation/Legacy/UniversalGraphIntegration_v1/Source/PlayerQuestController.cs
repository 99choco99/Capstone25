using UniversalGraph;
// UniversalGraph v1 게임 연결 코드 보관본
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerQuestController : UniversalGraph.IQuestController
{
    private int playerLevel = 0;
    public Dictionary<int, QuestProgress> QuestProgress { get; private set; } = new();

    //퀘스트 상태 변경
    public event Action<UniversalGraph.QuestContainer, QuestProgress> OnQuestStatusChanged;
    public Func<int, bool> CheckInventorySpace;
    public Action OnRewardFailed_InventoryFull;
    public event Action<int, int> OnItemRewardEarned;
    public event Action<int, int> OnStatRewardEarned; // exp, gold


    public void LoadQuestData(List<QuestProgress> questProgress)
    {
        QuestProgress.Clear();

        foreach (var template in QuestManager.Instance.QuestTemplates.Values)
        {
            QuestProgress[template.id] = new QuestProgress(template);
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
    public void SaveQuestStatus(UniversalGraph.QuestContainer data, QuestProgress status)
    {
    }


    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if (QuestProgress.TryGetValue(questId, out var status))
        {
            if (status.state != QuestState.Ready) { return; }
            QuestRunner.StartQuestGraph(this, questId);
            OnQuestStatusChanged?.Invoke(QuestManager.Instance.GetQuestTemplate(questId), status);
        }
    }

    //퀘스트 포기
    public void AbandonQuest(int questId)
    {
        if (!QuestProgress.TryGetValue(questId, out var status)) { return; }
        if (status.state == QuestState.InProgress || status.state == QuestState.CanComplete)
        {

            UniversalGraph.QuestContainer template = QuestManager.Instance.GetQuestTemplate(questId);
            if (template == null) return;

            status.state = QuestState.Ready;
            status.currentNodeGuid = string.Empty;
            status.activeNodeGuids.Clear();
            status.nodeProgressCounts.Clear();
            OnQuestStatusChanged?.Invoke(template, status);
        }
    }

    //퀘스트 완료 검증
    public void TurnInQuest(int questId)
    {
        if (QuestProgress.TryGetValue(questId, out var status) && status.state == QuestState.CanComplete)
        {
            UniversalGraph.QuestContainer template = QuestManager.Instance.GetQuestTemplate(questId);
            if (template == null)
            {
                return;
            }

            if (template.reward != null
                && template.reward.itemId > 0
                && CheckInventorySpace != null
                && !CheckInventorySpace.Invoke(template.reward.itemId))
            {
                OnRewardFailed_InventoryFull?.Invoke();
                return;
            }

            // 보상 지급
            status.state = QuestState.TurnedIn;
            QuestRunner.NotifyQuestCompleted(this, questId);
            GiveReward(template.id);
            OnQuestStatusChanged?.Invoke(template, status);
            CheckAndUnlockQuests(playerLevel);
        }
    }

    public void ReportEnemyKilled(int enemyId)
    {
        UpdateObjectiveProgress("Kill", enemyId, 1);
    }
    public void ReportTalkToNPC(int npcId)
    {
        UpdateObjectiveProgress("TalkTo", npcId, 1);
    }
    private void HandleCollectItem(int itemId, int amount)
    {
        UpdateObjectiveProgress("Collect", itemId, amount);
    }


    //퀘스트 현재 진행 상태 업데이트.
    public void InvokeStatusChanged(UniversalGraph.QuestContainer c, QuestProgress p) { OnQuestStatusChanged?.Invoke(c, p); }
    public void UpdateObjectiveProgress(string type, int targetId, int amount)
    {
        QuestRunner.ProcessEvent(this, type, targetId, amount);
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
        UniversalGraph.QuestContainer template = QuestManager.Instance.GetQuestTemplate(questId);
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









