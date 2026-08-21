using System;
using System.Collections.Generic;
using UniversalGraph;

[Serializable]
public class QuestProgress
{
	public int questId;

	public QuestState state;

	public int[] objectiveProgresses;

	public string currentNodeGuid;

	public List<string> activeNodeGuids = new List<string>();

	public Dictionary<string, int> nodeProgressCounts = new Dictionary<string, int>();

	public int currentObjectiveCount;

	public QuestProgress()
	{
	}

	public QuestProgress(QuestContainer data)
	{
		questId = data.questId;
		state = QuestState.Locked;
		currentNodeGuid = string.Empty;
		currentObjectiveCount = 0;
		if (data.objectives != null)
		{
			objectiveProgresses = new int[data.objectives.Count];
		}
	}
}
