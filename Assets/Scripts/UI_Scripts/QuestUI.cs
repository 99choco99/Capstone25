using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    [SerializeField] GameObject questPrefab;
    public Quest selectedQuest;
    [SerializeField] Transform content;
    [SerializeField] TextMeshProUGUI questName;
    [SerializeField] TextMeshProUGUI questGuide;
    public void SetNewQuest(Quest quest)
    {
        GameObject newQuest = Instantiate(questPrefab);
        newQuest.transform.SetParent(content);
        newQuest.name = quest.questName;
        newQuest.GetComponentInChildren<TextMeshProUGUI>().text = quest.questName;
        newQuest.GetComponent<Button>().onClick.AddListener(() => ShowQuestInfo(quest));
    }

    public void ShowQuestInfo(Quest quest)
    {
        selectedQuest= quest;
        questGuide.text = quest.script;
        questName.text = quest.questName;
    }

    public void SelectQuest()
    {

    }
    public void EndQuest(Quest quest)
    {
        Transform[] allQuests = content.GetComponentsInChildren<Transform>();
        foreach (Transform child in allQuests)
        {
            if (child.name == quest.questName)
            {
                Destroy(child.gameObject);
                questGuide.text = "";
                questName.text = "";
                return; // 찾았으면 종료
            }
        }
    }


}
