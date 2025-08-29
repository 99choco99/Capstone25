using UnityEngine;

public class Quest
{
    public int questNum = 0; // 퀘스트 번호
    public string questName = "";  // 퀘스트 이름
    public int[] npcId = {};  // 퀘스트 npc ID
    public int questStep = 0; // 퀘스트 진행도
    public int requiredLevel = 0;  //레벨 제한
    public string script;       //퀘스트 설명

    QuestState state;

    public enum QuestState{ locked, ready, running, complete }

    public Quest(int questNum, string questName, int[] npcId, int requiredLevel, string script)
    {
        state = QuestState.ready;
        this.questNum = questNum;
        this.questName = questName;
        this.npcId = npcId;
        this.requiredLevel = requiredLevel;
        this.script = script;
    }


    public void QuestStart()
    {
        state = QuestState.running;
    }
    public void QuestComplete()
    {
        state = QuestState.complete;
    }
    public QuestState GetQuestState()
    {
        return state;
    }

    public void SetQuestState(QuestState state)
    {
        this.state = state;
    }

    //퀘스트 조건
    public virtual bool CheckCondition(int id)
    {
        if (npcId[questStep] != id) { return false; }
        Debug.Log("서브퀘스트 완료");
        questStep++;
        return true;
    }

    public void ChangeScript()
    {

    }
}
