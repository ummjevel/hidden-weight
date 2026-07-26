# UI 모듈

`HiddenWeight.UI`는 HUD, 파편 텍스트, 씬 페이드, 일시정지 화면, 타이틀 화면을 담당한다. `HiddenWeight.Core`/`HiddenWeight.Player`/`HiddenWeight.Emotions`를 참조하는 의존 그래프의 최상단 모듈이며, Core/World는 UI를 절대 참조하지 않는다 — 대신 `ScreenFader`와 `FragmentLog`가 각각 Core의 정적 훅(`SceneFlow.FadeLoader`, `GameManager.FragmentPresenter`)에 자기 자신을 등록해, Core/World가 UI를 몰라도 UI 효과를 유발할 수 있게 하는 역방향 훅 패턴을 쓴다.

## FragmentLog.cs
- **역할**: 화면 하단 중앙에 파편(스토리 조각) 텍스트를 한 줄, 페이드 인 → 유지 → 페이드 아웃 순서로 띄우는 표시기.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`)에 의존. TMP 패키지 추가를 피하기 위해 `TextMeshProUGUI`가 아니라 uGUI `Text`를 사용.
- **주요 멤버**:
  - `static FragmentLog Instance { get; private set; }` — `DontDestroyOnLoad` 싱글턴 접근점.
  - `void Show(string text, float seconds = 4f)` — 표시 요청 진입점. 연달아 호출되면 진행 중인 코루틴을 `StopCoroutine`으로 즉시 끊고 새 텍스트로 교체(대기열 없음).
  - `IEnumerator ShowRoutine(string text, float seconds)` — 텍스트 대입 → `Fade(1f, 0.3f)`(페이드 인 0.3초) → `WaitForSecondsRealtime(seconds)`(기본 4초 유지) → `Fade(0f, 0.5f)`(페이드 아웃 0.5초).
  - `IEnumerator Fade(float target, float duration)` — `Time.unscaledDeltaTime` 누적으로 텍스트 알파를 `Mathf.Lerp` 보간(일시정지 중에도 진행).
- **동작**: `Awake()`에서 싱글턴 중복이면 `Destroy(gameObject)`, 아니면 `Instance = this` + `DontDestroyOnLoad(gameObject)` + `BuildHierarchy()`(코드로 `Canvas`(`sortingOrder = 500`) + 화면 하단 중앙 `Text` 생성) 실행 후, 마지막 줄에서 `GameManager.FragmentPresenter = s => Show(s);`로 자신을 Core의 정적 훅에 등록한다. World의 `StoryFragment.cs`가 파편을 주울 때 이 훅을 `?.Invoke(text)`로 호출하면 `FragmentLog`가 이를 받아 화면에 띄운다 — World → UI 직접 참조 없이 위임이 이루어진다.

## HUD.cs
- **역할**: 플레이 중 화면 상단에 HP(하트 3개)·현재 감정 스킬(쿨타임 포함)·되감기 채널링 게이지·수집 파편 수를 표시하는 오버레이. `GameManager.State`가 `Playing`이 아니면 캔버스 전체를 숨긴다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`, `GameState`), `HiddenWeight.Player`(`PlayerController`, `PlayerHealth`), `HiddenWeight.Emotions`(`EmotionSkillController`, `RewindSkill`)에 의존.
- **주요 멤버**:
  - `const int HeartCount = 3` — 하트 아이콘 개수.
  - `PlayerHealth _health` — `Update`에서 지연 바인딩(플레이어가 늦게 스폰될 수 있으므로).
  - `void HandleHealthChanged(int current, int max)` — 하트 `Image[i].enabled = i < current`로 갱신.
  - `void HandleStateChanged(GameState next)` → `ApplyVisibility` — 캔버스 표시/숨김.
  - `void UpdateSkillDisplay()` / `void UpdateFragmentCount()` — 매 프레임 폴링 갱신.
- **동작**: `Awake`에서 `BuildHierarchy()`(하트/스킬 그룹/파편 텍스트를 코드로 생성, `sortingOrder = 100`). `OnEnable`/`OnDisable`에서 `GameManager.Instance.StateChanged += HandleStateChanged`(및 해제) 구독, `_health`가 있으면 `HealthChanged` 구독도 해제. `Start`에서 현재 `GameManager.Instance.State`(없으면 `GameState.Boot`)로 최초 가시성을 맞춘다. `Update`에서 `_health == null`이면 `TryBindPlayerHealth()`(`PlayerController.Instance.GetComponent<PlayerHealth>()`를 찾아 `HealthChanged` 구독 + 초기값 동기화)를 재시도하고, 매 프레임 `UpdateSkillDisplay()`(`EmotionSkillController.Instance.Active`가 없으면 스킬 그룹·되감기 게이지 모두 숨김, 있으면 `"{Data.displayName} ({CooldownRemaining:F1})"` 표시, `Active`가 `RewindSkill`이고 `IsActive`면 게이트 게이지 `fillAmount = ChannelProgress`)와 `UpdateFragmentCount()`(`GameManager.Instance.Progress.FragmentCount`)를 호출한다. 이벤트 구독(HP/상태 전이)과 폴링(스킬/파편 수)이 혼용된 구조다.

