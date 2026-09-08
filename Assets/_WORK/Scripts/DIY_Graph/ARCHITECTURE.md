# UniversalGraph 코드 분석 지도

이 문서는 모든 파일을 처음부터 읽지 않고도 UniversalGraph의 전체 흐름을 파악하기 위한 안내서입니다.
현재 C# 파일 수가 많은 가장 큰 이유는 노드마다 **저장 데이터**와 **에디터 화면**이 한 쌍으로 존재하고,
Dialogue와 Quest가 각각 Runtime, Editor, Attribute Binding을 갖기 때문입니다.

## 먼저 알아야 할 세 계층

```text
Graph Asset
  └─ GraphContainer + NodeBaseData + NodeLinkData
            │
            ├─ Editor: GraphNode 화면 ↔ GraphViewSerializer 저장·복원
            │
            └─ Runtime: DialogueManager 또는 QuestRunner가 데이터 실행
```

- **Data**: Unity 에셋에 실제로 저장되는 값입니다. `UnityEditor`와 화면 코드에 의존하지 않습니다.
- **Runtime**: 저장된 데이터를 읽고 대화 또는 Quest를 진행합니다.
- **Editor**: Data를 GraphView 노드로 보여주고 편집한 뒤 다시 Data에 저장합니다.

## 처음 읽을 파일 10개

아래 순서만 읽으면 저장, 편집, 실행의 큰 흐름을 이해할 수 있습니다.

1. `Data/GraphContainer.cs` — 그래프 에셋이 저장하는 노드와 연결 목록
2. `Data/NodeBaseData.cs` — 모든 노드 데이터의 GUID와 위치
3. `Data/NodeLinkData.cs` — 출력 포트와 입력 포트의 연결
4. `Editor/GraphNode.cs` — Data와 GraphView 화면 노드를 묶는 공통 부모
5. `Editor/GraphNodeEditorRegistry.cs` — Data 타입에 맞는 화면 노드를 찾는 등록부
6. `Editor/GraphViewSerializer.cs` — 에셋과 화면 사이의 저장·불러오기
7. `1_Dialogue/Data/DialogueContainer.cs` — 이름 있는 Dialogue 시작점
8. `1_Dialogue/Runtime/DialogueManager.API.cs` — Dialogue 실행 공개 API
9. `2_Quest/Runtime/QuestDefinitionRegistry.cs` — Quest 정의 등록과 ID 조회
10. `2_Quest/Runtime/QuestRunner.cs` — Quest 진행 상태와 노드 실행

처음 분석할 때 `Binding`, `Validation`, `Save`, `Tests`는 건너뛰어도 됩니다.
이 폴더들은 기본 실행 흐름을 이해한 뒤 필요한 기능을 추적할 때 읽습니다.

## Editor 저장·불러오기 흐름

```text
UniversalGraphWindow
  ├─ UniversalGraphView                 캔버스와 노드 선택·연결
  ├─ NodeInspector                      선택한 노드의 필드 편집
  └─ GraphViewSerializer
       ├─ WriteGraphViewToContainer     GraphNode → NodeBaseData/NodeLinkData
       └─ LoadGraph
            └─ GraphNodeEditorRegistry  NodeBaseData 실제 타입 → GraphNode 화면 타입
```

`GraphNodeEditorRegistry.CreateNode(container, nodeData)`에서 `new GraphNode()`를 직접 하지 않는 이유는
`NodeBaseData`의 실제 타입마다 포트와 Inspector가 다른 구체 화면 클래스가 필요하기 때문입니다.
화면 클래스의 `[GraphNodeEditor]`와 `GraphNode<TData>`의 `TData`가 그 대응 관계를 등록합니다.

## Dialogue 실행 흐름

```text
게임 코드
  └─ DialogueManager.StartConversation
       ├─ DialogueContainer.TryResolveEntry
       ├─ 실행용 노드·연결 인덱스 생성
       └─ 현재 노드 실행
            ├─ DialogueLineNode       대사 이벤트 발생 후 입력 대기
            ├─ DialogueChoiceNode     선택지 조건 평가 후 선택 입력 대기
            ├─ Condition     조건 평가 후 포트 선택
            ├─ Action        Attribute 메서드 실행
            ├─ Wait          시간 대기
            ├─ WaitSignal    DialogueManager.SendSignal 대기
            └─ End           대화 종료
```

- `DialogueManager.API.cs`: 대화 시작·종료 요청과 게임/UI가 호출하는 공개 API
- `DialogueManager.Execution.cs`: 노드 종류별 실행과 선택지 조건 평가
- `DialogueManager.Navigation.cs`: 포트에 연결된 다음 노드 탐색과 이동
- `DialogueManager.State.cs`: 실행 인덱스, 종료, 상태 정리와 외부 콜백 실행

게임 UI는 `DialogueManager` 이벤트를 구독하고 다음 대사 또는 선택지만 전달합니다.
그래프 실행기는 특정 UI, Player, NPC 클래스를 알지 못합니다.

## Quest 실행 흐름

