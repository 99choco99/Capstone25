# UniversalGraph Dialogue & Quest

UniversalGraph는 Dialogue와 Quest Runtime을 제공하는 이식 가능한 Unity 그래프 작성 기반입니다. 그래프 데이터와 실행기는 현재 프로젝트의 `Player`, `NPC`, UI, 인벤토리, 저장 시스템, Addressables 코드에 의존하지 않습니다.

## 처음 코드를 분석한다면

전체 파일을 순서대로 읽지 말고 먼저 [`ARCHITECTURE.md`](ARCHITECTURE.md)를 확인합니다.
공통 Data → Editor 저장·복원 → Dialogue Runtime → Quest Runtime 순서와 처음 읽을 핵심 파일 10개를 정리했습니다.
`Binding`, `Validation`, `Save`, `Tests`는 기본 흐름을 이해한 뒤 필요한 기능만 추적하면 됩니다.

## 어셈블리 경계

- `UniversalGraph.Runtime`: 공통 그래프 데이터, Dialogue·Quest Runtime, Reflection 기반 Attribute 호출
- `UniversalGraph.Editor`: 공통 GraphView, 노드 등록부, Inspector, Serializer, Undo, 에셋 창
- `UniversalGraph.Dialogue.Editor`: Dialogue 노드 화면과 메서드 인수 입력 필드
- `UniversalGraph.Quest.Editor`: Quest 진행 및 대화 경로 노드 화면
- `UniversalGraph.EditModeTests`: 핵심 Runtime 동작 테스트

Editor 어셈블리는 플레이어 빌드에 포함되지 않습니다. Runtime 어셈블리는 현재 게임의 `Player`, `NPC`, 인벤토리, UI, 네트워크 클래스를 참조하지 않습니다.

## 폴더 구조

```text
DIY_Graph
|-- Data                     공통 그래프 에셋과 연결 데이터
|   `-- Migrations           그래프 스키마 버전 확인과 향후 변환 진입점
|-- Runtime
|   `-- Binding              공통 메서드 설명자, 인수와 직렬화 Codec
|-- Editor                   공통 그래프 창, Serializer, Inspector, 검증
|   `-- Styles               공통 USS 디자인과 상태 클래스
|-- 1_Dialogue
|   |-- Data                 Dialogue 컨테이너와 직렬화 데이터
|   |   `-- Nodes            Dialogue 노드 데이터
|   |-- Runtime              Dialogue 재생과 씬 연결 API
|   |   `-- Binding          Dialogue Attribute와 메서드 호출기
|   `-- Editor
|       `-- Nodes            Dialogue 노드 화면
|-- 2_Quest
|   |-- Data                 Quest 정의와 진행 데이터
|   |   `-- Nodes            Quest 노드 데이터
|   |-- Runtime              Quest 정의 등록부, Runner, 조회 API
|   |   |-- Binding          Quest Attribute와 메서드 호출기
|   |   `-- Save             저장 DTO, 검증과 복원
|   `-- Editor
|       `-- Nodes            Quest 노드 화면
`-- Tests                    EditMode 자동 테스트
```

폴더는 코드의 역할만 구분하며 추가 asmdef를 만들지 않습니다. 컴파일과 의존성 규칙은 위의 다섯 어셈블리 경계가 결정합니다.

## Dialogue 연동

그래프에서 실행할 게임 메서드에 고정 키를 부여합니다.

```csharp
[DialogueAction("inventory.give-item", Owner = DialogueMethodOwner.Interactor)]
public void GiveItem(ItemData item, int amount, bool showPopup)
{
    // 게임 전용 구현
}
```

`ItemData`는 `ScriptableObject` 또는 다른 `UnityEngine.Object`일 수 있습니다. 현재 그래프에서 편집할 수 있는 인수는 string, bool, int, float, enum, Unity 객체와 자동 주입되는 `DialogueExecutionContext` 하나입니다. `ref`, `out`, `in`, 선택적 인수, `params`, 제네릭, 비동기, 임의 관리 객체 인수는 진단 오류로 거부합니다.

