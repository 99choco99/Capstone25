using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// 프로젝트에서 사용하는 Quest 정의 목록입니다. 핵심 Runtime은 이 에셋이나 정의 목록을
    /// <c>QuestDefinitionRegistry.Initialize</c>에 명시적으로 전달받습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Universal/Quest Catalog")]
    public class QuestCatalog : ScriptableObject
    {
        [Tooltip("런타임에 등록할 Quest 정의입니다. Quest ID는 중복될 수 없습니다.")]
        public List<QuestContainer> quests = new List<QuestContainer>();
    }
}
