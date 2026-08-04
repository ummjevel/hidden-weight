# UI 모듈 — HUD·페이드·파편 텍스트·타이틀/일시정지 화면

> 기획서에 UI 전용 절이 명시적으로 없어 "HUD·연출 UI 전반"(체력·감정 스킬·되감기 게이지·파편 로그, 씬 페이드, 타이틀/일시정지 화면)으로 서술한다.
> UI는 의존 그래프의 최상단이다 — `HiddenWeight.Core`, `HiddenWeight.Player`, `HiddenWeight.Emotions`를 직접 참조하지만, Core/World/Player/Emotions는 UI를 절대 참조하지 않는다. 대신 Core가 미리 파놓은 정적 훅(`SceneFlow.FadeLoader`, `GameManager.FragmentPresenter`)에 UI가 자기 자신을 등록해 역방향으로 연결된다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| FragmentLog.cs | 화면 하단 중앙에 파편 텍스트를 페이드 인 → 유지 → 페이드 아웃으로 표시. `GameManager.FragmentPresenter`에 자신을 등록 | HUD·연출 UI 전반 |
| HUD.cs | 플레이 중 상단 HP(하트)·감정 스킬 쿨타임·되감기 채널링 게이지·파편 수를 표시, `GameState.Playing`이 아니면 캔버스를 숨김 | HUD·연출 UI 전반 |
| PauseMenu.cs | `PlayerInput.PausePressed`로 토글되는 일시정지 화면, `GameManager.SetState`/`PlayerInput.Enabled` 제어 | HUD·연출 UI 전반 |
| ScreenFader.cs | 씬 전환 시 화면을 검게 덮는 페이드. `SceneFlow.FadeLoader`에 자신을 등록 | 4장 씬 구성 |
| TitleScreen.cs | 타이틀 화면(이어하기/새 게임/설정/제작진), `SaveService`와 연동 | 4장 씬 구성 |
| PauseSectionPanel.cs | 스크롤 가능한 지역 노드 지도·기억 카드·조작·접근성 설정 | UI_UX_DESIGN |
| StatusEmblem.cs | 지역 의미에 맞는 능력·노출·위험·진행 문양 | UI_UX_DESIGN |
| TutorialHint.cs | 지역 스킬 획득 지점용 월드 공간 `TextMesh` 안내 — 플레이어가 가까이 오면 페이드 인 | UI_UX_DESIGN 7장 |
| PrologueActionHint.cs | 프롤로그 T01~T03 전용 맥락형 안내. 이동·점프·벽점프·대시·공격을 실제 수행하면 해당 안내를 영구히 숨기고 키 리맵·게임패드 표기를 반영 | LEVEL_10_DREAM_TUTORIAL 1·4·5장 |
| PrologueConceptHint.cs | 프롤로그 T01~T04 전용 세계·목표 안내. 안전 구간 진입 시 한 번만 페이드 인하고 이동이나 확인 입력을 막지 않음 | LEVEL_10_DREAM_TUTORIAL 0·1·4장 |

## 핵심 규칙 구현

- **FragmentLog**: 메시지를 순서대로 대기열에 쌓아 페이드 인·유지·페이드 아웃한다. uGUI `Text`이며 `Time.unscaledDeltaTime` 기준이라 일시정지 중에도 흐름이 유지된다.
- **HUD**: 하트 3개(`HeartCount = 3`, `PlayerHealth.HealthChanged` 이벤트로 갱신), 감정 스킬 그룹은 `EmotionSkillController.Instance.Active`가 없으면 숨김, 있으면 쿨다운 잔여 시간을 `"{표시이름} ({쿨다운:F1})"`으로 표시. `Active`가 `RewindSkill`이고 `IsActive`(채널링 중)일 때만 되감기 게이지를 보여주고 `fillAmount = ChannelProgress`. 파편 수는 `GameManager.Instance.Progress.FragmentCount`를 매 프레임 폴링해 표시. 캔버스 자체는 `GameManager.State == GameState.Playing`일 때만 활성화(`StateChanged` 이벤트 구독 + `Start`에서 최초 1회 동기화).
- **PauseMenu**: 열릴 때 입력과 시간을 멈추고 닫을 때 복원한다. 타이틀 복귀 전 `SaveService`에 진행을 저장하며, 보조 화면은 설정 변경 뒤에도 선택 포커스를 복구한다.
- **ScreenFader**: 전체 화면 검은 `Image`(`raycastTarget = false`)의 알파를 `Time.unscaledDeltaTime` 기준으로 보간. `FadeAndLoad(sceneName, seconds = 0.5f)` 기본 페이드 시간은 0.5초 — 페이드인 → `SceneManager.LoadScene` → 페이드아웃 순서.
- **TitleScreen**: 저장이 있으면 `이어하기`에 기본 포커스를 주고, 새 게임은 기존 기억을 교체하기 전에 확인한다. 손상된 주 파일은 백업으로 복구한다.
- 5개 파일 모두 UI 계층을 인스펙터 프리팹이 아니라 `Awake()`/`BuildHierarchy()`에서 코드로 직접 생성한다(우GUI `Canvas`/`Image`/`Text`/`Button`, `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`) — TMP 패키지 의존을 피하기 위함.

## 씬 배치