런타임은 관련 어셈블리의 Attribute 메서드를 최초 초기화 때 Reflection으로 찾아 캐시하고, 이후에는 저장된 MethodInfo로 호출합니다. 에디터는 TypeCache로 메서드를 찾아 같은 Factory 규칙으로 검증합니다. 별도 Generator DLL이나 생성 코드가 필요하지 않습니다. 그래프에 저장되는 인수 ID는 편집 가능한 파라미터 순서로 자동 생성되므로 파라미터 이름은 자유롭게 바꿀 수 있습니다. 단, 기존 그래프가 사용 중일 때 파라미터 순서를 바꾸거나 중간 파라미터를 삭제하면 호출 계약이 달라집니다.

게임 코드는 `DialogueManager.StartConversation`에 `DialogueEntryPoint`와 선택적인 `DialogueExecutionContext`를 전달합니다. 텍스트 전용 또는 전역 메서드만 쓰는 대화는 실행 문맥 없이도 시작할 수 있습니다. UI는 `ShowLine`과 `ShowChoices`를 구독하고 `ContinueDialogue` 또는 `SelectChoice`를 호출합니다.

`DialogueLineNode`는 대사 한 줄을 표시하고, 다음에 연결한 `DialogueChoiceNode`는 선택지 묶음을 표시합니다. UI가 `ShowChoices`에서 기존 대사를 지우지 않으면 마지막 대사를 유지한 채 선택지를 함께 보여줄 수 있습니다.

각 선택지는 `DialogueCondition`을 가질 수 있습니다. Condition은 `DialogueChoiceNode`에 진입할 때 평가하며 false인 선택지는 표시하지 않고 선택 요청도 거부합니다. 표시 가능한 선택지가 하나도 없으면 `Default` 포트로 즉시 진행합니다.

## Quest 연동

게임의 플레이어 및 Quest 소유 구조에 맞춰 `IQuestController`를 한 번 구현합니다. 해당 Controller의 인스턴스 메서드를 Quest Action, Condition, Reward 노드에 공개할 수 있습니다. 다른 컴포넌트의 기능은 Controller의 연결 메서드에서 호출하거나, `Owner = QuestMethodOwner.Global`인 static 메서드로 연결합니다.

```csharp
[QuestAction("inventory.give-item", Owner = QuestMethodOwner.Controller)]
public void GiveItem(QuestExecutionContext context, ItemData item, int amount)
{
    // 게임 전용 구현
}
```

Quest는 Dialogue와 같은 기본형, enum, 에셋 인수와 자동 주입되는 `QuestExecutionContext` 하나를 지원합니다. Action과 Condition은 Attribute가 붙은 메서드만 실행하며, 등록되지 않은 키는 에디터 검증과 런타임 오류로 바로 알려줍니다.

이식 가능한 로더에서는 Quest 정의를 명시적으로 등록합니다.

```csharp
QuestDefinitionRegistry.Initialize(loadedQuestCatalog);
```

게임은 `QuestRunner.AdvanceObjective`로 목표 하나를 직접 진행할 수 있습니다. 처치·수집처럼 이벤트 키와 대상 ID로 여러 Quest를 함께 갱신할 때는 `QuestRunner.ReportObjectiveProgress`를 사용합니다. Objective의 `EventKey`와 `TargetId`가 보고한 이벤트에 일치하면 진행량이 누적됩니다. `TargetReference`는 제작·UI용으로 제공하는 선택적 Unity 객체 참조이며, 런타임 일치 판정에는 사용하지 않습니다. `QuestQueries`는 `IQuestController`와 고정 상호작용 대상 문자열만 사용해 Quest 상태별 대화 경로를 `DialogueCandidate` 또는 `QuestOffer`로 반환합니다.

Quest를 제공할 때는 그래프에 `Interaction Entry → Condition → Quest Offer` 경로를 만듭니다. False 경로를 연결하지 않으면 목록에서 숨길 수 있고, 선택할 수 없는 Offer 노드로 연결하면 이유가 있는 비활성 항목을 표시할 수 있습니다.

```csharp
QuestOffer[] offers = QuestQueries.GetQuestOffers(controller, npcId);
QuestOffer selected = offers.First(offer => offer.IsAvailable);
bool started = QuestRunner.TryAcceptQuest(controller, selected);
```

