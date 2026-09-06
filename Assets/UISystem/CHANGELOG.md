# 변경 이력

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따르고,
버전은 [유의적 버전](https://semver.org/lang/ko/)을 지킨다.

## [Unreleased]

아직 없음. 다음에 손댈 순서는 [README 의 "아직 없는 것"](../../README.md#아직-없는-것) 을 따른다.

## [0.1.0] - 2026-09-07

UI 스택 시스템의 기초. 스택 관리, 레이어 분리, 정렬 순서 배정, 씬 전환에 걸친 수명 관리의 뼈대를 세웠다.

### 추가

**스택 관리**
- `UIManager` — 열린 UI 를 (레이어, 정렬 순서) 오름차순 단일 스택으로 관리한다.
  열기/닫기, 가림 상태 재계산, 타입별 풀링, 뒤로가기 라우팅을 담당한다.
- `UIBase` — 스택에 참여하는 모든 UI 의 베이스. `WaitForCloseAsync()` 로 닫힘 결과를 기다린다.
- `UIViewOptions` — 인스턴스마다 갈리는 값은 `BlockClose`, `Pooled` 둘뿐이다.
- `UICloseReason` — `Confirmed` / `Cancelled` / `Dismissed` / `ClosedByService`.

**레이어와 뷰 타입**
- 뷰 타입이 자기 레이어를 `sealed override` 로 고정한다.
  `UIScreen` / `UIWindow` / `UIPopup` / `UIOverlay` / `UIToast`.
- `UIElement` — 스택에 참여하지 않는 부품용 베이스.
- `UILayerSettings` — 레이어 이름, `BaseSortingOrder`, 용량, 캔버스 생성 여부와 공통 스케일 설정.
- `UIScreen` 은 `UIRoot` 로 옮기지 않고 씬에 놓인 채로 추적만 한다.
  루트 캔버스인 채로 남아 씬에서 저작한 모습이 그대로 실행된다.

**정렬 순서**
- `SortingOrderAllocator` — 레이어별 커서로 연속 구간을 예약하고, 반납된 구간은
  커서에 닿는 순간 연쇄적으로 회수한다. 용량 초과 시 열기를 거부한다.
- `UseDim` 인 뷰는 자기 캔버스 바로 아래 한 칸을 Dim 자리로 예약한다.

**입력 차단**
- `UIDim` — 앱에 하나뿐인 공유 반투명 판. 보이는 것 중 최상단 모달 자리로 옮겨 다닌다.
  인스턴스가 하나라 반투명이 겹쳐 짙어지지 않는다. 눌러서 닫는 통로도 겸한다.

**씬 수명 관리**
- `sceneLoaded` — 그 씬에 배치된 `UIBase` 를 저작 순서대로 자동 편입한다(`Adopt`).
  씬마다 등록용 컴포넌트를 붙일 필요가 없다.
- `sceneUnloaded` — 그 씬이 소유하던 뷰를 스택에서 빼고 정렬 구간을 반납한다.
  이미 파괴된 참조도 함께 걷어낸다.
- 뷰 출신에 따라 처분이 갈린다. 씬 소유 뷰는 비활성화, 프리팹 뷰는 풀 또는 파괴.

**부팅**
- `UIBootstrap` — `BeforeSceneLoad` 훅에서 영속 영역을 세운다.
  부트스트랩 씬을 강제하지 않아 작업하던 씬에서 그대로 Play 할 수 있다.
  `SubsystemRegistration` 훅으로 도메인 리로드를 꺼도 안전하다.
- `UIRootProvider` — 레이어 캔버스를 실행 시 생성하고 `CanvasScaler` 를 한 곳에서 통일한다.
- 접근점은 `UIManager.Instance` 하나. `UIManager` 의 정적 멤버도 이것뿐이라
  나머지는 전부 생성자 주입이고, DI 컨테이너에 직접 등록해도 된다.

**교체 지점**
- `IUIPrefabProvider` — 프리팹 공급원. 기본 구현은 타입과 프리팹을 직접 참조로 묶는 `UIPrefabTable`.
  Addressables 나 번들로 갈아탈 때 이 구현만 바꾸면 된다.
- `IUIRootProvider` — 레이어 루트 공급원.
- `PlayOpenAsync` / `PlayCloseAsync` — 뷰별 연출. `null` 을 반환하면 즉시 진행한다.

### 알려진 제한

- 테스트가 없다.
- 규약(§규약)을 검사하는 무결성 툴이 없어 어겨도 실행해 봐야 안다.
- `UIToast` 에 자동 소멸이 없다. 호출부가 직접 `Close` 해야 한다.
- `PlayOpenAsync` 도중 취소되면 예외는 나가지만 뷰는 스택에 남는다.

[Unreleased]: https://github.com/Frenil-client/unity-ui-system/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Frenil-client/unity-ui-system/releases/tag/v0.1.0