```text
게임 시작
  └─ QuestDefinitionRegistry.Initialize            Quest 정의 등록

플레이어별 연결
  └─ IQuestController                   QuestProgress 보관·변경 알림

NPC 또는 오브젝트 상호작용
  └─ QuestQueries.GetQuestOffers
       └─ Interaction Entry부터 조건 경로 평가
            ├─ QuestOffer 목록을 UI에 제공
            └─ QuestRunner.TryAcceptQuest가 수락 직전 같은 조건을 다시 검사

게임 흐름에서 직접 시작
  └─ QuestRunner.StartQuest                Offer 조건 없이 지정한 Quest 시작

게임 이벤트
  ├─ QuestRunner.AdvanceObjective          목표 하나를 직접 진행
  └─ QuestRunner.ReportObjectiveProgress   EventKey·TargetId가 같은 목표를 일괄 진행
       ├─ 목표 수치 갱신
       ├─ 조건·Action·보상 노드 실행
       └─ IQuestController.InvokeStatusChanged
```

- `QuestDefinitionRegistry`: 모든 플레이어가 공유하는 Quest **정의 목록**
- `IQuestController`: 플레이어 한 명이 소유한 Quest **진행 상태**
- `QuestRunner`: 노드를 따라가며 진행 상태를 **변경하는 실행기**
- `QuestQueries`: UI가 그래프를 직접 읽지 않도록 제공하는 **조회 API**
- `QuestInteractionQuery`: API 내부에서 Quest 상태에 맞는 `DialogueCandidate`와 `QuestOffer`를 만드는 **조회 전용 연결기**

`GetQuestOffers`와 `GetDialogueCandidates`는 후보를 선택하거나 정렬하지 않습니다. 게임 코드는 반환된
데이터를 이용해 자동 선택, 플레이어 선택, 추적 Quest 우선 같은 프로젝트 전용 정책을 정합니다.
상태를 게임 코드에서 직접 바꿀 때는 `SetQuestState`를 사용하며, Wait For Quest 노드는 기획자가 지정한
`RequiredState`에 도달했을 때만 상위 흐름을 재개합니다.

Quest 진행 기록이 아직 없는 ID는 조건 평가에서 `NotStarted`로 취급합니다. 여러 Quest는 서로 다른
`QuestProgress`로 동시에 진행되며, 동시에 진행할 수 없는 조합은 Offer 앞의 Quest 상태 또는 Attribute
Condition으로 작성합니다. UI가 받은 Offer는 표시 중 상태가 바뀔 수 있으므로 `TryAcceptQuest`가 원래
Interaction Entry부터 다시 평가하고 같은 Offer에 도달할 때만 시작합니다.

`QuestRunner`의 공개 진입점은 `QuestRunner.cs`에 있습니다. `QuestRunner.Flow.cs`는 즉시 노드 흐름과
그 안에서 필요한 Attribute 메서드 호출을 처리합니다. 기존 `QuestRunner.Bindings.cs`는 이 파일로 통합했습니다.
별도 클래스 `QuestGraphIndex.cs`는 정의를 등록할 때 노드·연결 조회용 인덱스를 만듭니다.

Objective의 `EventKey`는 게임이 보고한 이벤트 종류, `TargetId`는 그 대상의 고정 ID입니다.
`TargetReference`는 제작·UI에서 이용할 선택적 Unity 객체 참조이며 이벤트 일치 판정에는 참여하지 않습니다.
게임이 정한 목표 실패는 `Failed`, 그래프·메서드 실행 오류로 중단된 경우는 `ExecutionError`로 구분합니다.

## Quest 저장·복원 흐름

1. `QuestRunner`의 공개 진행 API 호출이 끝난 뒤 `QuestSaveData.Capture`로 진행 기록을 복사합니다.
2. `ToJson` 또는 게임의 Serializer로 저장하고, 불러올 때 `TryFromJson`으로 현재 저장 버전을 확인합니다.
3. `TryApplyTo`는 모든 기록을 검증한 뒤 Controller의 데이터를 교체하거나 병합합니다. 아직 노드를 실행하거나 알림을 보내지 않습니다.
4. 인벤토리 등 다른 게임 데이터까지 모두 복원한 뒤 `QuestRunner.ResumeRestoredQuests`를 호출합니다. Quest 간 대기를 재평가하여 흐름을 재개하고 변경 알림을 보냅니다.

Action 내부에서 즉시 저장하지 않습니다. 저장 요청 플래그만 남기고 게임 코드가 노드 실행이 끝난 뒤 저장해야 합니다.
복원 후 재개 시에도 Action이 실행될 수 있으므로 다른 게임 데이터보다 먼저 재개하지 않습니다.
`QuestProgressSaveData`는 Quest 하나의 진행 기록, `QuestSaveData`는 그 목록과 저장 스키마 버전을 보관합니다.
현재 저장 형식은 최초 버전 1이며 이전 테스트 세이브는 지원하지 않습니다. 변환 규칙이 없어 `QuestSaveMigrator`는 제거했습니다.

