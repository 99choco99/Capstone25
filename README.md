# Capstone25

Unity 6로 제작한 3D 액션 RPG 캡스톤 프로젝트입니다. 소울라이크 전투의 손맛을 만드는 락온, 패링, 자세, 가드 브레이크, 처형 흐름을 중심으로 구현했고 인벤토리, 퀘스트, 상점, 대화와 서버 통신까지 하나의 플레이 흐름으로 연결했습니다.

## 브랜치

- `main`: 포트폴리오에서 확인할 기준 버전
- `refactor/player`: 최신 플레이어·전투 구조의 비교 기준
- `develop`: FreeCam, LockOn, 처형 카메라를 포함한 최신 작업 버전

`develop`은 현재 작업 내용을 보존한 브랜치입니다. Unity Play Mode와 빌드 확인 전까지는 `main`과 분리해 관리합니다.

## 주요 구현

### 전투

- 상태 클래스로 이동, 공격, 회피, 가드, 피격, 처형 전이를 분리
- 패링과 자세 수치, 가드 브레이크, 전방·후방 처형 구현
- 공격 패턴과 아이템·적 데이터를 ScriptableObject로 관리
- 공통 전투 규칙을 `IDamageable`, `IWeaponOwner`, `LivingEntity`, `Weapon` 구조로 재사용

### 카메라

- 씬마다 직접 연결하던 카메라 참조를 Cinemachine 기반 구조로 변경
- 자유 시점과 락온 시점이 같은 궤도를 사용하도록 구성해 전환 시 위치가 튀는 문제 완화
- 타겟 거리와 화면 구도를 기준으로 락온 피치를 보정
- 처형 시작·종료 이벤트에 맞춰 전용 카메라 우선순위를 전환

최근 카메라 작업은 `develop`의 `SekiroCamera`, `DeathblowCamera`, `CameraTargetBinder`에서 확인할 수 있습니다.

### 적과 콘텐츠

- Unity Behavior 기반 탐지, 추격, 거리 조절, 공격, 패링 판단 노드 구성
- 인벤토리, 장비·소비 아이템, 퀵슬롯, 상점 거래 구현
- NPC 상호작용, 선행 조건이 있는 퀘스트, 대화 UI 구현

### 통신과 초기화

- 로그인, 플레이어 데이터, 인벤토리, 상점, 퀘스트, 대화 API를 기능별로 분리
- REST, Socket.IO, Netcode for GameObjects 연동 구조 구성
- 씬 전환과 플레이어 스폰 시 데이터 준비 순서를 부트스트랩 흐름으로 관리

## 사용 기술

| 구분 | 내용 |
|---|---|
| 엔진 | Unity `6000.3.9f1` |
| 언어 | C# |
| 렌더링 | Universal Render Pipeline, Shader Graph |
| 카메라·입력 | Cinemachine, Input System |
| 적 행동 | Unity Behavior, AI Navigation |
| 네트워크 | Netcode for GameObjects, Unity Transport, Socket.IO |
| 기타 | Timeline, Animation Rigging, UGUI, ScriptableObject |

## 실행 메모

1. Unity Hub에서 프로젝트를 Unity `6000.3.9f1`로 엽니다.
2. `main`은 `Assets/Scripts/Scenes/01_Login.unity`, `develop`은 `Assets/_WORK/Scenes/00_Init.unity`부터 실행합니다.
3. 로그인과 온라인 데이터 기능은 연결된 서버 환경이 필요합니다.

## 현재 상태

- 최신 카메라 작업은 별도 개발 브랜치와 Draft PR로 보존합니다.
- 자동화 테스트와 CI는 아직 없습니다.
- 다음 확인 항목은 FreeCam/LockOn 반복 전환, 처형 카메라 복귀, 씬 이동 후 참조 유지, 플레이어 빌드입니다.

기능이 추가된 순서와 설계 변경 내용은 [개발 기록](docs/DEVELOPMENT_LOG_KO.md)에 정리했습니다.