`TryAcceptQuest`는 UI에 표시한 뒤 조건이 바뀐 오래된 Offer를 수락하지 않도록 같은 그래프 경로를 다시 검사합니다. 컷신·튜토리얼·하위 Quest처럼 게임 흐름이 시작을 이미 결정했다면 `StartQuest(controller, questId)`로 Offer 조건을 거치지 않고 시작합니다. 게임이 재시작·포기 정책을 결정한 뒤 `ResetQuest`를 호출하면 모든 노드 진행 기록을 지우고 `NotStarted` 상태로 되돌릴 수 있습니다.

`QuestQueries.GetQuestOffers`와 `GetDialogueCandidates`는 후보를 정렬하거나 하나를 선택하지 않습니다. UI 표시 순서, 자동 선택, 추적 Quest 우선 같은 규칙은 `DialogueCandidate.Priority`, `QuestOffer.Priority`, 상태와 프로젝트 데이터를 이용해 게임에서 결정합니다. 현재 목표 UI도 `GetCurrentObjectives`가 반환하는 구조화된 데이터를 원하는 문장과 형식으로 표시합니다.

게임 코드가 Quest 상태만 직접 변경할 때는 `QuestRunner.SetQuestState`를 사용합니다. Reward 노드는 보상 Action만 실행하므로 완료 시점은 State Change 노드나 게임 코드가 결정합니다. `CanComplete`와 `TurnedIn` State Change는 현재 실행을 끝내는 종점이며, `InProgress` State Change만 다음 흐름을 가질 수 있습니다. Wait For Quest 노드는 대상 Quest를 자동 시작하지 않고, 다른 Quest가 Inspector에서 선택한 `RequiredState`에 도달할 때까지 현재 흐름을 기다립니다.

`Failed`는 Fail 노드처럼 게임 규칙상 목표에 실패한 상태입니다. `ExecutionError`는 잘못된 그래프나 Attribute 메서드 예외로 실행이 중단된 상태입니다. 따라서 실행 오류가 `Failed`를 기다리는 Quest 경로를 충족시키지 않습니다. 게임 UI도 두 상태를 구분해 표시할 수 있습니다.

여러 Quest는 기본적으로 동시에 진행할 수 있습니다. 동시에 진행하면 안 되는 조합은 각 Offer 앞에서 다른 Quest의 상태를 검사하도록 기획자가 그래프로 정합니다. 따라서 단일 진행, 다중 진행, 선행 Quest, 일시적 상호 배제, 반복 Quest 정책을 특정 게임 규칙으로 코드에 고정하지 않습니다. `QuestOffer.DialogueEntryPoint`는 선택 전후에 게임 UI가 재생할 수 있는 선택적 시작점이며, Quest 시작 API가 대화를 강제로 재생하지는 않습니다.

저장 시스템은 Dictionary 기반 목표 수치를 포함한 모든 Runtime 컬렉션을 저장하고 복원할 수 있습니다.

```csharp
string json = QuestSaveData.Capture(controller).ToJson();
if (QuestSaveData.TryFromJson(json, out QuestSaveData save, out string error)
    && save.TryApplyTo(controller, replaceExisting: true, out error))
{
    // 인벤토리 등 다른 게임 데이터도 모두 복원한 뒤 호출합니다.
    QuestRunner.ResumeRestoredQuests(controller);
}
```

`TryApplyTo`는 모든 기록을 검증한 뒤 데이터만 교체하거나 병합하며, Action 실행이나 변경 알림을 보내지 않습니다. `replaceExisting: false`이면 기존 Quest는 유지하고 저장 파일에 포함된 Quest 기록만 덮어씁니다. 인벤토리 등 게임 데이터 복원이 끝나면 `ResumeRestoredQuests`를 호출합니다. 이때 복원된 활성 Quest의 상태를 알린 뒤, 다른 Quest의 상태를 기다리던 노드를 다시 평가하여 흐름을 재개합니다. 재개한 경로의 Action과 변경 알림도 실행될 수 있으므로 호출 순서를 지켜야 합니다.

저장은 `QuestRunner`의 공개 진행 API가 반환하여 노드 실행이 끝난 뒤 `Capture`로 수행합니다. Action 내부에서는 저장 요청 플래그만 남기고 실제 저장은 게임 코드에서 나중에 실행합니다. 실행 중인 노드 대기열까지 저장하는 기능은 제공하지 않습니다.

