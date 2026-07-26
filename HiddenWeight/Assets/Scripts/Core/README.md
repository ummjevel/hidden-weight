# Core 모듈 — 게임 전역 상태·씬 흐름·진행도 관리

> 기획서 2.1(레이어/전역), 3.1(의존 방향), 5.6(진행 상태), 5.7(엔딩 훅) 대응.
> 다른 모든 모듈이 참조하는 유일한 공통 기반이며, `Data`를 제외한 어떤 `HiddenWeight.*` 모듈도 참조하지 않는다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `GameManager.cs` | 씬 영속 싱글턴. `ProgressState`/`GameState`/`CurrentZoneData`를 들고 다니며 씬 로드마다 지역을 자동 동기화 | 3.1 의존 방향, 5.6 진행 상태 |
| `GameState.cs` | 게임 전체 흐름 상태 열거형 (`Boot/Title/Playing/Paused/Ending`) | 3장 모듈 구조 |
| `ProgressState.cs` | 스킬 해금·자각·파편·현재 지역·체크포인트를 들고 다니는 순수 C# 진행도 객체 | 5.6 진행 상태 |
| `Checkpoint.cs` | 플레이어 통과 시 리스폰 지점을 갱신 | 5.5 월드(체크포인트) |
| `SceneFlow.cs` | 씬 이름 상수 + 씬 전환 진입점(페이드 훅 포함) | 4장 씬 구성, 3.1 역방향 훅 |
| `AudioManager.cs` | BGM 크로스페이드 재생 + SFX 원샷 재생 싱글턴 | 2장 사운드 에셋 자리만 확보 |

## 핵심 규칙 구현

- `GameManager.Awake()`가 `SceneManager.sceneLoaded`를 구독해 `balance.GetZoneByScene(scene.name)`으로 지역을 찾고 `EnterZone(zone.id)`를 자동 호출한다. 지역 씬은 별도로 `EnterZone`을 호출할 필요가 없다.
- `EnterZone(ZoneId id)`는 `Progress.CurrentZone`과 `CurrentZoneData`만 갱신하고, 스킬 해금(`grantedSkill`)은 지역 안의 픽업(`StoryFragment`)에서 처리한다 — GameManager는 해금 로직을 갖지 않는다.
- `SetState(GameState next)`는 같은 상태로 재진입 시 아무 것도 하지 않고(no-op 가드), `Paused`로 전이할 때 `Time.timeScale`을 0으로, 그 외에는 1로 되돌린다.
- `autoLoadTitle`(기본값 `false`)은 Bootstrap 씬의 `GameManager` 인스턴스에서만 `true`로 오버라이드되어 `Start()`에서 `SceneFlow.Load(SceneFlow.Title)`을 호출한다. 프리팹 기본값 자체는 건드리지 않는다.
- `ProgressState.CanOpenFinalGate()`는 기획서 5.3절의 잔재 백트래킹 최종 파편 조건 그대로, `되감기 보유(EmotionId.Rewind) && HasAwareness && HasClearedFracture` 세 조건을 모두 요구한다. 일반 게이트는 `CanOpenGate(EmotionId required)`로 `required == None`이면 무조건 통과, 아니면 스킬 보유 여부만 본다.
- `Checkpoint`는 `Collider2D`를 요구하며(`[RequireComponent]`), `Player` 레이어와의 `OnTriggerEnter2D` 1회만 반응하고(`_used` 플래그) 이후에는 재사용되지 않는다.
- `AudioManager.PlayBgm`/`StopBgm`은 `fadeSeconds`(기본 1초)를 절반씩 나눠 in/out 크로스페이드하며, `Time.unscaledDeltaTime` 기준이라 `Paused`(timeScale 0) 상태에서도 페이드가 진행된다.

## 씬 배치

- `GameManager`, `AudioManager`는 `Bootstrap` 씬에만 배치하고 `DontDestroyOnLoad`로 씬 전환 후에도 유지한다. 둘 다 `Instance`가 이미 있으면 자신을 파괴하는 중복 방지 로직이 있어, 실수로 다른 씬에 다시 두어도 안전하게 무시된다.
- `Checkpoint`는 각 지역(`Zone_*`) 씬 안, 플레이어 진행 경로 위에 배치한다.
- `SceneFlow`, `GameState`, `ProgressState`는 씬에 배치하는 컴포넌트가 아니라 정적 유틸/데이터 클래스이므로 씬 오브젝트가 없다.

## 다른 모듈과의 연결

Core는 `using HiddenWeight.UI`, `using HiddenWeight.Player`, `using HiddenWeight.World`를 갖지 않는다. 대신 아래 세 가지 정적/인스턴스 훅으로 의존 방향을 뒤집어, Core가 상위 모듈을 몰라도 그 모듈들이 Core의 이벤트에 반응할 수 있게 한다.

- `SceneFlow.FadeLoader` (`Action<string, float>`) — UI의 `ScreenFader`가 등록하고, World의 `ZoneTrigger`와 Core 자신의 `SceneFlow.LoadWithFade(sceneName, fadeSeconds = 0.5f)`가 소비한다. 등록되어 있지 않으면 `LoadWithFade`는 페이드 없이 `SceneManager.LoadScene`으로 폴백한다.
- `GameManager.FragmentPresenter` (`static Action<string>`) — UI의 `FragmentLog`가 등록하고, World의 `StoryFragment`가 파편 텍스트를 화면에 띄울 때 호출한다. World가 UI를 직접 참조하지 않기 위한 훅이다.
- `GameManager.RespawnRequested` (`event Action<Vector3>`) — Player의 `PlayerHealth`가 구독하고, `GameManager.RespawnPlayer()`가 `Progress.LastCheckpoint`를 실어 호출한다. Core가 Player를 직접 참조하지 않기 위한 훅이다.

참고: World가 읽는 `AwarenessRegistry`는 World 모듈 소속이며 Core에는 없다.

## 의존성 주의

- `GameManager.EnterZone`/`HandleSceneLoaded`가 정상 동작하려면 `balance`(`BalanceData`) 필드가 인스펙터에 반드시 할당되어 있어야 한다. 비어 있으면 `HandleSceneLoaded`는 조용히 아무 것도 하지 않는다(zone이 null이면 EnterZone 자체가 호출되지 않음).
- `GameManager.Instance`가 존재하기 전에는(즉 Bootstrap 씬을 거치지 않고 지역 씬을 단독 실행하면) `Checkpoint`, `EmotionSkillController` 등 Core를 참조하는 모든 컴포넌트가 `NullReferenceException`을 낸다.
- Core는 `HiddenWeight.Data`에만 의존하므로, `Data`의 `BalanceData`/`ZoneData`/`EmotionId`/`ZoneId` API가 바뀌면 `GameManager`/`ProgressState`가 영향을 받는다. 반대 방향(Data가 Core를 참조)은 절대 만들지 않는다.
