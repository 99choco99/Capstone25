using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 UI 아이템을 관리하는 별도 스크립트
public class QuestUIItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questStatusText;

    private Action<int> OnItemSelected;

    public int QuestId { get; private set; }

    public void Awake()
    {
        button.onClick.AddListener(OnItemClicked);
    }

    public void Initialize(QuestDefinition data, QuestStatus status, System.Action<int> OnSelectCallback)
    {
        this.QuestId = data.questID;
        OnItemSelected += OnSelectCallback;
        questNameText.text = data.questName;
        UpdateUI(status);
    }

    public void UpdateUI(QuestStatus status)
    {
        switch (status.state)
        {
            case QuestState.Ready:
                questStatusText.text = "[시작 가능]";
                questStatusText.color = Color.black;
                break;
            case QuestState.InProgress:
                questStatusText.text = "[진행중]";
                questStatusText.color = Color.green;
                break;
            case QuestState.Complete:
                questStatusText.text = "[완료]";
                questStatusText.color = Color.gray;
                break;
            case QuestState.TurnedIn:
                questStatusText.text = "[종료]";
                questStatusText.color = Color.gray;
                break;
            default:

                break;
        }
        if (status.IsFocused)
        {
            questStatusText.text = "[집중]";
            questStatusText.color = Color.red;
        }
    }

    public void OnItemClicked()
    {
        OnItemSelected?.Invoke(QuestId);
    }
}