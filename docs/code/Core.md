# Core 모듈

`HiddenWeight.Core`는 게임 전역 상태(`GameState`), 씬을 넘나드는 진행도(`ProgressState`), 씬 전환(`SceneFlow`), 체크포인트, 오디오 재생을 담당한다. `GameManager`와 `AudioManager`는 `Bootstrap` 씬에 배치되어 `DontDestroyOnLoad`로 게임 전체 수명 동안 유지되는 싱글턴이며, Core는 `HiddenWeight.Data`만 참조하고 다른 어떤 `HiddenWeight.*` 모듈도 참조하지 않는다.

## AudioManager.cs

- **역할**: BGM 크로스페이드 재생과 SFX 원샷 재생을 담당하는 오디오 싱글턴. MVP 단계에서는 클립 에셋이 비어 있을 수 있으므로 `clip == null`이면 아무 것도 하지 않는다.
- **상속/의존**: `MonoBehaviour`. 인스펙터 필드 없이 `Awake`에서 `AudioSource` 2개(`_bgmSource`, `_sfxSource`)를 `AddComponent`로 직접 생성한다.
- **주요 멤버**:
  - `static AudioManager Instance { get; private set; }` — 전역 접근점.
  - `void PlayBgm(AudioClip clip, float fadeSeconds = 1f)` — 현재 BGM을 페이드아웃한 뒤 새 클립으로 교체하고 페이드인.
  - `void StopBgm(float fadeSeconds = 1f)` — BGM을 페이드아웃 후 정지.
  - `void PlaySfx(AudioClip clip, float volume = 1f)` — `PlayOneShot`으로 1회 재생.
- **동작**:
  - `Awake`에서 `Instance`가 이미 있고 자신이 아니면 `Destroy(gameObject)` 후 리턴(중복 싱글턴 방지), 아니면 `Instance = this` + `DontDestroyOnLoad`.
  - `_bgmSource`는 `loop = true`, `_sfxSource`는 `loop = false`, 둘 다 `playOnAwake = false`.
  - `PlayBgm`은 진행 중이던 페이드 코루틴이 있으면 `StopCoroutine`으로 취소하고 `FadeToClip`을 새로 시작한다. `FadeToClip`은 `fadeSeconds`의 절반 동안 볼륨을 0으로 내린 뒤 클립을 교체·재생하고, 나머지 절반 동안 볼륨을 1로 올린다.
  - `StopBgm`도 동일하게 기존 페이드를 취소하고 `FadeOutAndStop`을 실행 — `fadeSeconds` 전체에 걸쳐 볼륨을 0으로 내린 뒤 `Stop()` + `clip = null`.
  - `FadeVolume`은 `duration <= 0f`이면 즉시 목표 볼륨으로 스냅하고, 아니면 `Time.unscaledDeltaTime` 누적으로 `Mathf.Lerp` 보간한다 — `unscaledDeltaTime`을 쓰므로 `GameState.Paused`(`Time.timeScale = 0`) 중에도 페이드가 계속 진행된다.
  - `PlaySfx`/`PlayBgm` 모두 `clip == null`이면 즉시 리턴하는 안전 가드가 있다.

## Checkpoint.cs

