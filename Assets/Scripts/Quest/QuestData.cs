using System.Collections.Generic;
using UnityEngine;

public class QuestDefinition
{
    public int questID;         // 퀘스트 번호
    public string questName = "";   // 퀘스트 이름
    public string script;           // 퀘스트 설명 (ex: 마야의 진실)
    public int requiredLevel = 0;   // 레벨 제한
    public List<QuestStep> steps;   // 퀘스트 단계
}

public class QuestStep
{
    public string stepDescription;          //퀘스트 단계별 설명(ex: 마야의 집을 찾아가자.)

    public int startNpcId;                  // 이 단계를 시작하게 해주는 NPC의 ID
    public int turnInNpcId;                 // 이 단계의 완료 보고를 받을 NPC의 ID

    public List<QuestMission> missions;     //퀘스트 목표
    public QuestReward rewards;         //퀘스트 보상

    public string dialogueKey_Start;      // 이 단계를 '시작'시킬 때의 대화 키
    public string dialogueKey_InProgress; // 이 단계가 진행중일 때의 대화 키
    public string dialogueKey_Complete;   // 이 단계의 목표를 '완료'했을 때의 대화 키
}
public enum MissionType { Kill, Collect, TalkTo }
public class QuestMission
{
    public MissionType type;  //퀘스트 목표 타입
    public string missionScript;  //퀘스트 설명(ex: 마야에게 말걸기)
    public int targetId; // 몬스터 ID, 아이템 ID, NPC ID 등
    public int requiredAmount;
}

public class QuestReward
{
    public int exp;
    public int itemId;
    public int gold;
}


public enum QuestState { Locked, Ready, InProgress, Complete, TurnedIn }

[System.Serializable]
public class QuestStatus
{
    public int questId;
    public QuestState state;
    public int currentStepIndex;
    public bool IsFocused;

    public Dictionary<int, int> MissionProgress = new Dictionary<int, int>();
    public QuestStatus() { }

    public QuestStatus(QuestDefinition data)
    {
        this.questId = data.questID;
        this.state = QuestState.Locked; // 모든 퀘스트는 잠긴 상태로 시작
        currentStepIndex = 0;


        // 목표 진행도 딕셔너리 초기화
        for (int i = 0; i < data.steps.Count; i++)
        {
            for (int j = 0; j < data.steps[i].missions.Count; j++)
            {
                MissionProgress.Add(i * 100 + j, 0);
            }
        }
    }
}


