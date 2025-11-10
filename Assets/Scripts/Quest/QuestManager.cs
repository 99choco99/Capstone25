
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;



public class QuestManager : MonoBehaviour
{
    Player player;


    //퀘스트id, 퀘스트데이터
    public Dictionary<int, QuestDefinition> QuestDefinition = new();
    //퀘스트id, 퀘스트 상태
    public Dictionary<int, QuestStatus> playerQuestState = new();



    public event Action<QuestDefinition, QuestStatus> OnQuestStatusChanged;


    private int? currentQuestId = null;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        OnQuestStatusChanged = null;
        player.localAPI.Quest.OnGetQuestData += Initialize;
        player.Stats.OnLevelUp += UnlockQuests;
        OnQuestStatusChanged += SaveQuestStatus;
    }

    private void Start()
    {
        player.localAPI.Quest.RequestGetQuestData();
    }

    private void OnDestroy()
    {
        player.localAPI.Quest.OnGetQuestData -= Initialize;
        OnQuestStatusChanged -= SaveQuestStatus;
        if (player.Stats != null)
        {
            player.Stats.OnLevelUp -= UnlockQuests;
        }
        OnQuestStatusChanged = null;
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
            }
        }
        UnlockQuests();
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
        player.localAPI.Quest.RequestSaveQuestStatus(status);
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


            if (questId == 102) // "사라진 동생" 퀘스트
            {
                if (status.currentStepIndex == 1) // 1번 스텝(아리사 제안 수락) 완료 시
                {
                    // Combat Scene으로 이동
                    string targetSceneName = "Combat";
                    Vector3 targetPosition = new Vector3(254.249f, 2.32969f, 390.802f);
                    transform.position = targetPosition;
                    LoadingScene.LoadScene("Combat");

                    if (SocketManager.instance != null)
                    {
                        SocketManager.instance.EmitSceneChange(targetSceneName, targetPosition);
                    }
                }
                else if (status.currentStepIndex == 2) // 2번 스텝(카야 구출) 완료 시
                {
                    // Main Scene으로 복귀
                    string targetSceneName = "Main";
                    Vector3 targetPosition = new Vector3(-15.76f, 3.866f, 49.78f);
                    transform.position = targetPosition;
                    LoadingScene.LoadScene("Main");
                    if (SocketManager.instance != null)
                    {
                        SocketManager.instance.EmitSceneChange(targetSceneName, targetPosition);
                    }
                }
            }


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


    //NPC가 퀘스트를 가지고있느지 확인
    public QuestInteractionInfo GetQuestInteractionForNpc(int npcId)
    {
        if (!currentQuestId.HasValue) { return null; }

        var status = GetCurerntQuestStatus();
        var questDef = GetCurerntQuestDefinition();

        if (status == null || questDef == null || status.currentStepIndex >= questDef.steps.Count) { return null; }


        // ========== [신규 로직: 퀘스트 102 완료 후 재입장] ==========
        // 퀘스트 102 (사라진 동생)의 상태를 별도로 확인
        if (playerQuestState.TryGetValue(102, out var quest102Status) &&
            QuestDefinition.TryGetValue(102, out var quest102Def))
        {
            // 1번 스텝을 완료시킨 NPC (아리사) ID 확인
            int arisaNpcId = quest102Def.steps[1].turnInNpcId;

            // 아리사와 "Main" 씬에서 대화하는지 확인
            if (npcId == arisaNpcId && SceneManager.GetActiveScene().name == "Main")
            {
                // [기존 재입장 로직] 퀘스트 진행 중(스텝 2) 사망/복귀 시
                if (quest102Status.currentStepIndex == 2 && quest102Status.state == QuestState.InProgress)
                {
                    // (참고: DialogueManager에 "COMBAT_REENTRY_ARISA" 같은 새 대화 키가 필요할 수 있습니다)
                    return new QuestInteractionInfo("COMBAT_REENTRY_ARISA", 102, npcId, QuestInteractionType.Talk);
                }

                // [사용자 요청] 퀘스트를 완료한(TurnedIn) 후
                if (quest102Status.state == QuestState.TurnedIn)
                {
                    // 퀘스트 완료 후 재입장용 대화 키 (새로 만드는 것을 추천)
                    // 예: "COMBAT_POST_QUEST_ENTRY" -> "그곳에 다시 가보시겠어요?"
                    return new QuestInteractionInfo("COMBAT_POST_QUEST_ENTRY", 102, npcId, QuestInteractionType.Talk);
                }
            }
        }
        // ==================================


        var currentStep = questDef.steps[status.currentStepIndex];


        if (status.state == QuestState.Complete && currentStep.turnInNpcId == npcId)
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_Complete, status.questId, npcId, QuestInteractionType.Complete);
        }


        if (status.state == QuestState.Ready && currentStep.startNpcId == npcId)
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_Start, status.questId, npcId, QuestInteractionType.Start);
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

        if (currentStep.startNpcId == npcId)
        {
            return new QuestInteractionInfo(currentStep.dialogueKey_InProgress, status.questId, npcId, QuestInteractionType.None);
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
            if(player.Stats.Level >= quest.requiredLevel && status.state == QuestState.Locked)
            {
                status.state = QuestState.Ready;
                OnQuestStatusChanged?.Invoke(quest, status);
            }
        }

    }




    public void ReportEnemyKilled(int enemyId)
    {
        UpdateMissionProgress(MissionType.Kill, enemyId, 1);
    }
    public void ReportTalkToNPC(int npcId)
    {
        // ========== [수정된 퀘스트 102 재입장 로직] ==========
        // 퀘스트 102 (사라진 동생)의 상태를 별도로 확인
        if (playerQuestState.TryGetValue(102, out var quest102Status) &&
            QuestDefinition.TryGetValue(102, out var quest102Def))
        {
            // 1번 스텝을 완료시킨 NPC (아리사) ID 확인
            int arisaNpcId = quest102Def.steps[1].turnInNpcId;

            // 아리사와 "Main" 씬에서 대화하는지 확인
            if (npcId == arisaNpcId && SceneManager.GetActiveScene().name == "Main")
            {
                // [기존 재입장] 퀘스트 진행 중(스텝 2)이거나,
                // [신규 재입장] 퀘스트를 완료(TurnedIn)했거나.
                if ((quest102Status.currentStepIndex == 2 && quest102Status.state == QuestState.InProgress) ||
                     (quest102Status.state == QuestState.TurnedIn))
                {
                    // "Combat" 씬으로 즉시 이동시킵니다.
                    string targetSceneName = "Combat";
                    Vector3 targetPosition = new Vector3(254.249f, 2.32969f, 390.802f);

                    // QuestManager가 Player에 붙어있으므로 player.transform을 사용합니다.
                    player.transform.position = targetPosition;
                    LoadingScene.LoadScene(targetSceneName);

                    if (SocketManager.instance != null)
                    {
                        SocketManager.instance.EmitSceneChange(targetSceneName, targetPosition);
                    }
                    return; // 일반 미션 진행 로직(UpdateMissionProgress)을 건너뜁니다.
                }
            }
        }
        // ==================================

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

        player.Stats.AddExp(reward.exp);
        player.Stats.AddGold(reward.gold);
        DataManager.Instance.Inventory.AddItem(reward.itemId);

        DataManager.Instance.SaveData();
        SoundManager.Instance.PlaySFX("missionComplete");
        Debug.Log($"보상 획득: 경험치 {reward.exp}, 골드 {reward.gold}");
    }
}
