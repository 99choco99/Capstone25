using System;
using System.Text;
using TMPro;
using UnityEngine;
using UniversalGraph;

/// <summary>수락한 퀘스트의 진행도와 완료한 목표를 상시 표시하는 HUD입니다. 플레이어 입력은 변경하지 않습니다.</summary>
public class QuestUI : MonoBehaviour
{
    [SerializeField] private QuestContainer questContainer;
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI objectives;
    [SerializeField] private TextMeshProUGUI questState;
    [SerializeField, Min(0f)] private float refreshInterval = 0.25f;

    private IQuestController controller;
    private float nextRefreshTime;
    private readonly StringBuilder text = new();

    //============ 초기화와 갱신 ============

    private void Start()
    {
        controller = GameManager.Instance.QuestController;
        RefreshQuest();
    }

    private void Update()
    {
        // HUD 내용만 숨겨 두고 진행 기록은 계속 확인하므로, 수락·재시도 때도 자동으로 반영됩니다.
        if (Time.unscaledTime >= nextRefreshTime)
            RefreshQuest();
    }

    //============ 퀘스트 표시 ============

    /// <summary>진행 기록은 읽기만 하고, 목표의 달성 처리는 QuestManager에 맡깁니다.</summary>
    private void RefreshQuest()
    {
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        controller.QuestProgress.TryGetValue(questContainer.QuestId, out QuestProgress progress);
        QuestState state = progress?.state ?? QuestState.NotStarted;
        bool show = state == QuestState.InProgress || state == QuestState.CanComplete;
        content.SetActive(show);
        if (!show)
            return;

        questName.text = questContainer.questName;
        questState.text = state == QuestState.CanComplete ? "완료 보고 가능" : "진행 중";

        RefreshObjectives(progress);
    }

    //============ 목표 목록 표시 ============

    private void RefreshObjectives(QuestProgress progress)
    {
        text.Clear();
        foreach (NodeBaseData nodeData in questContainer.Nodes)
        {
            // 진입한 목표만 표시하되, 완료 후에도 진행 기록을 읽어 체크 표시를 남깁니다.
            if (nodeData is not QuestObjectiveNodeData data
                || data.EventKey == QuestEventKeys.Report
                || !progress.ObjectiveAmounts.TryGetValue(data.Guid, out int amount))
                continue;

            bool completed = progress.CompletedNodeGuids.Contains(data.Guid);
            text.Append(completed ? "<color=#91D890>[V] " : "[ ] ")
                .Append(data.ObjectiveDescription).Append("  ")
                .Append(amount).Append('/').Append(data.RequiredAmount);
            if (completed)
                text.Append("</color>");
            text.AppendLine();
        }

        string currentText = objectives.text;
        if (currentText == null || !text.Equals(currentText.AsSpan()))
            objectives.text = text.ToString();
    }
}
