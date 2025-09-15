using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class QuestManager : MonoBehaviour
{
    PlayerStats playerStats;

    //레벨, 퀘스트리스트
    public Dictionary<int, List<QuestData>> QuestList; // 퀘스트 리스트

    public Quest currentQuest;
    public int currentQuestStep = 0; // 현재 퀘스트 진행도


    private void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        QuestList = new Dictionary<int, List<QuestData>>();

        APIEvents.OnGetQuestData += GenerateData;

        playerStats.OnLevelUp += UnlockQuests;
        APIManager.Instance.Quest.RequestGetQuestData();
    }

    private void OnDestroy()
    {
        playerStats.OnLevelUp -= UnlockQuests;
        APIEvents.OnGetQuestData -= GenerateData;
    }

    void GenerateData(QuestData[] questData)
    {
        foreach (QuestData quest in questData)
        {
            Debug.Log(quest);
            if (!QuestList.ContainsKey(quest.requiredLevel))
            {
                QuestList.Add(quest.requiredLevel, new List<QuestData>());
            }
            QuestList[quest.requiredLevel].Add(quest);
        }
    }



    //레벨 따라 퀘스트 해금
    public void UnlockQuests()
    {

        if (!QuestList.ContainsKey(playerStats.Level)) { 
            Debug.Log("해당 레벨 퀘스트 없음.");
            return;
        }
        foreach(QuestData data in QuestList[playerStats.Level])
        {
            QuestEvents.QuestUnlocked(data);
        }
    }


}
