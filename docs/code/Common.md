# Common 모듈

`HanGame.Common` 네임스페이스의 공통 시스템 계층이다. 게임 전체 흐름(씬 전환·층 진행·회귀), 플레이어의 이동·생명(멘탈)·평판·런타임 스탯, 그리고 사운드 재생 등 낮·밤 어느 씬에서나 공유되는 기반 기능을 담는다. `GameManager`와 `AudioManager`는 `Boot` 씬에 하나만 두고 `DontDestroyOnLoad`로 유지되며, `Player` 계열은 플레이어 프리팹에 붙는다.

## AudioManager.cs

- **역할**: id로 등록한 클립을 이름으로 재생하는 간단한 SFX/BGM 재생기(기획서 15.4).
- **상속/의존**: `MonoBehaviour`. `AudioSource` 2개(sfx/bgm)를 인스펙터로 주입받고, 자신을 static 싱글턴으로 등록.
- **주요 멤버**:
  - `static AudioManager Instance` — 전역 접근점.
  - `struct Clip { string id; AudioClip clip; float volume(0~1); }` — 직렬화되는 클립 정의.
  - `[SerializeField] Clip[] clips`, `AudioSource sfxSource`, `AudioSource bgmSource`.
  - `void PlaySfx(string id)` — 등록된 클립을 `PlayOneShot`으로 1회 재생.
  - `void PlayBgm(string id, bool loop = true)` — bgmSource에 클립 세팅 후 재생.
  - `static class Sfx` — 사운드 id 문자열 상수 모음(`KeyboardHit`, `StaplerFire`, `ApprovalStamp`, `EmailAlert`, `PhoneRing`, `CoffeeHeal`, `BossGazeWarn`, `GuardSpotted`, `ReputationDown`, `Resignation`, `CeoWaveWarn`).
- **동작**:
  - `Awake`에서 싱글턴 중복 제거 후 `DontDestroyOnLoad`, `clips` 배열을 `Dictionary<string, Clip>`로 인덱싱.
  - 재생 시 source가 null이거나 id 미등록이거나 clip이 null이면 무시(안전 no-op).
  - `volume`이 0 이하면 1로 보정해 재생.

## GameManager.cs

- **역할**: 게임 흐름 오케스트레이터. 씬 전환과 층 진행, 낮/밤 결과 처리, 회귀를 담당(기획서 2.1, 3.2, 19.1).
- **상속/의존**: `MonoBehaviour`. `UnityEngine.SceneManagement.SceneManager`로 씬 로드, `RunState`를 생성·보유, `PlayerHealth`가 이 매니저의 `OnFired`를 호출.
- **주요 멤버**:
  - `static GameManager Instance`.
  - 직렬화: `string dayScene = "Day"`, `string nightScene = "Night"`, `int startingReputation = 3`.
  - `RunState Run { get; private set; }` — 현재 런 상태.
  - `GameState State { get; private set; } = Boot`.
  - `event Action<GameState> StateChanged` — 상태 전환 시 UI 등이 구독.
  - `void StartNewRun()` — 첫 출근. `Run.ResetToFirstDay()` 후 평판 초기화, `EnterDay()`.
  - `void EnterDay()` / `void EnterNight()` — Phase·State 설정 후 해당 씬 로드.
  - `void OnDaySurvived()` — 낮 60초 생존. 4층(최종)이면 `Ending`, 아니면 밤 진입.
  - `void OnFired()` — 낮 중 평판 0. `Fired` 상태 + 1층 회귀.
  - `void OnNightCleared(string acquiredWeaponId)` — 무기 획득(미보유 시 추가) → `NightClears++`, `Floor++`, 낮 진입.
  - `void OnNightFailed()` — 발각·시간초과. `Fired` 상태 + 1층 회귀.
  - `void ShowResult()`, `void SetState(GameState next)`, `bool IsPaused => Time.timeScale == 0f`.
