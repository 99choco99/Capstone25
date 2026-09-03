# UniversalGraph Dialogue & Quest

UniversalGraph는 Dialogue와 Quest Runtime을 제공하는 이식 가능한 Unity 그래프 작성 기반입니다. 그래프 데이터와 실행기는 현재 프로젝트의 `Player`, `NPC`, UI, 인벤토리, 저장 시스템, Addressables 코드에 의존하지 않습니다.

## 처음 코드를 분석한다면

전체 파일을 순서대로 읽지 말고 먼저 [`ARCHITECTURE.md`](ARCHITECTURE.md)를 확인합니다.
공통 Data → Editor 저장·복원 → Dialogue Runtime → Quest Runtime 순서와 처음 읽을 핵심 파일 10개를 정리했습니다.
`Binding`, `Generator`, `Validation`, `Save`, `Tests`는 기본 흐름을 이해한 뒤 필요한 기능만 추적하면 됩니다.

## 어셈블리 경계

- `UniversalGraph.Runtime`: 공통 그래프 데이터, Dialogue·Quest Runtime, Attribute 호출, Source Generator 규약
- `UniversalGraph.Editor`: 공통 GraphView, 노드 등록부, Inspector, Serializer, Undo, 에셋 창
- `UniversalGraph.Dialogue.Editor`: Dialogue 노드 화면과 메서드 인수 입력 필드
- `UniversalGraph.Quest.Editor`: Quest 진행 및 대화 경로 노드 화면
- `UniversalGraph.EditModeTests`: 핵심 Runtime 동작 테스트

Editor 어셈블리는 플레이어 빌드에 포함되지 않습니다. Runtime 어셈블리는 현재 게임의 `Player`, `NPC`, 인벤토리, UI, 네트워크 클래스를 참조하지 않습니다.

## 폴더 구조

```text
DIY_Graph
|-- Data                     공통 그래프 에셋과 연결 데이터
|   `-- Migrations           공통 마이그레이션 등록부와 버전별 공통 단계
|-- Runtime
|   `-- Binding              공통 메서드 설명자, 생성 호출자, 인수와 직렬화 Codec
|-- Editor                   공통 그래프 창, Serializer, Inspector, 검증
|   `-- Styles               공통 USS 디자인과 상태 클래스
|-- Generator                Dialogue·Quest 공통 Roslyn Source Generator 플러그인
|-- 1_Dialogue
|   |-- Data                 Dialogue 컨테이너와 직렬화 데이터
|   |   |-- Nodes            Dialogue 노드 데이터
|   |   `-- Migrations       DialogueContainer 전용 스키마 단계
|   |-- Runtime              Dialogue 재생과 씬 연결 API
|   |   `-- Binding          Dialogue Attribute와 생성 메서드 등록부
|   `-- Editor
|       `-- Nodes            Dialogue 노드 화면
|-- 2_Quest
|   |-- Data                 Quest 정의와 진행 데이터
|   |   |-- Nodes            Quest 노드 데이터
|   |   `-- Migrations       QuestContainer 전용 스키마 단계
|   |-- Runtime              Quest 정의 등록부, Runner, 조회 API
|   |   |-- Binding          Attribute와 생성 메서드 등록부
|   |   `-- Save             저장 DTO와 순차 마이그레이션
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

Roslyn Source Generator는 가능하면 직접 등록·호출 코드를 만들고, 접근할 수 없는 메서드만 검증된 Reflection 경로를 사용합니다. 그래프에 저장되는 인수 ID는 편집 가능한 파라미터 순서로 자동 생성되므로 파라미터 이름은 자유롭게 바꿀 수 있습니다. 단, 기존 그래프가 사용 중일 때 파라미터 순서를 바꾸거나 중간 파라미터를 삭제하면 호출 계약이 달라집니다.

게임 코드는 `DialogueManager.StartConversation`에 `DialogueEntryPoint`와 선택적인 `DialogueExecutionContext`를 전달합니다. 텍스트 전용 또는 전역 메서드만 쓰는 대화는 실행 문맥 없이도 시작할 수 있습니다. UI는 `ShowLine`과 `ShowChoices`를 구독하고 `ContinueDialogue` 또는 `SelectChoice`를 호출합니다.

`DialogueLineNode`는 대사 한 줄을 표시하고, 다음에 연결한 `DialogueChoiceNode`는 선택지 묶음을 표시합니다. UI가 `ShowChoices`에서 기존 대사를 지우지 않으면 마지막 대사를 유지한 채 선택지를 함께 보여줄 수 있습니다.

각 선택지는 `DialogueCondition`을 가질 수 있습니다. Condition은 `DialogueChoiceNode`에 진입할 때 평가하며 false인 선택지는 표시하지 않고 선택 요청도 거부합니다. 표시 가능한 선택지가 하나도 없으면 `Default` 포트로 즉시 진행합니다.

## Quest 연동

