using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Quest 정의를 ID로 찾기 위한 런타임 등록부입니다. 게임에서 원하는 에셋 로딩 방식으로
	/// 정의 목록을 준비한 뒤 <see cref="Initialize(IEnumerable{QuestContainer})"/>로 명시적으로 초기화합니다.
	/// </summary>
	public sealed class QuestDefinitionRegistry
	{
    private readonly Dictionary<int, QuestContainer> definitionsById = new();
    private readonly Dictionary<int, QuestGraphIndex> indexesById = new();
    private readonly List<QuestContainer> definitionsInRegistrationOrder = new();

    public static QuestDefinitionRegistry Instance { get; private set; }

    /// <summary>호출자가 전달한 원본 등록 순서를 유지하는 Quest 정의 목록입니다.</summary>
    public IReadOnlyList<QuestContainer> Definitions => definitionsInRegistrationOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    /// <summary>전달받은 Catalog의 정의로 런타임 인덱스를 교체합니다.</summary>
    public static void Initialize(QuestCatalog catalog)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog), "등록할 Quest Catalog가 필요합니다.");
        }

        Initialize(catalog.quests);
    }

    /// <summary>
    /// 명시적으로 전달받은 정의로 런타임 인덱스를 교체합니다. Addressables, 멀티플레이 서버,
    /// 테스트 데이터와 프로젝트 전용 콘텐츠 공급자가 사용하는 이식 가능한 진입점입니다.
    /// </summary>
    public static void Initialize(IEnumerable<QuestContainer> definitions)
    {
        if (definitions == null)
        {
            throw new ArgumentNullException(nameof(definitions), "등록할 Quest 정의 목록이 필요합니다.");
        }

        var registry = new QuestDefinitionRegistry();
        foreach (QuestContainer definition in definitions)
        {
            if (definition == null)
            {
                Debug.LogWarning("[Quest] Quest Catalog에 null 항목이 있어 무시했습니다.");
                continue;
            }

            if (definition.QuestId <= 0)
            {
                throw new InvalidOperationException(
                    $"Quest '{definition.name}'의 ID {definition.QuestId}은 올바르지 않습니다. 양수를 사용하세요.");
            }

            if (!GraphAssetMigrator.TryMigrate(definition, out _, out string migrationError))
            {
                throw new InvalidOperationException(migrationError);
            }

            if (registry.definitionsById.TryGetValue(definition.QuestId, out QuestContainer duplicate))
            {
                throw new InvalidOperationException(
                    $"중복된 Quest ID {definition.QuestId}: '{duplicate.name}', '{definition.name}'.");
            }

            if (!QuestGraphIndex.TryCreate(definition, out QuestGraphIndex index, out string indexError))
            {
                throw new InvalidOperationException($"Quest '{definition.name}'을 등록하지 못했습니다: {indexError}");
            }

            registry.definitionsById.Add(definition.QuestId, definition);
            registry.indexesById.Add(definition.QuestId, index);
            registry.definitionsInRegistrationOrder.Add(definition);
        }

        Instance = registry;
    }

    /// <summary>Quest 정의 하나를 반환하며, 등록되지 않은 ID이면 null을 반환합니다.</summary>
    public QuestContainer GetDefinition(int questId)
    {
        definitionsById.TryGetValue(questId, out QuestContainer definition);
        return definition;
    }

    /// <summary>등록된 Quest 정의 하나를 찾아 반환합니다.</summary>
    public bool TryGetDefinition(int questId, out QuestContainer definition)
    {
        return definitionsById.TryGetValue(questId, out definition);
    }

    /// <summary>등록된 Quest 정의와 초기화할 때 만들어 둔 조회용 인덱스를 반환합니다.</summary>
    internal bool TryGetQuestIndex(
        int questId,
        out QuestContainer container,
        out QuestGraphIndex index)
    {
        if (definitionsById.TryGetValue(questId, out container)
            && indexesById.TryGetValue(questId, out index))
        {
            return true;
        }

        container = null;
        index = null;
        return false;
    }
	}
}
