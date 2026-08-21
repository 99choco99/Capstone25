using System.Collections.Generic;
using UnityEngine;
using UniversalGraph;

public class QuestManager
{
	public static QuestManager Instance { get; private set; }

	public Dictionary<int, QuestContainer> QuestTemplates { get; private set; } = new Dictionary<int, QuestContainer>();


	public static void Init()
	{
		if (Instance != null)
		{
			return;
		}
		Instance = new QuestManager();
		Instance.QuestTemplates.Clear();
		QuestContainer[] array = Resources.LoadAll<QuestContainer>("");
		if (array == null)
		{
			return;
		}
		QuestContainer[] array2 = array;
		QuestContainer[] array3 = array2;
		foreach (QuestContainer questContainer in array3)
		{
			if (Instance.QuestTemplates.ContainsKey(questContainer.questId))
			{
				Debug.LogWarning((object)$"[QuestManager] 以묐났???섏뒪??ID媛\u0080 諛쒓껄?섏뿀?듬땲?? {questContainer.questId} ({((Object)questContainer).name})");
			}
			else
			{
				Instance.QuestTemplates[questContainer.questId] = questContainer;
			}
		}
		Debug.Log((object)$"[QuestManager] {array.Length}媛쒖쓽 ?섏뒪??洹몃옒?꾨? ?깃났?곸쑝濡?濡쒕뱶?덉뒿?덈떎.");
	}

	public QuestContainer GetQuestTemplate(int questId)
	{
		return CollectionExtensions.GetValueOrDefault<int, QuestContainer>((IReadOnlyDictionary<int, QuestContainer>)QuestTemplates, questId);
	}
}
