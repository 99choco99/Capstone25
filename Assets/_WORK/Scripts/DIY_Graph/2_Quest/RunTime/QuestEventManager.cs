using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// 게임과 Quest 시스템 경계에서 사용하는 가벼운 신호 통로입니다. 게임은 <see cref="ReportEvent"/>로
	/// 목표 진행을 보고하고 <see cref="OnActionTriggered"/>를 통해 그래프 Action 키를 받을 수 있습니다.
	/// </summary>
	public static class QuestEventManager
	{
    /// <summary>게임에서 수치로 누적할 Quest 진행을 보고하면 발생합니다.</summary>
    public static event Action<string, int, int> OnObjectiveEvent;

    /// <summary><c>IQuestActionReceiver</c>가 처리하지 않은 Action 노드의 대체 이벤트입니다.</summary>
    public static event Action<string> OnActionTriggered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        OnObjectiveEvent = null;
        OnActionTriggered = null;
    }

    /// <summary>고정 이벤트 키와 대상 ID, 양수 진행량을 전달합니다.</summary>
    public static void ReportEvent(string type, int targetId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            Debug.LogWarning("[Quest] 타입 키가 빈 Objective 이벤트는 무시했습니다.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"[Quest] 진행량이 양수가 아닌 Objective 이벤트 '{type}'은 무시했습니다. 입력값: {amount}.");
            return;
        }

        OnObjectiveEvent?.Invoke(type.Trim(), targetId, amount);
    }

    /// <summary>프로젝트에서 정의한 Action 키를 전달합니다.</summary>
    public static void TriggerAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            Debug.LogWarning("[Quest] 빈 Action 키는 무시했습니다.");
            return;
        }

        OnActionTriggered?.Invoke(actionId.Trim());
    }
	}
}
