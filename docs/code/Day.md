# Day 모듈

낮 전투(Vampire-Survivors형 사무실 방어) 씬을 구성하는 스크립트 모음이다. `DayCombatManager`가 스포너·타이머·상사의 시선·경험치·강화·드롭을 오케스트레이션하며, 층별 `FloorConfig` 수치를 적용해 60초 생존 웨이브를 진행한다. 모든 스크립트는 `HanGame.Day` 네임스페이스에 속한다.

---

## BossGaze.cs

**역할**: "상사의 눈치" 맵 기믹. 붉은 시선 영역이 화면을 가로질러 이동하며, 걸린 플레이어를 2초간 '일하는 척'(자동 공격 정지) 상태로 만든다.

**상속/의존**: `MonoBehaviour`. `Transform gazeVisual`(시선 스프라이트) 참조, `FloorConfig`(HanGame.Data)로 설정, `Player.Local`·`AudioManager`(HanGame.Common)에 의존. `AutoAttackSystem`이 `PlayerCaught`를 읽는다.

**주요 멤버**
- `[SerializeField] Transform gazeVisual`, `float pretendDuration = 2f`
- `bool PlayerCaught { get; }` — 걸린 동안 true (자동 공격 정지 신호)
- `event Action WarningRaised` — "상사가 보고 있습니다" UI용
- `void Configure(FloorConfig floor)` — 폭·활성화 설정 및 상태 초기화
- `void Tick(float waveElapsed)` — 매니저가 매 프레임 웨이브 경과 시간 전달

**동작**
- `Tick`에서 `bossGazeFirstAt` 도달 시 한 번만 `RunSweeps` 코루틴 예약(`_scheduled` 플래그).
- `RunSweeps`: `bossGazeSweeps` 횟수만큼 반복. 경고(`WarningRaised` + `BossGazeWarn` SFX) → 2초 대기 → 시선 이동 → 3초 간격.
- `Sweep`: 시선 비주얼을 좌측(-half=−12)에서 우측으로 `bossGazeSpeed`로 이동. 플레이어 x좌표가 시선 폭(`bossGazeWidth`) 절반 안에 들면 `CatchPlayer` 실행.
- `CatchPlayer`: `PlayerCaught`를 `pretendDuration` 동안 true로 유지(이동은 가능, 공격만 정지). 이미 걸린 상태면 무시.

---

## DayCombatManager.cs

**역할**: 낮 전투 씬 오케스트레이터. 하위 시스템을 연결하고, 적 처리 시 드롭, 레벨업 시 시간 정지, 60초 생존 시 층 통과를 처리한다.

**상속/의존**: `MonoBehaviour`. `EnemySpawner`, `WaveTimer`, `BossGaze`, `StatUpgradeSystem`, `ExperienceSystem`(런타임 검색) 참조. `FloorConfig[]`, 드롭 프리팹 참조. `GameManager`·`AudioManager`(HanGame.Common)에 의존.

**주요 멤버**
- `[SerializeField] FloorConfig[] floors`(index 0 = 1층), 시스템 참조 필드, `expPickupPrefab`/`coffeePickupPrefab`, `bool pauseOnLevelUp = true`
- `event Action<List<UpgradeData>> LevelUpOffered` — 레벨업 시 후보 3종 전달(UI 구독)
- `event Action DaySurvived`
- `void ResolveLevelUp(UpgradeData picked)` — UI 선택 콜백

**동작**
- `Start`: `GameManager.Run.Floor`로 층 인덱스 선택, 상태를 `Day`로 설정, 이벤트 구독(`spawner.EnemyKilled`, `timer.Completed`, `exp.LeveledUp`), `bossGaze.Configure` → `spawner.Begin` → `timer.Begin(dayDuration)`. 최종 층이면 `CeoWaveWarn` SFX.
- `Update`: 타이머 실행 중이면 `bossGaze.Tick(timer.Elapsed)`.
- `OnEnemyKilled`: `Run.TasksProcessed++`, 경험치 픽업 생성(`e.Data.expReward`), 확률(`coffeeDropChance`)로 커피 픽업 생성.
- `OnLeveledUp`: `upgrades.RollOptions()` 후보가 있으면 `Time.timeScale = 0`(정지), 상태 `DayLevelUp`, `LevelUpOffered` 발행. `ResolveLevelUp`에서 `upgrades.Pick` 후 시간 복구.
- `OnWaveComplete` → `SurviveSequence`: 남은 적 제거(`ClearRemaining`), 1초 대기 후 `DaySurvived` + `GameManager.OnDaySurvived`.