## Editor 검증 코드 구성

- `GraphValidation.cs`: 심각도와 개별 진단 결과
- `GraphValidationIndex.cs`: 도메인 검증기가 공유하는 읽기 전용 그래프 인덱스
- `GraphValidator.cs`: 검증기 인터페이스와 강타입 부모 클래스
- `GraphValidatorRegistry.cs`: 검증기 검색과 전체 검증 공개 진입점
- `GraphStructureValidator.cs`: 모든 그래프에 공통인 직렬화·연결 무결성 검사

## Attribute Binding은 마지막에 읽기

```text
[DialogueAction] / [QuestAction]
          │
          ├─ Editor: TypeCache → Factory → Catalog → Inspector에서 선택
          └─ Runtime: Reflection → Factory → Invoker에 등록
                    │
                    └─ 저장된 인수를 복원해 메서드 실행
```

각 도메인 Binding 폴더의 `Attribute → DescriptorFactory → Descriptor → Invoker`가 한 묶음입니다.
공통 `Runtime/Binding`은 두 도메인이 함께 사용하는 `MethodDescriptor`,
인수 데이터, Parameter Descriptor와 Codec을 가집니다. Dialogue와 Quest Descriptor는 대상 종류와 표시 이름만 추가합니다.
Editor의 `MethodCatalog`와 `MethodBindingInspector`는 같은 Descriptor를 이용해 드롭다운과 인수 필드를 만듭니다.

## 노드 하나를 추가할 때

새 노드는 보통 다음 요소가 필요합니다.

1. `Data/Nodes`: `NodeBaseData`를 상속한 직렬화 데이터
2. `Editor/Nodes`: `GraphNode<TData>`를 상속한 화면과 `[GraphNodeEditor]`
3. Runtime 실행기: 새 데이터 타입을 처리하는 분기
4. Validator: 필수 포트와 값 검증
5. Test: 저장·실행·잘못된 데이터 검증

Data와 Editor Node를 분리하는 것은 중복이 아니라 플레이어 빌드에서 GraphView 코드를 제외하기 위한 경계입니다.

## 그래프 스키마 마이그레이션 흐름

현재 저장 형식을 최초 버전 1로 사용합니다. 새 `GraphContainer`는 생성할 때
`GraphAssetMigrator.CurrentVersion`으로 시작하고, 저장된 에셋은 자신의 `SchemaVersion`을 유지합니다.
이 기준 이전의 테스트 그래프는 호환하지 않으며 다시 생성해야 합니다. 현재는 변환 단계가 없습니다.

호출은 `GraphAssetMigrator.Migrate(container)` 하나로 통일합니다. 성공하면 변환 결과를 반환하고,
실패하면 `InvalidOperationException`을 던집니다. 에디터는 결과의 `Changed`로 저장 여부를 결정하며,
대화 시작 API는 예외를 받아 로그와 `false`로 처리합니다.

다음 저장 형식 변경 시 `CurrentVersion`을 2로 올리고 `GraphAssetMigrator.Migrate`에
`MigrateVersion1To2` 변환과 성공 후 `SetSchemaVersion(2)` 호출을 추가합니다.
이후에는 `MigrateVersion2To3`처럼 단계를 보존하고, 에셋 버전부터 현재 버전까지 순서대로 실행하도록 합니다.
버전 숫자만 올리고 변환을 구현하지 않으면 구버전 에셋은 거부됩니다.
이미 사용 중인 데이터를 보존하기 시작한 이후에는 배포한 변환 규칙을 수정하지 않습니다.

Quest 진행 저장 파일의 버전은 그래프 에셋과 별개로 `QuestSaveData.CurrentSchemaVersion`에서 관리합니다.

## Reflection 등록과 빌드 검증

Invoker는 관련 어셈블리를 최초 초기화 때만 탐색합니다. Attribute의 Key로 Descriptor를 보관하고,
호출할 때는 Codec으로 저장된 인수를 복원한 뒤 Context를 추가하여 `MethodInfo.Invoke`에 전달합니다.
MethodInfo와 Descriptor는 런타임 캐시이며 그래프 에셋에는 Key와 인수 데이터만 저장됩니다.

`GraphMethodBuildValidator`는 같은 Factory 규칙으로 작성 오류와 중복 키를 검사합니다.
`GraphValidatorRegistry`는 처음 사용할 때 검증기를 등록하고, 그래프의 공통 구조 검사 후 도메인 규칙을 검사합니다.
`GraphBuildValidator`는 에셋 임포트까지 완료된 스크립트 reload 후와 Mono·IL2CPP 빌드 전에
메서드와 모든 그래프를 자동 검사합니다. Error는 빌드를 차단하고 Warning은 허용하며 에셋은 수정하지 않습니다.
Attribute의 Preserve 상속과 `link.xml`은 빌드에서 Reflection에 필요한 코드를 보존합니다.
에디터 검사만으로 코드 보존까지 보장하지 않으므로 대상 플랫폼 IL2CPP Smoke Build 검증이 필요합니다.