- **역할**: 플레이어가 지나가면 이후 리스폰 지점을 자신의 위치로 갱신하는 통과형 트리거.
- **상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Collider2D))]`. `GameManager.Instance.Progress`에 직접 접근한다.
- **주요 멤버**:
  - `bool _used` (기본값 `false`, private) — 1회성 사용 플래그.
- **동작**:
  - `OnTriggerEnter2D(Collider2D other)`에서 `_used`가 이미 `true`면 즉시 리턴.
  - `other.gameObject.layer`가 `"Player"` 레이어가 아니면 리턴 — `LayerMask.NameToLayer("Player")`와 비교.
  - 조건을 통과하면 `GameManager.Instance.Progress.LastCheckpoint = transform.position`으로 기록하고 `_used = true`로 잠가 이후 재사용을 막는다.

## GameManager.cs

- **역할**: 게임 전역 싱글턴. `ProgressState`, 현재 `GameState`, 현재 지역의 `ZoneData`, 밸런스 데이터(`BalanceData`)를 들고 다니며 씬 로드마다 지역 진입을 자동 처리한다.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.SceneManagement`와 `HiddenWeight.Data`(`BalanceData`, `ZoneData`, `ZoneId`)에 의존. `HiddenWeight.Core.SceneFlow`를 내부에서 호출.
- **주요 멤버**:
  - `static GameManager Instance { get; private set; }` — 전역 접근점.
  - `[SerializeField] BalanceData balance` — 밸런스 데이터 에셋 참조(인스펙터 할당 필수). `public BalanceData Balance => balance`로 읽기 전용 노출.
  - `[SerializeField] bool autoLoadTitle = false` — Bootstrap 씬의 `GameManager` 인스턴스만 `true`로 오버라이드되어 있고, 프리팹 기본값은 `false`.
  - `ProgressState Progress { get; private set; }` — `Awake`에서 `new ProgressState()`로 생성.
  - `GameState State { get; private set; }` — 초기값은 C# 기본값인 `GameState.Boot`(enum 0번).
  - `ZoneData CurrentZoneData { get; private set; }` — 현재 지역 데이터, `EnterZone`에서 갱신.
  - `event System.Action<GameState> StateChanged` — `SetState`가 상태 변경 시 호출.
  - `event System.Action<Vector3> RespawnRequested` — Player 모듈을 직접 참조하지 않기 위한 역방향 훅. `PlayerHealth`가 구독.
  - `static System.Action<string> FragmentPresenter` — World가 UI(`FragmentLog`)를 직접 참조하지 않기 위한 훅. UI가 채운다.
  - `void SetState(GameState next)` — 상태 전이 + `Time.timeScale` 제어 + 이벤트 발행.
  - `void EnterZone(ZoneId id)` — `Progress.CurrentZone`과 `CurrentZoneData` 갱신.
  - `void RespawnPlayer()` — `RespawnRequested?.Invoke(Progress.LastCheckpoint)`.
- **동작**:
  - `Awake`에서 싱글턴 중복 시 `Destroy(gameObject)` 후 리턴, 아니면 `Instance = this` + `DontDestroyOnLoad` + `Progress = new ProgressState()` + `SceneManager.sceneLoaded += HandleSceneLoaded` 구독.
  - `OnDestroy`에서 `Instance == this`일 때만 `sceneLoaded` 구독을 해제한다(파괴되는 중복 인스턴스가 실수로 구독 해제하지 않도록 가드).
  - `Start`에서 `autoLoadTitle`이 `true`면 `SceneFlow.Load(SceneFlow.Title)` 호출(페이드 없는 즉시 로드).
  - `HandleSceneLoaded(Scene scene, LoadSceneMode mode)`는 `balance != null`일 때만 `balance.GetZoneByScene(scene.name)`으로 지역을 찾고, 찾으면 `EnterZone(zone.id)`를 자동 호출한다. `balance`가 null이거나 매칭되는 지역이 없으면(Title/Ending 등) 아무 것도 하지 않는다.
  - `SetState`는 `State == next`이면 즉시 리턴(no-op 가드). 아니면 `State`를 갱신하고, `next == GameState.Paused`일 때만 `Time.timeScale = 0f`, 그 외에는 `1f`로 설정한 뒤 `StateChanged` 이벤트를 발행한다.
  - `EnterZone`은 스킬 해금(`grantedSkill`)을 처리하지 않는다 — 주석에 명시된 대로 지역 안의 픽업(`StoryFragment`)에서 처리하는 책임 분리다.

## GameState.cs

- **역할**: 게임 전체 흐름 상태를 나타내는 열거형.
- **상속/의존**: 없음(순수 enum, `HiddenWeight.Core` 네임스페이스만 사용).
- **주요 멤버**:
  - `enum GameState { Boot, Title, Playing, Paused, Ending }` — 선언 순서대로 `Boot = 0`, `Title = 1`, `Playing = 2`, `Paused = 3`, `Ending = 4`.
- **동작**:
  - 값 자체에 로직은 없다. `GameManager.State`의 타입이며, `GameManager.SetState`가 `Paused` 여부에 따라 `Time.timeScale`을 분기하는 데 쓰인다.

## ProgressState.cs

