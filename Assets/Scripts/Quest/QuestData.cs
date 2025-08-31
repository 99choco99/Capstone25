using System.Collections.Generic;
using UnityEngine;
public enum QuestState { locked, ready, running, complete }
[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{

    public QuestState state;        // 퀘스트 사애
    public string questID;         // 퀘스트 번호
    public string questName = "";   // 퀘스트 이름
    public string script;           // 퀘스트 설명
    List<string> stepDescriptions;  // 퀘스트 조건 설명
    public int[] npcId = { };       // 퀘스트 npc ID
    public int questStep = 0;       // 퀘스트 진행도
    public int requiredLevel = 0;   // 레벨 제한

}
