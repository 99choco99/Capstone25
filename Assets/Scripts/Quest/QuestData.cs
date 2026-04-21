using System.Collections.Generic;
using UnityEngine;


public enum QuestState { Locked, Ready, InProgress, Complete, TurnedIn }

[System.Serializable]
public class QuestData
{
    public QuestTemplate[] questTemplate;
    public QuestProgress[] questProgress;
}

public class QuestTemplate
{
    public int questID;         // 퀘스트 번호
    public string questName = "";   // 퀘스트 이름
    public string script;           // 퀘스트 설명 (ex: 마야의 진실)
    public int requiredLevel = 0;   // 레벨 제한
    public List<int> prerequisiteQuestId; //선행퀘스트
    public List<QuestStep> steps;   // 퀘스트 단계
}

//퀘스트 단계를 정의
public class QuestStep
{
    public string stepDescription;          // 퀘스트 단계별 설명(ex: 마야의 집을 찾아가자.)
    public int startNpcId;                  // 이 단계를 시작하게 해주는 NPC의 ID
    public int turnInNpcId;                 // 이 단계의 완료 보고를 받을 NPC의 ID
    public List<QuestObjective> objectives; // 퀘스트 목표
    public QuestReward rewards;             // 퀘스트 보상
}


public enum ObjectiveType {Kill, Collect,TalkTo,Interact }
//퀘스트 목표를 정의
public class QuestObjective
{
    public ObjectiveType type;
    public string missionScript;  //퀘스트 설명(ex: 마야에게 말걸기)
    public int targetId;          // 몬스터 ID, 아이템 ID, NPC ID 등
    public int requiredAmount;
}


//퀘스트 보상
public class QuestReward
{
    public int exp;
    public int itemId;
    public int gold;
}

[System.Serializable]
public class QuestProgress
{
    public int questId;
    public QuestState state;     //퀘스트의 현재 상태
    public int currentStepIndex; //현재 진행중인 단계

    public int[] currentObjectiveProgresses;  //퀘스트 목표 진행상태

    public QuestProgress() { }

    public QuestProgress(QuestTemplate data)
    {
        this.questId = data.questID;
        this.state = QuestState.Locked; // 처음 생성될 때는 무조건 잠김 상태로 시작
        SetStep(0, data);
    }

    public void SetStep(int stepIndex, QuestTemplate data)
    {
        currentStepIndex = stepIndex;
        int objectiveCount = data.steps[stepIndex].objectives.Count;
        currentObjectiveProgresses = new int[objectiveCount];
    }
}


