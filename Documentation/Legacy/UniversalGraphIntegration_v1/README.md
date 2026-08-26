# UniversalGraph v1 게임 연결 보관본

이 폴더는 UniversalGraph를 현재 캡스톤 게임에 처음 연결했던 코드를 포트폴리오 기록으로 보관합니다.
현재 게임과 도구를 독립적으로 유지하기 위해 Unity의 `Assets` 밖에 보관하며, 게임 컴파일에는 포함되지 않습니다.

## 당시 연결 흐름

```text
TitleScreenManager
  └─ DialogueManager.Init / QuestManager.Init

NPC.Interact
  └─ DialogueManager.StartConversation

Player
  ├─ PlayerQuestController 생성
  ├─ Dialogue 시작·종료에 맞춰 ConversationState 전환
  └─ Quest 보상을 Stats와 Inventory에 전달

DialogueUI
  └─ DialogueManager의 대사·선택지 이벤트 표시

QuestUI
  └─ PlayerQuestController의 상태 변경 이벤트 표시

QuestAPI
  └─ QuestProgress를 서버 JSON과 변환
```

## 보관된 원본 역할

- `Source/DialogueUI.cs`: 현재 게임 UI와 Dialogue Runtime 연결
- `Source/ConversationState.cs`: 대화 중 플레이어 조작 제한 및 다음 대사 입력
- `Source/PlayerQuestController.cs`: 현재 게임의 Player를 `IQuestController`로 연결
- `Source/QuestEvaluator.cs`: Player와 NPC를 Quest Dialogue Router 입력으로 변환
- `Source/QuestUI.cs`, `Source/QuestUIItem.cs`: Quest 진행 상태 UI
- `Source/QuestAPI.cs`: 기존 서버 Quest 저장·불러오기
- `Assets/QuestUI.prefab.txt`: 당시 Quest UI 프리팹의 YAML 원본
- `Assets/questData.json`: Resources에서 불러오던 초기 Quest 데이터

이 코드는 과거 구조를 보여주기 위한 스냅샷입니다. 새 게임 연결은 도구 본체를 수정하지 않고 별도의 Bridge 계층에서 다시 작성합니다.
