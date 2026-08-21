using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
	[CreateAssetMenu(fileName = "QuestSystemSettings", menuName = "UniversalGraph/Quest System Settings")]
	public class QuestSystemSettings : ScriptableObject
	{
		[Header("而ㅼ뒪?\u0080 紐⑺몴 ?\u0080???ㅼ젙 (?쒕∼?ㅼ슫???쒖떆??")]
		[Tooltip("湲고쉷?먭? ??由ъ뒪?몄뿉 臾몄옄???? Kill, Collect, Build)??異붽??섎㈃ ?몃뱶 ?먮뵒?곗쓽 ?쒕∼?ㅼ슫??利됱떆 諛섏쁺?⑸땲??")]
		public List<string> CustomObjectiveTypes = new List<string> { "Kill", "Collect", "TalkTo", "Interact" };

		private static QuestSystemSettings _instance;

		public static QuestSystemSettings Instance
		{
			get
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0011: Expected O, but got Unknown
				if ((Object)_instance == (Object)null)
				{
					_instance = Resources.Load<QuestSystemSettings>("QuestSystemSettings");
				}
				return _instance;
			}
		}
	}
}
