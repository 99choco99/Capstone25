using System;
using UnityEditor;
using UnityEngine;

namespace UniversalDialogue.Editor
{
	internal static class DialogueGraphMenu
	{
		[MenuItem("Window/Dialogue Graph")]
		private static void CreateDialogueGraph()
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Expected O, but got Unknown
			string text = EditorUtility.SaveFilePanelInProject("새 다이얼로그 파일 생성", "NewDialogue", "asset", "새 다이얼로그 에셋을 저장할 위치를 선택하세요.");
			if (!string.IsNullOrEmpty(text))
			{
				DialogueContainer val = ScriptableObject.CreateInstance<DialogueContainer>();
				((GraphContainer)(object)val).Nodes.Add((NodeBaseData)new StartNodeData
				{
					Guid = Guid.NewGuid().ToString(),
					Position = new Vector2(100f, 100f),
					EntryId = "Default"
				});
				AssetDatabase.CreateAsset((Object)(object)val, text);
				AssetDatabase.SaveAssets();
				UniversalGraphWindow.Open((GraphContainer)(object)val);
			}
		}
	}
}
