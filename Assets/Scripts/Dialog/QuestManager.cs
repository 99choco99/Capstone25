using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class QuestManager : MonoBehaviour
{
    PlayerSetting playerSetting;

    //레벨, 퀘스트리스트
    public Dictionary<int, List<QuestData>> QuestList; // 퀘스트 리스트

    public Quest currentQuest;
    public int currentQuestStep = 0; // 현재 퀘스트 진행도
    bool isQuestActive = false;


    private void Awake()
    {
        playerSetting = GetComponentInParent<PlayerSetting>();
        QuestList = new Dictionary<int, List<QuestData>>();
        GenerateData();

        playerSetting.OnLevelUp += UnlockQuests;
    }

    private void OnDisable()
    {
        playerSetting.OnLevelUp -= UnlockQuests;
    }

    void GenerateData()
    {
        
        // 퀘스트 리스트 받아오기
        // 퀘스트 번호, 퀘스트 이름, 관련 NPC, 퀘스트 설명

    }


    //레벨 따라 퀘스트 해금
    public void UnlockQuests()
    {
        if (QuestList[playerSetting.level].Count <= 0) { Debug.Log("해당 레벨 퀘스트 없음."); }
        foreach(QuestData data in QuestList[playerSetting.level])
        {
            QuestEvents.QuestUnlocked(data);
        }
    }


}
