using System;

public static class QuestEventManager
{
	public static event Action<string, int, int> OnObjectiveEvent;

	public static event Action<string> OnActionTriggered;

	public static void ReportEvent(string type, int targetId, int amount = 1)
	{
		QuestEventManager.OnObjectiveEvent?.Invoke(type, targetId, amount);
	}

	public static void TriggerAction(string actionId)
	{
		QuestEventManager.OnActionTriggered?.Invoke(actionId);
	}
}
