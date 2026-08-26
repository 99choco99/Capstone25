using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// 프로젝트에서 사용하는 Quest 정의 목록입니다. 핵심 Runtime은 이 에셋을 직접 받을 수 있어
    /// Resources에 둘 필요가 없으며, 선택적 단축 함수인 <c>QuestManager.Init()</c>만 Resources를 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Universal/Quest Catalog")]
    public class QuestCatalog : ScriptableObject
    {
        [Tooltip("런타임에 등록할 Quest 정의입니다. Quest ID는 중복될 수 없습니다.")]
        public List<QuestContainer> quests = new List<QuestContainer>();
    }
}
