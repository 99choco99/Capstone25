using UnityEngine;

public class Quest : MonoBehaviour
{

    QuestData questData;

    public void Unlock() {
        questData.state = QuestState.ready;
    }
    
    public QuestData GetQuestData()
    {
        return questData;
    }

    public QuestState GetQuestState()
    {
        return questData.state;
    }
    public void SetQuestState(QuestState state)
    {
        questData.state = state;
    }


    //퀘스트 조건 체크
    public virtual bool CheckCondition(int id)
    {
        if (questData.npcId[questData.questStep] != id) { return false; }
        Debug.Log("서브퀘스트 완료");
        questData.questStep++;
        return true;
    }

}
