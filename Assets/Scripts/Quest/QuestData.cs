using System.Collections.Generic;
using UnityEngine;


public enum QuestState { Locked, Ready, InProgress,CanComplete, TurnedIn }
public enum ObjectiveType { Kill, Collect, TalkTo, Interact }


[System.Serializable]
public class QuestData
{
    public QuestTemplate[] questTemplate;
    public QuestProgress[] questProgress;
}

public class QuestTemplate
{
    public int id;                         // 퀘스트 번호
    public string questName = "";               // 퀘스트 이름
    public string description;                       // 퀘스트 설명 (ex: 마야의 진실)
    public int requiredLevel = 0;               // 레벨 제한


    //위상 정렬의 아이디어를 가져와서 씀
    [Header("퀘스트 연결")]
    [Tooltip("선행퀘스트 목록")]
    public List<int> prerequisiteQuestIds;       //선행퀘스트

    [Header("NPC 연동")]
    public int startNPCId;
    public int turnInNpcId;

    [Header("목표 및 보상")]
    [Tooltip("목표")]
    public List<QuestObjective> objectives;
    [Tooltip("보상")]
    public int rewardId;


    [Header("대화(Dialogue) 연동 키")]
    public string dialogueKey_Start;       // 퀘스트 수락 시 출력할 대화 Key
    public string dialogueKey_InProgress;  // 진행 중일 때 출력할 대화 Key
    public string dialogueKey_Complete;    // 완료/보고 시 출력할 대화 Key
}



//퀘스트 목표를 정의
public class QuestObjective
{
    public ObjectiveType type;
    public string objectiveDescription;  // 목표 설명(ex: 마야에게 말걸기)
    public int targetId;                 // 몬스터 ID, 아이템 ID, NPC ID 등
    public int requiredAmount;           // 필요 수량
}


//퀘스트 보상
public class QuestReward
{
    public int Id;
    public int exp;
    public int itemId;
    public int gold;
}

[System.Serializable]
public class QuestProgress
{
    public int questId;
    public QuestState state;                  //퀘스트의 현재 상태
    public int[] objectiveProgresses;  //퀘스트 목표 진행상태

    public QuestProgress(QuestTemplate data)
    {
        this.questId = data.id;
        this.state = QuestState.Locked; // 처음 생성될 때는 무조건 잠김 상태로 시작
        
        if(data.objectives != null)
        {
            objectiveProgresses = new int[data.objectives.Count];
        }
    }

}