Quest 저장 형식도 현재를 최초 스키마 1로 사용합니다. 이전 테스트 세이브는 폐기하며, 현재 형식과 다른 버전은 진행 상태를 바꾸지 않고 거부합니다. 구형 변환 규칙이 필요 없어 `QuestSaveMigrator`는 제거했습니다. 각 Quest 기록은 저장 당시 그래프 정의 스키마도 보관하며, 향후 실제 저장 형식을 변경할 때 필요한 변환을 추가합니다.

그래프 에셋은 현재 형식을 최초 스키마 1로 시작합니다. 기준 초기화 이전의 테스트 그래프는 다시 생성해야 합니다. 아직 그래프 변환 규칙은 없으며, 다음 형식 변경부터 `GraphAssetMigrator.Migrate`에 `1 -> 2`, `2 -> 3` 순서로 추가합니다. Quest 진행 저장 파일의 스키마는 이와 별개입니다.

## 작성 단계 검증

그래프 창은 불러오기, 필드 수정, 구조 변경과 Undo/Redo 후에 자동으로 검증합니다. 도구 모음에는 오류와 경고 수가 표시되며 `다음 문제`는 문제가 있는 노드를 선택해 화면에 보여줍니다. 해당 노드의 Inspector 위에는 진단 메시지가 표시됩니다.

공통 검증은 누락된 직렬화 타입, 잘못된 GUID, 끊어진 연결, 중복 연결과 누락된 입력 포트를 검사합니다. Dialogue 규칙은 시작점, 메서드 바인딩과 인수, 필수 출력, 도달할 수 없는 노드, 즉시 실행 순환을 추가로 검사합니다. Quest 규칙은 진행 및 대화 경로, Quest 참조, Dialogue 시작점, 필수 출력, 도달할 수 없는 노드, 프로젝트 내 중복 Quest ID와 단방향 순환을 검사합니다.

에디터는 자주 발생하는 실수를 직접 방지합니다. Dialogue 단일 흐름 출력은 연결 하나만 허용하고, Dialogue 시작점 이름은 중복되지 않게 만들며, Quest Start는 하나만 생성할 수 있습니다. Quest와 Dialogue 참조는 프로젝트 에셋 기반 선택기를 사용하고 AND Gate 입력 수는 연결 상태에서 계산합니다. 도구 모음은 노드 내용을 검색하며 참조 Inspector에서 대상 그래프를 열 수 있습니다. 복사·붙여넣기는 새로운 노드 GUID를 만들면서 선택 영역 내부 연결을 복원합니다.

`GraphBuildValidator`는 스크립트 재로드 후 에셋 임포트가 끝나면 전체 그래프를 자동 검사하고, 빌드 직전에 다시 검사합니다. 사용하지 않는 그래프와 같은 파일의 하위 그래프도 포함합니다. Error가 하나라도 있으면 빌드를 중단하고 Warning만 있으면 허용합니다. 자동 검사는 에셋을 수정하거나 저장하지 않습니다. 구버전 그래프는 에디터에서 열어 업그레이드한 뒤 다시 검사해야 합니다. 수동 전체 검사·일괄 마이그레이션 메뉴는 제공하지 않습니다.

같은 자동 검사에서 `GraphMethodBuildValidator`가 플레이어에 포함되는 Dialogue·Quest Attribute 메서드의 선언과 중복 키도 검사합니다. Mono·IL2CPP 빌드 모두 오류가 있으면 빌드 전에 중단합니다. 이는 C# 컴파일러 오류가 아니라 도구의 작성 단계 검사입니다. 에디터에 없는 `#if !UNITY_EDITOR` 등 플랫폼 전용 선언은 이 검사로 확인할 수 없으므로 대상 플레이어에서 검증해야 합니다.

네 Dialogue·Quest Attribute의 `PreserveAttribute` 상속은 Reflection으로 호출하는 게임 메서드가 제거되지 않도록 유지합니다. `link.xml`은 공통 Runtime 타입과 메타데이터를 보존합니다. 다른 코드에서 전혀 참조하지 않는 별도 어셈블리에 메서드만 넣었다면 해당 어셈블리가 빌드와 링커 처리에 포함되도록 `AlwaysLinkAssembly` 또는 게임의 `link.xml` 설정도 확인해야 합니다. 이 설정들은 대상 플랫폼의 실제 호출 테스트를 대신하지 않습니다.

