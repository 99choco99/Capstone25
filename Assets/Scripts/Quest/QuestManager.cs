
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;



public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    //퀘스트id, 퀘스트데이터
    public Dictionary<int, QuestDefinition> QuestDefinition = new();
    //퀘스트id, 퀘스트 상태
    public Dictionary<int, QuestStatus> playerQuestState = new();

    public event Action<QuestDefinition, QuestStatus> OnQuestStatusChanged;

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

        APIEvents.OnGetQuestData += Initialize;

        APIManager.Instance.Quest.RequestGetQuestData();

        playerStats.OnLevelUp += UnlockQuests;
        EnemyStats.OnEnemyDied += HandleEnemyKilled;
        OnQuestStatusChanged += SaveQuestStatus;
    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        EnemyStats.OnEnemyDied -= HandleEnemyKilled;

        APIEvents.OnGetQuestData -= Initialize;
        OnQuestStatusChanged -= SaveQuestStatus;
        if (playerStats != null)
        {
            playerStats.OnLevelUp -= UnlockQuests;
        }
    }

    void Initialize(QuestDefinition[] questData, QuestStatus[] questProgress)
    {
        QuestDefinition.Clear();
        playerQuestState.Clear();
        foreach (QuestDefinition quest in questData)
        {
            if (!QuestDefinition.ContainsKey(quest.questID))
            {
                QuestDefinition.Add(quest.questID, quest);
            }
            if (!playerQuestState.ContainsKey(quest.questID))
            {
                playerQuestState.Add(quest.questID, new QuestStatus(quest));
            }
        }

        if (questProgress != null)
        {
            foreach (QuestStatus progress in questProgress)
            {
                if (playerQuestState.ContainsKey(progress.questId))
                {
                    playerQuestState[progress.questId] = progress;
                }
                if (progress.IsFocused)
                {
                    currentQuestId = progress.questId;
                }
                OnQuestStatusChanged?.Invoke(GetQuestData(progress.questId), GetQuestStatus(progress.questId));
            }
        }
    }


    //퀘스트 상태 가져오기
    public QuestStatus GetQuestStatus(int questId) => playerQuestState.GetValueOrDefault(questId);
    public QuestStatus GetCurerntQuestStatus() => currentQuestId.HasValue ? playerQuestState.GetValueOrDefault(currentQuestId.Value) : null;
    public QuestDefinition GetCurerntQuestDefinition() => currentQuestId.HasValue ? QuestDefinition.GetValueOrDefault(currentQuestId.Value) : null;
    //퀘스트 원본 데이터 가져오기
    public QuestDefinition GetQuestData(int questId) => QuestDefinition.GetValueOrDefault(questId);
    //퀘스트 전체 상태 가져오기
    public List<QuestStatus> GetAllStatuses() => playerQuestState.Values.ToList();

    //퀘스트 서버에 저장하기
    public void SaveQuestStatus(QuestDefinition data, QuestStatus status)
    {
        APIManager.Instance.Quest.RequestSaveQuestStatus(status);
    }

    //퀘스트 지정
    public void SetCurrentQuest(int? questId)
    {
        if (currentQuestId == questId) return;

        // 이전 퀘스트 포커스 해제
        if (currentQuestId.HasValue && playerQuestState.TryGetValue(currentQuestId.Value, out var oldStatus))
        {
            oldStatus.IsFocused = false;
            OnQuestStatusChanged?.Invoke(GetQuestData(oldStatus.questId), oldStatus);
        }

        currentQuestId = questId;

        // 새 퀘스트 포커스 설정
        if (currentQuestId.HasValue && playerQuestState.TryGetValue(currentQuestId.Value, out var newStatus))
        {
            newStatus.IsFocused = true;
            OnQuestStatusChanged?.Invoke(GetQuestData(newStatus.questId), newStatus);
        }
    }



    //퀘스트 시작
    public void StartQuest(int questId)
    {
        if (playerQuestState.TryGetValue(questId, out var status))
        {
            status.state = QuestState.InProgress;
            OnQuestStatusChanged?.Invoke(GetQuestData(questId), status);
        }
    }


    public void TurnInQuest(int questId)
    {
        if (playerQuestState.TryGetValue(questId, out var status) && status.state == QuestState.Complete)
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
                SetCurrentQuest(null);
            }
            OnQuestStatusChanged?.Invoke(questDef, status);
        }
    }


    public QuestInteractionInfo GetQuestInteractionForNpc(int npcId)
    {
        if (!currentQuestId.HasValue) { return null; }

        var status = GetCurerntQuestStatus();
        var questDef = GetCurerntQuestDefinition();

        if (status == null || questDef == null || status.currentStepIndex >= questDef.steps.Count) { return null; }

        var currentStep = questDef.steps[status.currentStepIndex];


        if (status.state == QuestState.Complete && currentStep.turnInNpcId == npcId)
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_Complete, status.questId, npcId, QuestInteractionType.Complete);
        }


        if (status.state == QuestState.Ready && currentStep.startNpcId == npcId)
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_Start, status.questId, npcId, QuestInteractionType.Start);
        }

        if (status.state == QuestState.InProgress && (currentStep.turnInNpcId == npcId))
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_InProgress, status.questId, npcId, QuestInteractionType.None);
        }

        if (status.state == QuestState.InProgress)
        {
            // 현재 단계의 미션 목록에서 이 NPC와 관련된 'TalkTo' 미션이 있는지 찾기
            for (int i = 0; i < currentStep.missions.Count; i++)
            {
                var mission = currentStep.missions[i];
                if (mission.type == MissionType.TalkTo && mission.targetId == npcId)
                {
                    //미션이 완료되었는지 확인
                    int missionKey = status.currentStepIndex * 100 + i;
                    if (status.MissionProgress.GetValueOrDefault(missionKey) < mission.requiredAmount)
                    {
                        return new QuestInteractionInfo(mission.dialogueKey_Initial, status.questId, npcId, QuestInteractionType.Talk);
                    }
                    else
                    {
                        return new QuestInteractionInfo(mission.dialogueKey_Repeated, status.questId, npcId, QuestInteractionType.None);
                    }
                }
            }
        }

        // 위 모든 경우에 해당하지 않으면 퀘스트와 관련 없는 상호작용
        return null;
    }




    //레벨 따라 퀘스트 해금
    public void UnlockQuests()
    {
        foreach(var quest in QuestDefinition.Values)
        {
            var status = GetQuestStatus(quest.questID);
            if(playerStats.Level >= quest.requiredLevel && status.state == QuestState.Locked)
            {
                status.state = QuestState.Ready;
                OnQuestStatusChanged?.Invoke(quest, status);
            }
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
        UpdateMissionProgress(MissionType.Collect, itemId, amount);
    }


    //퀘스트 조건을 만족했는지 확인
    public void UpdateMissionProgress(MissionType type, int targetId, int amount)
    {
        if(currentQuestId == null) { return; }

        var questStatus = playerQuestState[currentQuestId.Value];
        if (!questStatus.IsFocused) { return; }


        var questData = QuestDefinition[currentQuestId.Value];
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
                questStatus.state = QuestState.Complete;

            }
        }
        OnQuestStatusChanged?.Invoke(questData, questStatus);
    }



    private void GiveReward(QuestReward reward)
    {
        if (reward == null) return;

        // 예시: 경험치, 골드, 아이템 지급 로직
        // 실제로는 PlayerStats나 InventoryManager 같은 다른 매니저의 함수를 호출해야 합니다.
        playerStats.AddExp(reward.exp);
        playerStats.AddGold(reward.gold);
        //InventoryManager.Instance.AddItem(reward.itemId);

        Debug.Log($"보상 획득: 경험치 {reward.exp}, 골드 {reward.gold}");
    }
}