- **동작**:
  - `Awake`에서 싱글턴 유지 및 `RunState` 인스턴스 생성(기본 평판 주입).
  - `Update`에서 `Day`/`Night` 상태일 때만 `Run.PlayTime`을 `unscaledDeltaTime`으로 누적(일시정지 무관).
  - 해고/발각 후에는 `Fired` 상태만 설정하고, 실제 낮 재진입(`EnterDay`)은 연출·UI가 트리거하도록 남겨둠.

## GameState.cs

- **역할**: 게임 전체 상태와 층의 낮/밤 단계를 정의하는 enum(기획서 19.1).
- **상속/의존**: 순수 enum 2종. `GameManager`가 흐름 제어에 사용.
- **주요 멤버**:

  `GameState`:

  | 값 | 의미 |
  |---|---|
  | `Boot` | 부팅/타이틀 |
  | `Prologue` | 회귀 프롤로그 연출 |
  | `Day` | 낮 디펜스 |
  | `DayLevelUp` | 레벨업 선택(시간 정지) |
  | `Night` | 밤 잠입 |
  | `Fired` | 해고 연출(회귀) |
  | `Ending` | CEO 취임 엔딩 |
  | `Result` | 결과 화면 |

  `FloorPhase`: `Day`, `Night`.
- **동작**: 값만 정의하며 로직 없음.

## Player.cs

- **역할**: 플레이어 루트 파사드. 하위 컴포넌트 참조를 모으고 자신을 static으로 노출.
- **상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(PlayerController))]`. 같은 오브젝트의 `PlayerController`/`PlayerHealth`/`PlayerStats`를 캐싱.
- **주요 멤버**:
  - `static Player Local` — 적·무기·시스템이 싸게 얻는 전역 참조.
  - `PlayerController Controller`, `PlayerHealth Health`, `PlayerStats Stats` (프로퍼티).
  - `Vector2 Position => transform.position`.
- **동작**:
  - `Awake`에서 `Local = this` 등록 후 세 컴포넌트를 `GetComponent`로 캐싱.
  - `OnDestroy`에서 자신이 `Local`이면 null로 해제.

## PlayerController.cs

- **역할**: WASD 이동 컨트롤러. 낮·밤 공통, Shift 달리기는 밤에만 활성(기획서 4.1~4.3).
- **상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Rigidbody2D))]`. Rigidbody2D(Dynamic, Gravity 0, Freeze Rotation Z) 전제. 마우스 조준 없음.
- **주요 멤버**:
  - 직렬화: `float moveSpeed = 5f`, `float runMultiplier = 1.6f`, `bool canRun = false`(밤 씬에서 true).
  - `bool IsRunning { get; private set; }`, `Vector2 Facing` (기본 `Vector2.down`).
  - `bool MovementLocked { get; set; }` — 외부 이동 잠금(레벨업 등).
  - `void SetSpeedBonus(float multiplier)` — '눈치' 등 외부 이동 배수(최소 0.1 클램프).
  - `void SetCanRun(bool value)` — 달리기 허용 토글.
- **동작**:
  - `Update`: `MovementLocked`이거나 `timeScale == 0`이면 입력 0. 아니면 Horizontal/Vertical 축을 읽어 대각선은 정규화. `canRun && LeftShift && 이동중`이면 `IsRunning`. 이동 중이면 `Facing` 갱신.
  - `FixedUpdate`: `moveSpeed * _speedBonus * (달리기면 runMultiplier)`로 속도 계산 후 `Rigidbody2D.MovePosition`으로 물리 이동.

## PlayerHealth.cs

- **역할**: 플레이어 HP(멘탈)와 평판 관리, 부활 사이클과 해고 처리(기획서 6장).
- **상속/의존**: `MonoBehaviour`. `System.Collections`(코루틴) 사용. `GameManager.Instance.Run`으로 평판 접근, 해고 시 `GameManager.OnFired` 호출.
- **주요 멤버**:
  - 직렬화: `float maxHp = 100f`, `float hitInvulnSeconds = 0.5f`, `float reviveDelay = 2f`, `float reviveInvulnSeconds = 3f`.
  - `float MaxHp`, `float Hp`, `bool IsDead`, `bool IsInvulnerable` (프로퍼티).
  - 이벤트: `Action<float,float> HpChanged(current,max)`, `Action<int> ReputationChanged`, `Action Revived`, `Action Fired`.
  - `void IncreaseMaxHp(float percent, float healPercentOfMax)` — '멘탈 관리' 강화용, 최대 HP 증가 + 비율 회복.
  - `void Heal(float amount)`, `void TakeDamage(float amount)`.