- **역할**: 지역을 넘나들며 유지되는 진행도(스킬 해금, 자각, 파편 수집, 현재 지역, 체크포인트, 균열 클리어 여부)를 보관하는 순수 C# 클래스. `MonoBehaviour`가 아니라 `GameManager.Progress`로 들려 다닌다.
- **상속/의존**: 없음(순수 C#). `HiddenWeight.Data`의 `EmotionId`, `ZoneId`에 의존.
- **주요 멤버**:
  - `HashSet<EmotionId> _skills` (private, readonly) — 해금된 감정 스킬 집합.
  - `HashSet<string> _fragments` (private, readonly) — 수집한 파편 id 집합.
  - `bool HasAwareness { get; private set; }` — 기본값 `false`.
  - `bool HasClearedFracture { get; private set; }` — 기본값 `false`.
  - `ZoneId CurrentZone { get; set; } = ZoneId.Prologue` — 기본값 `ZoneId.Prologue`.
  - `Vector3 LastCheckpoint { get; set; }` — 기본값 `Vector3.zero`.
  - `int FragmentCount => _fragments.Count` — 읽기 전용 계산 프로퍼티.
  - `void UnlockSkill(EmotionId id)` — `id != EmotionId.None`일 때만 `_skills.Add(id)`.
  - `bool HasSkill(EmotionId id) => _skills.Contains(id)`.
  - `void GrantAwareness() => HasAwareness = true`.
  - `void MarkFractureCleared() => HasClearedFracture = true`.
  - `bool CollectFragment(string id) => _fragments.Add(id)` — `HashSet.Add`의 반환값을 그대로 넘겨 신규 수집 여부(중복이면 `false`)를 알 수 있다.
  - `bool HasFragment(string id) => _fragments.Contains(id)`.
  - `bool CanOpenGate(EmotionId required) => required == EmotionId.None || _skills.Contains(required)` — 요구 스킬이 `None`이면 조건 없이 열린다.
  - `bool CanOpenFinalGate() => _skills.Contains(EmotionId.Rewind) && HasAwareness && HasClearedFracture` — 기획서 5.3절: 균열 클리어 후 자각을 갖춘 채 잔재로 백트래킹해야 열리는 최종 파편.
  - `void ResetAll()` — `_skills`/`_fragments`를 `Clear()`, `HasAwareness`/`HasClearedFracture`를 `false`, `CurrentZone`을 `ZoneId.Prologue`, `LastCheckpoint`를 `Vector3.zero`로 초기화.
- **동작**:
  - 모든 변경 메서드는 단순 위임이며 이벤트 발행이나 검증 로직은 없다(스킬 해금 시 `EmotionId.None` 방어 외에는 무조건 반영).
  - `CanOpenFinalGate`는 세 조건을 모두 `&&`로 요구하는 단일 식이며, 세 조건 중 하나라도 어긋나면 최종 게이트는 열리지 않는다.
  - `ResetAll`은 씬 재시작·타이틀 복귀 시 전체 진행도를 초기화하는 용도로 보이나, 이 파일 안에서 호출하는 곳은 없다(호출자는 다른 모듈).

## SceneFlow.cs

- **역할**: 씬 이름 상수와 씬 전환 진입점을 한곳에 모은 정적 유틸리티.
- **상속/의존**: 없음(정적 클래스). `UnityEngine.SceneManagement.SceneManager`에 의존.
- **주요 멤버**:
  - `const string Bootstrap = "Bootstrap"`, `Title = "Title"`, `Prologue = "Zone_Prologue"`, `Residue = "Zone_Residue"`, `Gaze = "Zone_Gaze"`, `Fracture = "Zone_Fracture"`, `Ending = "Ending"`.
  - `static System.Action<string, float> FadeLoader` — UI(`ScreenFader`)가 채우는 훅. Core는 UI 모듈을 참조하지 않는다.
  - `static void Load(string sceneName) => SceneManager.LoadScene(sceneName)`.
  - `static void LoadWithFade(string sceneName, float fadeSeconds = 0.5f)`.
- **동작**:
  - `Load`는 페이드 없이 즉시 `SceneManager.LoadScene`을 호출한다.
  - `LoadWithFade`는 `FadeLoader`가 등록되어 있으면 `FadeLoader(sceneName, fadeSeconds)`를 호출하고, 등록되어 있지 않으면(즉 UI의 `ScreenFader`가 씬에 없으면) `Load(sceneName)`으로 폴백해 페이드 없이 전환한다.
  - World의 `ZoneTrigger`는 이 `LoadWithFade`를 통해 UI를 직접 참조하지 않고도 페이드 전환을 트리거할 수 있다.