## PauseMenu.cs
- **역할**: `PlayerInput.PausePressed`로 열고 닫히는 일시정지 화면. 열릴 때 게임을 멈추고, 닫힐 때 되돌린다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`, `GameState`, `SceneFlow`), `HiddenWeight.Player`(`PlayerInput`)에 의존.
- **주요 멤버**:
  - `void Open()` — `_root.SetActive(true)` + `PlayerInput.Enabled = false` + `GameManager.Instance.SetState(GameState.Paused)`.
  - `void Close()` — 반대로 `_root.SetActive(false)` + `PlayerInput.Enabled = true` + `GameManager.Instance.SetState(GameState.Playing)`.
  - `void GoToTitle()` — `Progress.ResetAll()` + `GameManager.Instance.SetState(GameState.Title)` + `SceneFlow.LoadWithFade(SceneFlow.Title)`.
- **동작**: `Awake`에서 `BuildHierarchy()`(반투명 배경 패널, "일시정지" 타이틀, "계속하기"/"타이틀로" 버튼을 코드로 생성, `sortingOrder = 800`, 기본 비활성). `Start`에서 `GameManager.Instance.State == GameState.Paused`인 경우를 방어적으로 반영. `Update`에서 매 프레임 `PlayerInput.PausePressed`를 확인 — 이 값은 `PlayerInput.Enabled` 게이트를 의도적으로 우회하는 입력이므로(설계상 일시정지 자체는 `Enabled = false`여도 눌러야 함) `gm.State == Playing`이면 `Open()`, `Paused`면 `Close()`를 호출한다. UI가 직접 `PlayerInput.Enabled`를 껐다 켜는 유일한 스크립트다.

## ScreenFader.cs
- **역할**: 씬 전환 시 화면을 검게 덮었다가 걷어내는 페이드 연출. `Core.SceneFlow`가 씬 전환을 요청할 때 실제로 화면을 그리는 쪽.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`SceneFlow`)에 의존. `UnityEngine.SceneManagement.SceneManager`를 직접 호출.
- **주요 멤버**:
  - `static ScreenFader Instance { get; private set; }` — `DontDestroyOnLoad` 싱글턴.
  - `IEnumerator FadeTo(float alpha, float seconds)` — `Time.unscaledDeltaTime` 기준 알파 보간(일시정지 중에도 진행). `seconds <= 0f`면 즉시 스냅.
  - `void FadeAndLoad(string sceneName, float seconds = 0.5f)` — 진행 중인 페이드 코루틴을 끊고 `FadeAndLoadRoutine` 시작.
  - `IEnumerator FadeAndLoadRoutine(string sceneName, float seconds)` — `FadeTo(1f, seconds)` → `SceneManager.LoadScene(sceneName)` → `FadeTo(0f, seconds)`.
- **동작**: `Awake()`에서 싱글턴 중복이면 파괴, 아니면 `Instance = this` + `DontDestroyOnLoad` + `BuildHierarchy()`(전체 화면을 덮는 검은 `Image`, 알파 0, `raycastTarget = false`, `Canvas.sortingOrder = 999`를 코드로 생성) 실행 후, 마지막 줄에서 `SceneFlow.FadeLoader = FadeAndLoad;`로 자신의 인스턴스 메서드를 Core의 정적 훅(`Action<string, float>`)에 등록한다. Core의 `SceneFlow.LoadWithFade(sceneName, fadeSeconds = 0.5f)`는 `FadeLoader != null`이면 이 메서드로 위임하고, 씬에 `ScreenFader`가 없어 훅이 비어 있으면 페이드 없이 즉시 `SceneManager.LoadScene`으로 폴백한다.

## TitleScreen.cs
- **역할**: 게임 시작 지점인 타이틀 화면. 제목/부제 텍스트와 "시작하기"/"종료" 버튼을 표시한다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`, `GameState`, `SceneFlow`)에 의존.
- **주요 멤버**:
  - `void StartGame()` — `GameManager.Instance.Progress.ResetAll()` + `SetState(GameState.Playing)` + `SceneFlow.LoadWithFade(SceneFlow.Prologue)`.
  - `void Quit()` — `Application.Quit()`.
- **동작**: `Awake`에서 `BuildHierarchy()`(코드로 `Canvas`(`sortingOrder = 10`) 위에 제목 "Hidden Weight"(56pt), 부제 "눈뜨는 꿈"(24pt), 버튼 "시작하기"→`StartGame`/"종료"→`Quit` 생성). `Start`에서 `GameManager.Instance`가 있으면 `SetState(GameState.Title)`을 명시적으로 호출한다(콜드 부트 등으로 상태가 아직 반영되지 않았을 경우를 대비한 방어 코드 — 타이틀 씬에 있다는 사실 자체가 곧 `Title` 상태라는 전제). "시작하기"를 누르면 진행도를 초기화하고 `GameState.Playing`으로 전이한 뒤 `SceneFlow.LoadWithFade(SceneFlow.Prologue)`로 첫 지역(`Zone_Prologue`)에 페이드 전환한다.
