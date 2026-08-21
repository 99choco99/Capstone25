using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
	[CreateAssetMenu(fileName = "NewQuestGraph", menuName = "Universal/Quest Graph")]
	public class QuestContainer : GraphContainer
	{
		[Header("Quest Identity")]
		public int id;

		public string questName = "New Quest";

		[TextArea(3, 5)]
		public string description;

		public int requiredLevel = 0;

		[Header("Legacy Config (?몃뱶濡?留덉씠洹몃젅?댁뀡 ???꾩떆 ?좎?)")]
		public List<int> prerequisiteQuestIds = new List<int>();

		public int startNPCId;

		public int turnInNPCId;

		public List<QuestObjective> objectives = new List<QuestObjective>();

		public QuestReward reward;

		public int questId => id;
	}
}
