using System.Collections.Generic;

namespace UniversalGraph
{
	public interface IQuestController
	{
		Dictionary<int, QuestProgress> QuestProgress { get; }

		QuestProgress GetQuestStatus(int questId);

		void InvokeStatusChanged(QuestContainer container, QuestProgress progress);

		void TurnInQuest(int questId);
	}
}
