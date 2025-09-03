using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 UI 아이템을 관리하는 별도 스크립트
public class QuestUIItem : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI questNameText;
    private QuestData currentQuestData;

    public void Initialize(QuestData data)
    {
        currentQuestData = data;
        questNameText.text = data.questName;
    }
}