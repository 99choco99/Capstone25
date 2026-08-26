using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Quest 정의를 ID로 찾기 위한 런타임 인덱스입니다. 게임에서 원하는 에셋 로딩 방식으로 직접 초기화할 수 있으며,
	/// <see cref="Init"/>은 소규모 프로젝트를 위한 Resources 기반 간편 함수입니다.
	/// </summary>
	public sealed class QuestManager
	{
    private readonly Dictionary<int, QuestContainer> questTemplates = new();

    public static QuestManager Instance { get; private set; }

    /// <summary>고정 Quest ID로 구성된 읽기 전용 Quest 정의 목록입니다.</summary>
    public IReadOnlyDictionary<int, QuestContainer> QuestTemplates => questTemplates;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    /// <summary>
    /// <c>Resources/QuestCatalog</c>에서 초기화합니다. Addressables, 의존성 주입, 원격 콘텐츠 또는
    /// 별도 로딩 방식을 사용한다면 <see cref="Initialize(QuestCatalog)"/>를 사용합니다.
    /// </summary>
    public static void Init()
    {
        if (Instance != null)
        {
            return;
        }

        QuestCatalog catalog = Resources.Load<QuestCatalog>("QuestCatalog");
        if (catalog == null)
        {
            Instance = new QuestManager();
            Debug.LogWarning(
                "[Quest] Resources/QuestCatalog을 찾지 못했습니다. " +
                "다른 로딩 방식을 사용하려면 QuestManager.Initialize(catalog)을 호출하세요.");
            return;
        }

        Initialize(catalog);
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

        var manager = new QuestManager();
        foreach (QuestContainer definition in definitions)
        {
            if (definition == null)
            {
                Debug.LogWarning("[Quest] Quest Catalog에 null 항목이 있어 무시했습니다.");
                continue;
            }

            if (!GraphAssetMigrator.TryMigrate(definition, out _, out string migrationError))
            {
                throw new InvalidOperationException(migrationError);
            }

            if (manager.questTemplates.TryGetValue(definition.questId, out QuestContainer duplicate))
            {
                throw new InvalidOperationException(
                    $"중복된 Quest ID {definition.questId}: '{duplicate.name}', '{definition.name}'.");
            }

            if (!QuestGraphIndex.TryCreate(definition, out _, out string indexError))
            {
                throw new InvalidOperationException($"Quest '{definition.name}'을 등록하지 못했습니다: {indexError}");
            }

            manager.questTemplates.Add(definition.questId, definition);
        }

        Instance = manager;
        Debug.Log($"[Quest] Quest 정의 {manager.questTemplates.Count}개를 등록했습니다.");
    }

    /// <summary>Quest 정의 하나를 반환하며, 등록되지 않은 ID이면 null을 반환합니다.</summary>
    public QuestContainer GetQuestTemplate(int questId)
    {
        questTemplates.TryGetValue(questId, out QuestContainer template);
        return template;
    }

    /// <summary>등록된 Quest 정의 하나를 찾아 반환합니다.</summary>
    public bool TryGetQuestTemplate(int questId, out QuestContainer template)
    {
        return questTemplates.TryGetValue(questId, out template);
    }

    /// <summary>등록된 Quest를 찾고 현재 그래프 구조로 조회용 인덱스를 만듭니다.</summary>
    internal bool TryBuildQuestIndex(
        int questId,
        out QuestContainer container,
        out QuestGraphIndex index)
    {
        index = null;
        if (!questTemplates.TryGetValue(questId, out container))
        {
            return false;
        }

        if (QuestGraphIndex.TryCreate(container, out index, out string error))
        {
            return true;
        }

        Debug.LogError($"[Quest] '{container.name}'의 그래프를 읽지 못했습니다: {error}", container);
        return false;
    }
	}
}