- **ScreenFader**: `Instance` 싱글턴 + `DontDestroyOnLoad` 패턴. 훅 등록이 씬 전환과 무관하게 항상 유효해야 하므로 `Bootstrap` 씬에 한 번만 배치한다(다른 씬에 실수로 또 두어도 `Awake`의 중복 파괴 로직이 안전하게 무시한다).
- **FragmentLog**: `HUD` 프리팹의 일부로 각 지역(`Zone_*`) 씬에 씬 루트 오브젝트로 배치되며, `DontDestroyOnLoad`를 쓰지 않는 씬 단위 싱글턴이다(이전에는 Zone 루트의 자식이라 DontDestroyOnLoad가 씬 로드마다 에러를 냈다 — 지금은 애초에 호출하지 않는다). 지역 씬마다 새로 생성/등록되므로 Bootstrap 배치가 필요 없다.
- **HUD / PauseMenu**: 각 지역(`Zone_*`) 씬에 배치. `HUD`는 `GameState.Playing`이 아니면 스스로 캔버스를 숨기므로 Ending 씬에 있어도 무해하지만, 통상은 지역 씬 전용으로 둔다. `PauseMenu`는 `Update`에서 항상 `PlayerInput.PausePressed`를 폴링하므로 지역 씬에 반드시 있어야 일시정지가 동작한다.
- **TitleScreen**: `Title` 씬 전용.
- **Ending 씬**: HUD/PauseMenu를 두지 않는 것이 자연스럽다(플레이 상태가 아니므로 HUD는 어차피 숨겨지고, 일시정지 UX가 필요 없다면 PauseMenu도 생략 가능) — 단, 정확한 배치는 Ending 모듈 설계에 따른다.

## 다른 모듈과의 연결

- **ScreenFader → Core.SceneFlow (역방향 훅)**: `ScreenFader.Awake()`가 `SceneFlow.FadeLoader = FadeAndLoad;`로 자신의 인스턴스 메서드를 등록한다. Core의 `SceneFlow.LoadWithFade(sceneName, fadeSeconds = 0.5f)`는 `FadeLoader != null`이면 위임하고, 없으면(씬에 `ScreenFader`가 없는 경우) 페이드 없이 `SceneManager.LoadScene`으로 즉시 폴백한다 — Core는 UI를 참조하지 않고도 페이드 전환을 "요청"만 할 수 있다.
- **FragmentLog → Core.GameManager (역방향 훅)**: `FragmentLog.Awake()`가 `GameManager.FragmentPresenter = s => Show(s);`로 등록한다. World의 `StoryFragment.cs:34`가 파편을 주울 때 `GameManager.FragmentPresenter?.Invoke(text);`로 호출 — World는 UI를 전혀 모른 채 파편 텍스트를 화면에 띄운다.
- **HUD**: `GameManager.Instance.StateChanged` 이벤트를 구독(`OnEnable`/`OnDisable`)해 캔버스 표시를 갱신하고, `PlayerController.Instance.GetComponent<PlayerHealth>()`를 `Update`에서 폴링해 찾은 뒤 `PlayerHealth.HealthChanged` 이벤트를 구독한다(플레이어가 늦게 스폰되는 경우를 대비한 지연 바인딩). `EmotionSkillController.Instance.Active`/`RewindSkill.IsActive`/`ChannelProgress`는 매 프레임 직접 읽는다(이벤트 구독이 아닌 폴링).
- **PauseMenu**: `PlayerInput.PausePressed`를 매 프레임 폴링(이 값은 `PlayerInput.Enabled` 게이트를 의도적으로 우회하므로 일시정지 중에도 계속 눌림을 감지할 수 있다). `GameManager.Instance.SetState`/`State`/`Progress.ResetAll()`을 직접 호출.
- **TitleScreen**: `GameManager.Instance.SetState`/`Progress.ResetAll()`, `SceneFlow.LoadWithFade`/`SceneFlow.Prologue` 상수를 직접 호출.

## 의존성 주의

- `ScreenFader`와 `FragmentLog`는 각각(씬 안에서) 정확히 인스턴스 하나만 존재해야 훅이 의미가 있다 — 씬에 없으면 `SceneFlow.LoadWithFade`는 페이드 없이 동작(폴백 있음)하지만, `GameManager.FragmentPresenter`가 비어 있으면 `StoryFragment`의 `?.Invoke`가 조용히 아무 일도 하지 않아 파편 텍스트가 아예 표시되지 않는다(예외는 없지만 UX 누락이 발생) — 지역 씬에 `HUD` 프리팹(FragmentLog 포함) 배치를 빠뜨리지 않을 것.
- `HUD`/`PauseMenu`는 `GameManager.Instance`가 null이면 상태 갱신을 건너뛰므로(널 체크 있음) NRE는 나지 않지만, Bootstrap을 거치지 않고 지역 씬을 단독 실행하면 캔버스 표시/일시정지 전환이 정상 동작하지 않는다.
- `HUD.TryBindPlayerHealth`는 `PlayerController.Instance`가 나타날 때까지 매 프레임 재시도한다 — 플레이어 프리팹이 아예 없는 씬(Title 등)에서는 영구히 바인딩되지 않지만 예외 없이 조용히 실패한다.
- 새로 UI를 추가할 때 Core/World/Player/Emotions 쪽 코드를 수정해 UI를 직접 참조하게 만들지 말 것 — 반드시 기존 훅(`FadeLoader`/`FragmentPresenter`)을 재사용하거나, 필요하면 Core에 같은 패턴의 새 정적 훅을 추가하고 UI가 등록하는 방향을 유지한다.
