using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    Dictionary<int, string[]> dialogue;
    PlayerUIManager playerUIManager;

    int currentIndex = 0;
    int dialogIndex = 0;
    string currentText;

    private void Awake()
    {
        playerUIManager = GetComponent<PlayerUIManager>();
        dialogue = new Dictionary<int, string[]>();
        GenerateData();
    }

    void GenerateData()
    {
        //기본 대화
        //Npc번호, 대화내용들
        dialogue.Add(100, new string[] { "First", "Second", "Third", "Forth" });
        dialogue.Add(200, new string[] { "No Quest 1", "No Quest 2", "No Quest 3" });

        //퀘스트 대화
        //npc번호 + 퀘스트 번호 + 퀘스트 순서 , 대화내용들
        dialogue.Add(100 + 10 + 0, new string[] { "You have to go NPC 200" });
        dialogue.Add(200 + 10 + 1, new string[] { "mission 10 Complete"});

        dialogue.Add(100 + 20 + 0, new string[] { "You have to GO NPC 200"});
        dialogue.Add(200 + 20 + 1, new string[] { "mission 20 Complete"});

        dialogue.Add(200 + 30 + 0, new string[] { "You have to go NPC100" });
        dialogue.Add(100 + 30 + 1, new string[] { "mission 30 Complete"});

    }

    public void StartConversation(NPC npc)
    {
        currentIndex = 0;
        playerUIManager.SetNpcName(npc.transform.name);

        dialogIndex = npc.id + 0;// + questManager.GetQuest(npc.id);
        if (dialogue.ContainsKey(dialogIndex))
        {
            currentText = GetDialog(dialogIndex, 0);
        }
        else
        {
            dialogIndex = npc.id;
            currentText = GetDialog(npc.id, 0);
        }
        playerUIManager.SetNpcText(currentText);
    }
    public string GetDialog(int id, int index)
    {
        if (index >= dialogue[id].Length)
        {
            return null;
        }
        else
        {
            return dialogue[id][index];
        }
    }

    public bool NextDialog()
    {
        currentText = GetDialog(dialogIndex, ++currentIndex);
        if (currentText != null) {
            playerUIManager.SetNpcText(currentText);
            return false;
        }
        else
        {
            return true;
        }
    }
}
