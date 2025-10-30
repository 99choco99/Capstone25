using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueLine
{
    public string speakerName; // 화자 이름
    public string sentence;    // 대사 내용
}

public class NpcDialogueData
{
    [JsonProperty("DEFAULT")]
    public List<DialogueLine> DefaultDialogue { get; set; }

    [JsonProperty("QUESTS")]
    public Dictionary<string, Dictionary<string, List<DialogueLine>>> Quests { get; set; }
}

public class DialogueManager : MonoBehaviour
{
    Player player;

    public event Action OnConversationStart;
    public event Action OnConversationEnd;
    public event Action<DialogueLine> OnShowLine;

    Dictionary<string, List<DialogueLine>> DialogueData = new();
    Queue<DialogueLine> currentDialogueQueue = new();


    private QuestInteractionInfo currentInteractionInfo = null;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        PublicAPIManager.Instance.Dialogue.OnGetDialogue += GenerateData;
        PublicAPIManager.Instance.Dialogue.RequestGetDialogue();
    }

    private void OnDestroy()
    {
        if(PublicAPIManager.Instance != null)
        {
            PublicAPIManager.Instance.Dialogue.OnGetDialogue -= GenerateData;
        }

    }

    void GenerateData(string jsonText)
    {
        try
        {
            var npcDataMap = JsonConvert.DeserializeObject<Dictionary<string, NpcDialogueData>>(jsonText);
            if (npcDataMap == null)
            {
                Debug.LogError("[DialogueManager] JSON 역직렬화에 실패했거나 결과가 null입니다.");
                return;
            }
            DialogueData.Clear();

            foreach (var npcEntry in npcDataMap)
            {
                string npcIdentifier = npcEntry.Key;
                NpcDialogueData npcData = npcEntry.Value;

                if (npcData == null)
                {
                    Debug.LogWarning($"[DialogueManager] NPC '{npcIdentifier}'의 데이터가 null입니다.");
                    continue;
                }

                // DEFAULT 대화 추가 
                if (npcData.DefaultDialogue != null)
                {
                    string defaultKey = $"{npcIdentifier}_DEFAULT";
                    if (!DialogueData.ContainsKey(defaultKey))
                    {
                        DialogueData.Add(defaultKey, npcData.DefaultDialogue);
                    }
                }

                // QUESTS 대화 추가
                if (npcData.Quests != null)
                {
                    foreach (var questEntry in npcData.Quests)
                    {
                        string questKey = questEntry.Key;
                        var situations = questEntry.Value;

                        if (situations == null)
                        {
                            Debug.LogWarning($"[DialogueManager] NPC '{npcIdentifier}'의 퀘스트 '{questKey}' 데이터가 null입니다. 건너뜁니다.");
                            continue;
                        }

                        foreach (var situationEntry in situations)
                        {
                            string situationKey = situationEntry.Key;
                            List<DialogueLine> lines = situationEntry.Value;
                            if (lines == null)
                            {
                                Debug.LogWarning($"[DialogueManager] 대화 키 '{npcIdentifier}_{questKey}_{situationKey}'의 대화 목록(lines)이 null입니다. 건너뜁니다.");
                                continue;
                            }

                            string finalKey = $"{npcIdentifier}_{questKey}_{situationKey}";

                            if (!DialogueData.ContainsKey(finalKey))
                            {
                                DialogueData.Add(finalKey, lines);
                            }
                            else
                            {
                                Debug.LogWarning($"[DialogueManager] 중복된 대화 키가 감지되었습니다: {finalKey}");
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException ex) // JSON 형식 자체가 잘못된 경우
        {
            Debug.LogError($"[DialogueManager] JSON 파싱 오류: {ex.Message}\n--- 원본 JSON ---\n{jsonText}");
        }
        catch (Exception ex) //그 외 예상치 못한 모든 오류
        {
            Debug.LogError($"[DialogueManager] 대화 데이터 처리 중 예기치 않은 오류 발생: {ex.Message}");
        }
    }

    public void StartConversation(QuestInteractionInfo interactionInfo)
    {
        currentInteractionInfo = interactionInfo;

        if (interactionInfo.Type == QuestInteractionType.Start)
        {
            player.Quest.StartQuest(interactionInfo.QuestId);
        }
        else if (interactionInfo.Type == QuestInteractionType.Talk)
        {
            player.Quest.ReportTalkToNPC(interactionInfo.NpcId);
        }

        if (DialogueData.TryGetValue(interactionInfo.DialogueKey, out List<DialogueLine> lines))
        {
            currentDialogueQueue.Clear();
            foreach (var dialog in lines)
            {
                currentDialogueQueue.Enqueue(dialog);
            }
            OnConversationStart?.Invoke();
            ShowNextLine();
        }
        else
        {
            EndConversation();
        }
    }

    public void ShowNextLine()
    {
        if(currentDialogueQueue.TryDequeue(out var line))
        {
            OnShowLine?.Invoke(line);
        }
        else
        {
            EndConversation();
        }
    }

    private void EndConversation()
    {
        if (currentInteractionInfo != null && currentInteractionInfo.Type == QuestInteractionType.Complete)
        {
            player.Quest.TurnInQuest(currentInteractionInfo.QuestId);
        }

        currentInteractionInfo = null; // 정보 초기화

        player.InputHandler.UseInteractionInput();
        OnConversationEnd?.Invoke();
    }
}
