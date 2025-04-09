using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    public Dictionary<int, Queue<Quest>> availableQuests = new Dictionary<int, Queue<Quest>>(); // 해금된 퀘스트 목록
    Queue<Quest> Questsqueue = new Queue<Quest>(); // 퀘스트 큐
    Dictionary<int, Queue<Quest>> questList;
    public int currentQuestStep = 0; // 현재 퀘스트 진행도
    int currentQuestId;
    public Quest currentQuest;
    bool isQuestActive = false;
    [SerializeField] NPC[] npcList;


    [Header("QuestUISetting")]
    [SerializeField] QuestUI questUI;
    private bool isQuestListActive = false;

    private void Awake()
    {
        instance = this;
        questList = new Dictionary<int, Queue<Quest>>();
        GenerateData();
    }

    void GenerateData()
    {
        
        // 퀘스트 리스트 받아오기
        // 퀘스트 번호, 퀘스트 이름, 관련 NPC, 퀘스트 설명

        // NPC 100에 대한 퀘스트
        var questQueueFor100 = new Queue<Quest>();
        questQueueFor100.Enqueue(new Quest(10, "First Quest", new int[] { 100,200 },1, "First Mission : Go to NPC 100"));
        questQueueFor100.Enqueue(new Quest(20, "Second Quest", new int[] { 200 },2, "Second mission : Go to NPC 200!!!!"));
        questList.Add(100, questQueueFor100);

        // NPC 200에 대한 퀘스트
        var questQueueFor200 = new Queue<Quest>();
        questQueueFor200.Enqueue(new Quest(30, "First Quest200", new int[] { 200,100 }, 1, "This is 200 Quest: Go to NPC 200"));
        questQueueFor200.Enqueue(new Quest(40, "Second Quest200", new int[] { 100 }, 2, "This is Second 200 Quest Conversation4"));
        questList.Add(200, questQueueFor200);
    }


    public int GetQuest(int id)
    {
        if (currentQuest == null || !isQuestActive) { return 0; }
        if (CheckQuest(id))
        {
            return currentQuest.questNum + currentQuest.questStep - 1;
        }
        return 0;
    }



    public void ChangeQuest()
    {
        isQuestActive = true;
        currentQuest = questUI.selectedQuest;
        Debug.Log("현재 미션: " + currentQuest.questName);
    }

    public bool CheckQuest(int id)
    {
        if (currentQuest.CheckQeust(id)) //현재 퀘스트의 조건을 충족했는가?
        {
            if(currentQuest.questStep >= currentQuest.npcId.Length)
            {
                Debug.Log("퀘스트 끝");
                questUI.EndQuest(currentQuest);
                availableQuests[id].Dequeue();
                isQuestActive = false;
                currentQuest.QuestComplete();
                return true;
            }
            return true;
        }
        return false;
    }

    //레벨 따라 퀘스트 해금
    public void UnlockQuests(int playerLevel)
    {
        Questsqueue.Clear(); // 이전 레벨 퀘스트 목록 초기화
        foreach (var npc in npcList)
        {
            foreach (Quest quest in questList[npc.id])
            {
                if (quest.requiredLevel == playerLevel)
                {
                    Questsqueue.Enqueue(quest);
                    questUI.SetNewQuest(quest);  // questUI에 퀘스트 표시
                }
            }
            if (Questsqueue.Count > 0) // 퀘스트가 있을 경우에만 딕셔너리에 추가
            {
                availableQuests[npc.id] = Questsqueue;
            }
        }
    }


    //퀘스트 처음 시작
    public void StartQuest()
    {
        currentQuest.SetQuestState(Quest.QuestState.running);
    }

    public void OnQuestList(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            isQuestListActive = !isQuestListActive;
            questUI.gameObject.SetActive(isQuestListActive);
        }
    }

}