## 현재 한계

- Dialogue는 한 번에 하나의 로컬 대화만 실행합니다. 현 단계에서는 다국어 테이블, 음성·오디오 타이밍, 리치 텍스트 명령, 대화 저장·재개, 네트워크 복제를 제공하지 않습니다.
- Dialogue 신호는 현재 프로세스 안에서 문자열만 전달하며 Payload, 발신자, 대화 범위가 없습니다.
- 임의의 직렬화 가능한 POCO 인수는 자동으로 그리거나 변환하지 않습니다. 지원 범위를 넓히려면 명시적인 Codec과 입력 필드를 추가해야 합니다.
- Quest 수락 목록, 비활성 사유와 선택 UI는 제공 데이터와 API만 정의하며 화면 디자인과 입력 방식은 게임이 구현합니다. 특정 게임의 동시 진행 제한은 고정 정책 대신 Offer 앞의 조건 그래프로 작성합니다.
- `CanComplete` 이후 제출 시점에 그래프를 다시 열어 Reward를 실행하는 전용 Turn-In API는 아직 없습니다. 현재는 Reward를 `CanComplete` 전에 실행하거나, 게임 코드·Dialogue Action에서 보상 지급과 `TurnedIn` 변경을 함께 처리해야 합니다.
- 진행 중인 Dialogue의 저장·재개는 의도적으로 현재 범위에서 제외했습니다. 게임 체크포인트에서 저장하고 긴 대화는 게임의 Skip·기록 정책을 사용합니다. 지속적인 Quest 진행은 `QuestSaveData`가 저장합니다.
- 프로젝트 전체 검증 메뉴는 있지만 실패 코드로 빌드를 종료하는 Headless CI 진입점은 없습니다.
- 에디터는 `UnityEditor.Experimental.GraphView`를 사용하므로 상용 패키지의 장기 유지보수 위험이 있습니다.
- 배포 전 지원할 Unity·플랫폼 조합마다 내보낸 패키지의 IL2CPP 플레이어 Smoke Test가 필요합니다. 빌드 전 검증기는 잘못된 메서드 선언과 중복 키를 찾지만 코드 보존과 플랫폼 QA를 대신할 수 없습니다.

## Reflection 단일 경로 검증 (2026-09-06)

- Unity 6000.3.9f1의 별도 최소 프로젝트에 DIY_Graph만 복사하여 EditMode 테스트 102개를 모두 통과했습니다.
- Windows x64 IL2CPP + Managed Stripping Level High로 실제 플레이어를 빌드하고 실행하여 호출 검사를 통과했습니다.
- private static/instance, Speaker/Interactor/Quest Controller, 지원 인수 6종, 중간 위치 Context 주입, Condition false, JSON 기본형 인수 복원을 확인했습니다.
- 실행기가 타입을 직접 참조하지 않는 별도 asmdef의 private Global 메서드도 Dialogue/Quest Attribute만으로 보존·호출됐습니다.
- 이는 위 Unity·Windows 조합의 검증 결과이며 모바일·WebGL·콘솔이나 실제 게임 전체의 회귀 검증을 대신하지 않습니다.

## 현재 완성도

공통 그래프 에디터, Dialogue Runtime, 로컬 Quest Runtime은 실제로 사용할 수 있는 Alpha 기반입니다. 두 도메인의 타입 기반 Attribute 호출, 조건부 선택지, 그래프 기반 Quest Offer와 수락 직전 재검증, 독립적인 다중 Quest 진행, 포기·재시작, 이식 가능한 대화 경로, 그래프·저장 스키마 검사와 Quest 진행 복원, 범용 보상 Action, 실시간 검증, 빌드 전 메서드 검사와 핵심 EditMode 테스트를 제공합니다. 실제 배포 단계로 가려면 깨끗한 프로젝트에서의 장시간 연동 테스트, 플랫폼별 플레이어 Smoke Build와 다국어 및 미래 Dialogue 진행 저장 정책 결정이 추가로 필요합니다.