게임의 플레이어 및 Quest 소유 구조에 맞춰 `IQuestController`를 한 번 구현합니다. 이후 게임 메서드를 Quest Action, Condition, Reward 노드에 직접 공개할 수 있습니다.

```csharp
[QuestAction("inventory.give-item", Target = QuestMethodTarget.Controller)]
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

게임은 `QuestRunner.AdvanceObjective`로 목표 하나를 직접 진행할 수 있습니다. 처치·수집처럼 타입과 대상 ID로 여러 Quest를 함께 갱신할 때는 `QuestRunner.ReportObjectiveProgress`를 사용합니다. `QuestQueries`는 `IQuestController`와 고정 상호작용 대상 문자열만 사용해 Quest 상태별 대화 경로를 `DialogueCandidate` 또는 `QuestOffer`로 반환합니다.

Quest를 제공할 때는 그래프에 `Interaction Entry → Condition → Quest Offer` 경로를 만듭니다. False 경로를 연결하지 않으면 목록에서 숨길 수 있고, 선택할 수 없는 Offer 노드로 연결하면 이유가 있는 비활성 항목을 표시할 수 있습니다.

```csharp
QuestOffer[] offers = QuestQueries.GetQuestOffers(controller, npcId);
QuestOffer selected = offers.First(offer => offer.IsAvailable);
bool started = QuestRunner.TryStartQuest(controller, selected);
```

`TryStartQuest`는 UI에 표시한 뒤 조건이 바뀐 오래된 Offer를 수락하지 않도록 같은 그래프 경로를 다시 검사합니다. 컷신·튜토리얼·하위 Quest처럼 게임 흐름이 시작을 이미 결정한 경우에만 `ForceStartQuest`를 사용합니다. 게임이 재시작·포기 정책을 결정한 뒤 `ResetQuest`를 호출하면 모든 노드 진행 기록을 지우고 `NotStarted` 상태로 되돌릴 수 있습니다.

`QuestQueries.GetQuestOffers`와 `GetDialogueCandidates`는 후보를 정렬하거나 하나를 선택하지 않습니다. UI 표시 순서, 자동 선택, 추적 Quest 우선 같은 규칙은 `DialogueCandidate.Priority`, `QuestOffer.Priority`, 상태와 프로젝트 데이터를 이용해 게임에서 결정합니다. 현재 목표 UI도 `GetCurrentObjectives`가 반환하는 구조화된 데이터를 원하는 문장과 형식으로 표시합니다.

게임 코드가 Quest 상태만 직접 변경할 때는 `QuestRunner.SetQuestState`를 사용합니다. Reward 노드는 보상 Action만 실행하므로 완료 시점은 State Change 노드나 게임 코드가 결정합니다. `CanComplete`와 `TurnedIn` State Change는 현재 실행을 끝내는 종점이며, `InProgress` State Change만 다음 흐름을 가질 수 있습니다. Wait For Quest 노드는 대상 Quest를 자동 시작하지 않고, 다른 Quest가 Inspector에서 선택한 `RequiredState`에 도달할 때까지 현재 흐름을 기다립니다.

여러 Quest는 기본적으로 동시에 진행할 수 있습니다. 동시에 진행하면 안 되는 조합은 각 Offer 앞에서 다른 Quest의 상태를 검사하도록 기획자가 그래프로 정합니다. 따라서 단일 진행, 다중 진행, 선행 Quest, 일시적 상호 배제, 반복 Quest 정책을 특정 게임 규칙으로 코드에 고정하지 않습니다. `QuestOffer.DialogueEntryPoint`는 선택 전후에 게임 UI가 재생할 수 있는 선택적 시작점이며, Quest 시작 API가 대화를 강제로 재생하지는 않습니다.

저장 시스템은 Dictionary 기반 목표 수치를 포함한 모든 Runtime 컬렉션을 저장하고 복원할 수 있습니다.

```csharp
string json = QuestSaveData.Capture(controller).ToJson();
QuestSaveData.TryFromJson(json, out QuestSaveData save, out string error);
save.TryApplyTo(controller, replaceExisting: true, out error);
```

`QuestSaveData`는 스키마 버전을 가지며 구형 스냅샷을 순차 마이그레이션하고, 전체 데이터 검증 후에만 Controller를 변경합니다. 각 Quest 기록은 저장 당시 그래프 정의 스키마도 보관합니다. 알 수 없는 미래 버전은 현재 진행 상태를 바꾸지 않고 거부합니다.

## 작성 단계 검증

그래프 창은 불러오기, 필드 수정, 구조 변경과 Undo/Redo 후에 자동으로 검증합니다. 도구 모음에는 오류와 경고 수가 표시되며 `다음 문제`는 문제가 있는 노드를 선택해 화면에 보여줍니다. 해당 노드의 Inspector 위에는 진단 메시지가 표시됩니다.

공통 검증은 누락된 직렬화 타입, 잘못된 GUID, 끊어진 연결, 중복 연결과 구형 도착 포트를 검사합니다. Dialogue 규칙은 시작점, 메서드 바인딩과 인수, 필수 출력, 도달할 수 없는 노드, 즉시 실행 순환을 추가로 검사합니다. Quest 규칙은 진행 및 대화 경로, Quest 참조, Dialogue 시작점, 필수 출력, 도달할 수 없는 노드, 프로젝트 내 중복 Quest ID와 단방향 순환을 검사합니다.

에디터는 자주 발생하는 실수를 직접 방지합니다. Dialogue 단일 흐름 출력은 연결 하나만 허용하고, Dialogue 시작점 이름은 중복되지 않게 만들며, Quest Start는 하나만 생성할 수 있습니다. Quest와 Dialogue 참조는 프로젝트 에셋 기반 선택기를 사용하고 AND Gate 입력 수는 연결 상태에서 계산합니다. 도구 모음은 노드 내용을 검색하며 참조 Inspector에서 대상 그래프를 열 수 있습니다. 복사·붙여넣기는 새로운 노드 GUID를 만들면서 선택 영역 내부 연결을 복원합니다.

`Tools/Universal Graph/Validate All Graph Assets`는 수동 배포 검사 전에 프로젝트 전체에서 같은 검증기를 실행합니다. 그래프 에셋은 각자 스키마 버전을 가지며 열거나 불러올 때 업그레이드됩니다. `Tools/Universal Graph/Migrate All Graph Assets`는 업그레이드를 검토 가능한 한 번의 작업으로 저장합니다.

`Tools/Universal Graph/Validate IL2CPP Bindings`는 플레이어에 포함되는 모든 Dialogue·Quest Attribute 메서드에 Source Generator 정보가 있는지 확인합니다. IL2CPP 빌드는 같은 검사를 자동 실행하며 등록이 없거나 오래되었으면 빌드 전에 중단합니다. public/internal 메서드는 생성된 직접 호출을 사용하고, 접근할 수 없는 메서드는 정확한 시그니처를 보존한 Reflection 대체 경로를 사용합니다.

## 현재 한계

- Dialogue는 한 번에 하나의 로컬 대화만 실행합니다. 현 단계에서는 다국어 테이블, 음성·오디오 타이밍, 리치 텍스트 명령, 대화 저장·재개, 네트워크 복제를 제공하지 않습니다.
- Dialogue 신호는 현재 프로세스 안에서 문자열만 전달하며 Payload, 발신자, 대화 범위가 없습니다.
- 임의의 직렬화 가능한 POCO 인수는 자동으로 그리거나 변환하지 않습니다. 지원 범위를 넓히려면 명시적인 Codec과 입력 필드를 추가해야 합니다.
- Quest 수락 목록, 비활성 사유와 선택 UI는 제공 데이터와 API만 정의하며 화면 디자인과 입력 방식은 게임이 구현합니다. 특정 게임의 동시 진행 제한은 고정 정책 대신 Offer 앞의 조건 그래프로 작성합니다.
- `CanComplete` 이후 제출 시점에 그래프를 다시 열어 Reward를 실행하는 전용 Turn-In API는 아직 없습니다. 현재는 Reward를 `CanComplete` 전에 실행하거나, 게임 코드·Dialogue Action에서 보상 지급과 `TurnedIn` 변경을 함께 처리해야 합니다.
- 진행 중인 Dialogue의 저장·재개는 의도적으로 현재 범위에서 제외했습니다. 게임 체크포인트에서 저장하고 긴 대화는 게임의 Skip·기록 정책을 사용합니다. 지속적인 Quest 진행은 `QuestSaveData`가 저장합니다.
- 프로젝트 전체 검증 메뉴는 있지만 실패 코드로 빌드를 종료하는 Headless CI 진입점은 없습니다.
- 에디터는 `UnityEditor.Experimental.GraphView`를 사용하므로 상용 패키지의 장기 유지보수 위험이 있습니다.
- 배포 전 지원할 Unity·플랫폼 조합마다 내보낸 패키지의 IL2CPP 플레이어 Smoke Test가 필요합니다. 빌드 전 검증기는 누락된 생성 바인딩을 찾지만 플랫폼 QA를 대신할 수 없습니다.

## 현재 완성도

공통 그래프 에디터, Dialogue Runtime, 로컬 Quest Runtime은 실제로 사용할 수 있는 Alpha 기반입니다. 두 도메인의 타입 기반 Attribute 호출, 조건부 선택지, 그래프 기반 Quest Offer와 수락 직전 재검증, 독립적인 다중 Quest 진행, 포기·재시작, 이식 가능한 대화 경로, 순차 그래프·저장 마이그레이션, 범용 보상 Action, 실시간 검증, IL2CPP 빌드 전 검사와 핵심 EditMode·Generator 테스트를 제공합니다. 실제 배포 단계로 가려면 깨끗한 프로젝트에서의 장시간 연동 테스트, 플랫폼별 플레이어 Smoke Build와 다국어 및 미래 Dialogue 진행 저장 정책 결정이 추가로 필요합니다.