- **동작**:
  - `Awake`에서 `_maxHp = maxHp`, `Hp` 풀 채움. `Start`에서 초기 HP/평판 이벤트 발행.
  - `Update`에서 무적 타이머 감소.
  - `TakeDamage`: 사망·무적·0 이하 데미지면 무시. HP 차감 후 0.5초 피격 무적 부여. HP 0이면 `OnHpDepleted` 코루틴 시작.
  - `OnHpDepleted`(코루틴): `IsDead=true` → 평판 1 감소 → 평판 0이면 `Fired` 이벤트 + `GameManager.OnFired()` 후 종료. 아니면 2초 뒤 제자리 부활, HP 전체 회복, 3초 무적, `Revived` 발행(기획서 6.3).

## PlayerStats.cs

- **역할**: 강화로 누적된 런타임 스탯 배수 보관(기획서 7장).
- **상속/의존**: `MonoBehaviour`. `StatUpgradeSystem`이 값을 쓰고 무기·이동이 읽음. `UnityEngine.Mathf` 사용.
- **주요 멤버**:
  - 배수 프로퍼티(기본 1): `AttackPowerMul`, `AttackSpeedMul`, `MoveSpeedMul`, `AttackRangeMul`, `ActiveCooldownMul`.
  - `void Reset()` — 모든 배수 1로 초기화.
  - `void ApplyAttackPower/AttackSpeed/MoveSpeed/AttackRange(float perStack, int stacks)` — `1 + perStack*stacks` 증가형.
  - `void ApplyActiveCooldown(float perStack, int stacks)` — `1 - perStack*stacks`의 감소형(최소 0.1 클램프).
- **동작**:
  - 순수 값 보관 컴포넌트. 회귀 시 RunState 리셋 + 씬 재로드로 자연 초기화.
  - 쿨타임 계열은 0~1 배수(작을수록 짧음), 나머지는 1 이상 증가.

## RunState.cs

- **역할**: 한 번의 플레이(런) 동안 유지되는 층·평판·무기·강화·통계 상태. 회귀 시 초기화(기획서 3.3, 영구 성장 없음).
- **상속/의존**: 순수 C# 클래스(MonoBehaviour 아님). `GameManager`가 생성·보유. `WeaponIds` 상수 사용.
- **주요 멤버**:
  - `const int TotalFloors = 4`.
  - 필드: `int Floor = 1`, `FloorPhase Phase = Day`, `int Reputation = 3`, `int DefaultReputation = 3`.
  - `Dictionary<string,int> UpgradeStacks` — 강화 id별 누적 횟수.
  - `List<string> Weapons` — 보유 무기(시작 시 `KeyboardShotgun`).
  - 통계: `int TasksProcessed`, `int PlayerLevel = 1`, `int NightClears`, `float PlayTime`.
  - `void ResetToFirstDay()` — 1층·Day·평판·강화·무기·레벨 초기화(누적 통계는 유지).
  - `bool HasWeapon(string id)`, `int UpgradeLevel(string id)`, `void AddUpgrade(string id)`, `bool IsFinalFloor => Floor >= 4`.
  - `static class WeaponIds`: `KeyboardShotgun`(시작), `StaplerRapid`(1층 밤), `TaskDelegate`(2층 밤·액티브), `ResignationNotice`(3층 밤·궁극기).
- **동작**:
  - 해고·발각 시 `ResetToFirstDay`로 진행 리셋하되 결과 화면용 누적 통계는 보존.
  - 무기·강화 보유 여부를 딕셔너리/리스트로 조회해 시스템들이 참조.