---

## Enemy.cs

**역할**: 낮 전투 적 개체. 이동·공격·피격·사망 처리를 담당. `EnemyData` 기본 수치에 `FloorConfig` 배수를 곱해 초기화하며, 사람이 아닌 플레이어만 목표로 한다.

**상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Rigidbody2D))]`. `EnemyData`/`FloorConfig`(HanGame.Data), `Player.Local`/`PlayerHealth`(HanGame.Common), `EnemyRegistry`에 의존.

**주요 멤버**
- `EnemyData Data { get; }`, `bool IsDead { get; }`, `bool IsElite { get; }`, `EnemyType Type { get; }`
- `event Action<Enemy> Killed` — 처리 완료 통지(스포너 구독)
- `void Init(EnemyData data, FloorConfig floor, bool elite = false)`
- `void TakeDamage(float amount)`, `void Kill()`
- 상태 이상: `void ApplyFear(float duration, float eliteSlow)`, `void ApplyStun(float duration)`, `void Push(Vector2 origin, float force)`

**동작**
- `Init`: HP = `maxHp × hpMultiplier × (정예 2.5)`, 속도 = `moveSpeed × speedMultiplier`, 접촉 피해 설정, `EnemyRegistry.Register`. `OnDestroy` 시 Unregister.
- `FixedUpdate` 이동 상태 분기:
  - 스턴(`_stunTimer>0`)이면 정지.
  - 공포(`_fearTimer>0`)면 플레이어 반대 방향으로 도주(1.1배 속도).
  - `EnemyBehavior.Ranged`: 사거리(`attackRange`) 유지(가까우면 후퇴, 멀면 접근).
  - `EnemyBehavior.Dasher`: 사거리(`dashRange`) 안이고 쿨(`attackInterval`) 준비되면 예고(`dashTelegraph`) 후 `dashSpeed`로 돌진, 그 외엔 0.6배 속도로 접근.
  - 기본(Chaser/Tank/Debuffer/Boss): 플레이어로 직접 접근.
- 접촉 피해: 근접형만(`attackRange==0`) `OnCollisionStay2D`/`OnTriggerStay2D`에서 `attackInterval` 간격으로 `PlayerHealth.TakeDamage`.
- `TakeDamage`로 HP 0 이하 시 `Kill`: `IsDead`, Unregister, `Killed` 발행, `Destroy`.
- `ApplyFear`: 정예는 `_speedFactor` 감속(Invoke로 복구), 일반은 도주 타이머. `ApplyStun`: 정지 타이머. `Push`: origin 반대 방향으로 밀어냄.

---

## EnemyRegistry.cs

**역할**: 살아있는 적을 추적하는 정적 레지스트리. 자동 조준, 상사의 시선, 업무 떠넘기기, 퇴사 통보가 `FindObjectsOfType` 없이 대상을 얻게 한다.

**상속/의존**: `static class`(MonoBehaviour 아님). `Enemy`, `UnityEngine.Vector2`에 의존.

**주요 멤버**
- `static readonly List<Enemy> Alive`, `static int Count`
- `static void Register/Unregister/Clear(...)`
- `static Enemy Nearest(Vector2 from, float maxRange)` — 범위 내 최근접 적(없으면 null)
- `static List<Enemy> InRadius(Vector2 center, float radius, List<Enemy> buffer)` — 반경 내 적 수집(전달 버퍼 재사용)

**동작**
- `Enemy.Init`/`Kill`/`OnDestroy`가 등록·해제를 관리.
- `Nearest`/`InRadius` 모두 `sqrMagnitude` 비교로 거리 계산(제곱근 회피), null·`IsDead` 개체는 건너뜀.
- `InRadius`는 호출자가 넘긴 버퍼를 `Clear` 후 채워 GC 할당을 줄인다.

---

## EnemySpawner.cs

**역할**: 층별 60초 생성표(`WaveTable`)를 읽어 맵 사방에서 적을 스폰. 동시 최대 개체 수를 초과하면 스폰을 보류한다.

**상속/의존**: `MonoBehaviour`. `FloorConfig`/`WaveTable`/`EnemyData`(HanGame.Data), `Player.Local`(HanGame.Common), `Enemy`/`EnemyRegistry`에 의존.

**주요 멤버**
- `[SerializeField] float spawnRadius = 10f`, `float mapHalfExtent = 12f`
- `Action<Enemy> EnemyKilled` — 처리 콜백 외부 구독용
- `void Begin(FloorConfig floor)`, `void Stop()`, `void ClearRemaining()`

**동작**
- `Begin`: `waveTable` 로드, 시간·플래그 초기화, `EnemyRegistry.Clear`, 엔트리별 `_nextSpawnAt`을 `startTime`으로 설정.
- `Update`: `_time` 누적(레벨업 정지 반영), `maxAlive = table.maxAlive × spawnMultiplier` 계산. 각 엔트리에 대해 활성 구간(`startTime`~`endTime`), 다음 스폰 시각, 최대 개체 수 조건 충족 시 `countPerSpawn × spawnMultiplier`만큼 스폰하고 다음 스폰 시각을 `interval` 뒤로 설정.
- `Spawn`: `EnemyData.prefab` 인스턴스화, `Enemy` 컴포넌트 확보 후 `Init`, `Killed` → `OnEnemyKilled` → 외부 `EnemyKilled` 재발행.
- `RandomEdgePosition`: 플레이어 중심에서 `spawnRadius`만큼 떨어진 무작위 방향 위치를 맵 경계로 클램프.
- `ClearRemaining`: 실행 중지 후 스냅샷 순회하며 `CeoDirective`(CEO 지시)를 제외한 적을 `Kill`.

---

## ExperienceSystem.cs

**역할**: 경험치·레벨 관리. 적 처리로 경험치를 얻고 임계치 도달 시 레벨업 이벤트를 발행한다(시간 정지는 매니저가 처리).

**상속/의존**: `MonoBehaviour`. `PlayerData`(HanGame.Data), `GameManager`(HanGame.Common)에 의존.

**주요 멤버**
- `[SerializeField] PlayerData playerData`
- `int Level { get; } = 1`, `int Exp { get; }`, `int ExpToNext { get; }`
- `event Action<int,int> ExpChanged`(현재, 다음 필요치), `event Action<int> LeveledUp`(새 레벨)
- `void AddExp(int amount)`

**동작**
- `Awake`: `GameManager.Run.PlayerLevel`에서 레벨 복원, `ExpToNext` 초기 계산.
- `RequiredFor(level) = baseExpToLevel × expGrowthPerLevel^(level-1)`(반올림) — 지수적 증가.
- `AddExp`: 경험치 누적 후 `while` 루프로 임계치 초과분을 이월하며 다중 레벨업 처리. 레벨업마다 `Run.PlayerLevel` 갱신 및 `LeveledUp` 발행, 마지막에 `ExpChanged` 발행.

---

## Pickups.cs

**역할**: 드롭 아이템 두 종. `ExpPickup`(경험치 서류)과 `CoffeePickup`(아메리카노, HP 회복)을 정의한다.

**상속/의존**: 두 클래스 모두 `MonoBehaviour`, `[RequireComponent(typeof(Collider2D))]`. `Player`/`PlayerHealth`/`AudioManager`(HanGame.Common), `ExperienceSystem`(런타임 검색)에 의존.

**주요 멤버**
- `ExpPickup`: `[SerializeField] int amount`, `float attractRadius = 2.5f`, `float attractSpeed = 8f`; `void SetAmount(int)`
- `CoffeePickup`: `[SerializeField] float healAmount = 25f`; `void SetHeal(float)`

**동작**
- `ExpPickup.Update`: 플레이어가 `attractRadius` 안에 들면 `attractSpeed`로 빨려감(자석 효과). `OnTriggerEnter2D`에서 `Player` 접촉 시 `ExperienceSystem.AddExp(amount)` 후 파괴.
- `CoffeePickup.OnTriggerEnter2D`: `Player` 접촉 시 `Health.Heal(healAmount)`, `CoffeeHeal` SFX 재생 후 파괴.

---

## StatUpgradeSystem.cs

**역할**: 레벨업 시 강화 3종 후보를 제시하고 선택을 적용. 누적치는 `RunState`에 저장되어 다음 층까지 유지, 회귀 시 초기화된다.

**상속/의존**: `MonoBehaviour`. `UpgradeData`(HanGame.Data), `PlayerStats`/`PlayerHealth`/`Player`/`GameManager`/`RunState`(HanGame.Common)에 의존.

**주요 멤버**
- `[SerializeField] List<UpgradeData> allUpgrades`, `int optionsPerLevel = 3`
- `List<UpgradeData> RollOptions()` — 후보 추첨
- `void Pick(UpgradeData upgrade)` — 선택 적용
- `void ReapplyAll()` — 누적치를 `PlayerStats`에 반영

**동작**
- `Awake`: `Player.Local`(없으면 검색)에서 `PlayerStats`/`PlayerHealth` 확보, `ReapplyAll`로 이전 층 스탯 복원.
- `RollOptions`: 최대 중첩(`maxStacks`) 도달 강화, 미보유 무기(`requiresWeaponId`) 조건 강화를 풀에서 제외 후 중복 없이 무작위 `optionsPerLevel`개 반환.
- `Pick`: `Run.AddUpgrade(id)` 후 `ReapplyAll`. `MaxHp` 스탯은 여기서 즉시 `IncreaseMaxHp(valuePerStack, healPercentOnPick)` 적용.
- `ReapplyAll`: `_stats.Reset` 후 스택별로 `AttackPower`/`AttackSpeed`/`MoveSpeed`/`AttackRange`/`ActiveCooldown` 재적용(MaxHp는 즉시형이라 제외), 마지막에 이동속도 배수를 `Controller.SetSpeedBonus`로 반영.

---

## WaveTimer.cs

**역할**: 낮 전투 60초 카운트다운. `timeScale=0`(레벨업)이면 자동으로 멈춘다(unscaled 미사용).

**상속/의존**: `MonoBehaviour`. 외부 스크립트 참조 없음(순수 타이머).

**주요 멤버**
- `float Duration { get; } = 60f`, `float Elapsed { get; }`, `float Remaining { get; }`, `bool Running { get; }`
- `event Action Completed`, `event Action<float> Ticked`(남은 시간)
- `void Begin(float duration)`, `void Stop()`

**동작**
- `Begin`: 지속 시간 설정, `Elapsed` 초기화, 실행 시작.
- `Update`: 실행 중이면 `Time.deltaTime` 누적(`timeScale=0`이면 자동 정지), 매 프레임 `Ticked(Remaining)` 발행. `Elapsed ≥ Duration`이면 실행 중지 후 `Completed` 발행.

---

## 이벤트 흐름 요약

- 적 처리: `Enemy.Kill` → `Killed` → `EnemySpawner.OnEnemyKilled` → `EnemyKilled` → `DayCombatManager.OnEnemyKilled`(드롭·통계).
- 레벨업: `ExpPickup` → `ExperienceSystem.AddExp` → `LeveledUp` → `DayCombatManager.OnLeveledUp`(시간 정지 + `LevelUpOffered`) → UI 선택 → `ResolveLevelUp` → `StatUpgradeSystem.Pick`.
- 생존: `WaveTimer.Completed` → `DayCombatManager.SurviveSequence` → `ClearRemaining` → `DaySurvived`.
