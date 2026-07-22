# Data 모듈

`HanGame.Data` 네임스페이스의 데이터 테이블 계층이다. 수치를 코드에 하드코딩하지 않고 `ScriptableObject` 에셋으로 관리하며(기획서 19.4), 플레이어·무기·강화·적·웨이브·층·밤 설정을 담는다. 각 SO는 `[CreateAssetMenu]`로 `HanGame/...` 메뉴에서 에셋을 생성하고, 스포너·전투·잠입 시스템들이 이를 읽어 동작한다. `EnemyType.cs`와 `UpgradeStat`/`WeaponKind` 등은 순수 enum 정의다.

## EnemyData.cs

- **역할**: 적 한 종류의 기본 수치 정의(기획서 9.4).
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Enemy Data")]`. `EnemyType`/`EnemyBehavior` enum, 프리팹 `GameObject` 참조.
- **주요 멤버(직렬화 필드)**:

  | 필드 | 의미 |
  |---|---|
  | `EnemyType type` | 적 종류 식별 |
  | `EnemyBehavior behavior` | AI 행동 유형(분기용) |
  | `string displayName` | 표시 이름 |
  | `GameObject prefab` | 스폰 프리팹 |
  | `float maxHp = 20` | 최대 HP(1층 100% 기준) |
  | `float moveSpeed = 2` | 이동속도 |
  | `float contactDamage = 5` | 접촉 피해 |
  | `float attackInterval = 1` | 공격 주기 |
  | `float attackRange = 0` | 0이면 근접, >0이면 원거리 |
  | `float projectileSpeed = 4` | 원거리 발사체 속도 |
  | `float dashTelegraph = 0.6` | 돌진 예고 시간 |
  | `float dashSpeed = 8` / `float dashRange = 5` | 돌진 속도 / 시작 거리 |
  | `float debuffRadius = 2.5` | 디버프 반경 |
  | `float attackSpeedDebuff = 0.4` | 근처 시 플레이어 공격속도 -40% |
  | `int expReward = 1` | 경험치 지급량 |
  | `float coffeeDropChance = 0.05`(0~1) | 아메리카노 드롭 확률 |
- **동작**: 데이터 컨테이너. `FloorConfig`의 층별 배수(hp/speed/spawn)가 이 기본 수치에 곱해져 난이도가 상승하며, 행동별 필드(Dasher/Debuffer/원거리)는 해당 behavior에서만 사용된다.

## EnemyType.cs

- **역할**: 적 종류와 행동 유형 enum 정의(기획서 9.2).
- **상속/의존**: 순수 enum 2종. `EnemyData`가 참조하고 Enemy AI가 behavior로 분기.
- **주요 멤버**:

  `EnemyType`:

  | 값 | 설명 |
  |---|---|
  | `EmailEnvelope` | 이메일 봉투 — 기본 군집형 |
  | `PaperStack` | 서류 더미 — 탱커형 |
  | `UrgentPostit` | 긴급 수정 포스트잇 — 돌진형 |
  | `MeetingCalendar` | 회의 요청 달력 — 디버프형 |
  | `ClaimPhone` | 클레임 전화기 — 원거리형 |
  | `CeoDirective` | CEO 최종 지시서 — 최종 웨이브 |

  `EnemyBehavior`: `Chaser`(직선 추격), `Tank`(느림·고HP), `Dasher`(예고 후 돌진), `Debuffer`(근접 시 공격속도 감소), `Ranged`(거리 유지 원거리), `Boss`(소환·범위 공격).
- **동작**: 값만 정의. 로직 없음.

## FloorConfig.cs

- **역할**: 층 하나의 낮 전투 설정(기획서 5.4/5.5/10.3).
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Floor Config")]`. `WaveTable` 참조. `EnemySpawner`가 사용.
- **주요 멤버(직렬화 필드)**:

  | 필드 | 의미 |
  |---|---|
  | `int floor = 1` / `string displayName` | 층 번호 / 표시 이름 |
  | `WaveTable waveTable` | 이 층의 생성표 |
  | `float dayDuration = 60` | 낮 전투 길이(초) |
  | `float hpMultiplier = 1` | 적 HP 배수(1층 100% 기준) |
  | `float speedMultiplier = 1` | 적 속도 배수 |
  | `float spawnMultiplier = 1` | 생성량 배수 |
  | `bool bossGazeEnabled = true` | 상사의 시선 활성 |
  | `float bossGazeFirstAt = 30` | 최초 발동 시각(초) |
  | `int bossGazeSweeps = 1` | 시선 이동 횟수 |
  | `float bossGazeWidth = 2` / `float bossGazeSpeed = 3` | 시선 폭 / 이동 속도 |
  | `bool isFinalFloor = false` | CEO 최종 웨이브(4층) 여부 |
- **동작**: 층별 난이도는 체력보다 생성량 중심으로 배수를 적 기본 수치에 곱해 조절(기획서 5.5). '상사의 시선' 기믹 파라미터도 층 단위로 관리(10.3).

## NightConfig.cs

- **역할**: 밤 잠입 한 층 설정, 그리고 경비 순찰 경로 데이터(기획서 11장, 19.4).
- **상속/의존**: 두 개의 `ScriptableObject` — `NightConfig`(`HanGame/Night Config`), `GuardRouteData`(`HanGame/Guard Route`). `NightStealthManager`/`GuardPatrol`이 사용.
- **주요 멤버(직렬화 필드)**:

  `NightConfig`:

  | 필드 | 의미 |
  |---|---|
  | `int floor = 1` | 층 번호 |
  | `float timeLimit = 60` | 제한 시간(초) |
  | `float investigateSeconds = 1.5` | 조사 소요 시간 |
  | `string rewardWeaponId = "stapler_rapid"` | 조사 보상 무기 id |
  | `string objectiveName` | 목표물 이름 |
  | `bool noiseEnabled = true` / `float noiseRadius = 3` | 소음 활성 / 반경 |

  `GuardRouteData`:

  | 필드 | 의미 |
  |---|---|
  | `List<Vector2> waypoints` | 순찰 경로(월드 좌표) |
  | `bool loop = true` | 경로 순환 여부 |
  | `float moveSpeed = 2` | 이동 속도 |
  | `float waitAtWaypoint = 0.5` | 웨이포인트 대기 |
  | `float startDelay = 3` | 시작 후 정지 시간 |
  | `float viewDistance = 4` / `float viewAngle = 60` | 부채꼴 시야 거리 / 각도 |
- **동작**: 밤의 제한 시간·조사 보상·소음을 데이터로 관리하고, 경비의 순찰 경로와 부채꼴 시야를 별도 SO로 분리해 씬마다 조합한다.

## PlayerData.cs

- **역할**: 플레이어 기본 능력치 정의(기획서 19.4).
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Player Data")]`.
- **주요 멤버(직렬화 필드)**:

  | 필드 | 의미 |
  |---|---|
  | `float maxHp = 100` | 최대 HP(멘탈) |
  | `float moveSpeed = 5` | 이동속도 |
  | `float runMultiplier = 1.6` | 밤 달리기 배수 |
  | `int startingReputation = 3` | 시작 평판 |
  | `int baseExpToLevel = 8` | 1→2 레벨 필요 경험치 |
  | `float expGrowthPerLevel = 1.4` | 레벨당 필요량 배수 |
  | `float coffeeHealAmount = 25` | 커피(아메리카노) 회복량 |
- **동작**: 값 컨테이너. `PlayerController`/`PlayerHealth`의 기본 수치와 레벨업 경험치 곡선 산정에 사용.

## UpgradeData.cs

- **역할**: 스탯 강화 1종 정의(기획서 7.2/7.3). 강화 대상 스탯 enum도 포함.
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Upgrade Data")]` + `UpgradeStat` enum. `StatUpgradeSystem`이 배열로 읽음.
- **주요 멤버**:

  `UpgradeStat` enum:

  | 값 | 인게임 명칭 / 효과 |
  |---|---|
  | `AttackPower` | 업무처리력 — 모든 공격력 |
  | `AttackSpeed` | 손속도 — 기본 무기 공격속도 |
  | `MoveSpeed` | 눈치 — 이동속도 |
  | `MaxHp` | 멘탈 관리 — 최대 HP + 현재 회복 |
  | `AttackRange` | 일머리 — 공격 범위 |
  | `ActiveCooldown` | 짬 — 업무 떠넘기기 쿨타임 감소 |

  직렬화 필드:

  | 필드 | 의미 |
  |---|---|
  | `string id = "attack_power"` | 강화 식별자 |
  | `string displayName` / `string description`(TextArea) | 표시 이름 / 설명 |
  | `Sprite icon` | 아이콘 |
  | `UpgradeStat stat` | 영향 스탯 |
  | `float valuePerStack = 0.15` | 1회 효과(비율 또는 값) |
  | `int maxStacks = 5` | 최대 중첩 |
  | `float healPercentOnPick = 0.2` | 멘탈 관리 전용, 선택 시 즉시 회복 비율 |
  | `string requiresWeaponId = ""` | 비어있으면 항상 등장, 있으면 해당 무기 획득 전엔 후보 제외('짬') |
- **동작**: 레벨업 선택지 후보 구성에 쓰이며, `requiresWeaponId`로 조건부 등장을 제어. 값 누적은 `PlayerStats.Apply*`가 `valuePerStack × stacks`로 반영.

## WaveTable.cs

- **역할**: 한 층 60초 동안의 적 생성표(기획서 5.3).
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Wave Table")]`. `SpawnEntry`가 `EnemyData` 참조. `EnemySpawner`가 사용.
- **주요 멤버**:
  - `struct SpawnEntry { EnemyData enemy; float startTime; float endTime; float interval; int countPerSpawn; }` — 시간 구간·간격·1회 스폰 수.
  - `float duration = 60` — 웨이브 총 길이.
  - `int maxAlive = 60` — 동시 최대 개체 수(초과 시 스폰 보류).
  - `List<SpawnEntry> entries`.
- **동작**: 스포너가 경과 시간에 해당하는 항목들을 읽어 `interval`마다 `countPerSpawn`만큼 스폰하되, 살아있는 개체가 `maxAlive`를 넘으면 스폰을 보류한다.

## WeaponData.cs

- **역할**: 무기·스킬 수치 정의(기획서 8장). 무기 종류 enum 포함.
- **상속/의존**: `ScriptableObject`, `[CreateAssetMenu("HanGame/Weapon Data")]` + `WeaponKind` enum. `id`는 `RunState.WeaponIds` 값과 일치시킴. `AutoAttackSystem`/무기 로직이 사용.
- **주요 멤버**:

  `WeaponKind` enum: `AutoBasic`(자동 기본 — 키보드 샷건/스테이플러), `Active`(액티브 스킬 — 업무 떠넘기기), `Ultimate`(궁극기 — 퇴사 통보).

  직렬화 필드:

  | 필드 | 의미 |
  |---|---|
  | `string id = "keyboard_shotgun"` | 식별자(`WeaponIds`와 일치) |
  | `string displayName` / `WeaponKind kind` | 표시 이름 / 종류 |
  | `float damage = 8` | 피해량 |
  | `float attackInterval = 0.9` | 공격 주기(낮을수록 빠름) |
  | `float range = 6` | 자동 조준 탐색 거리 |
  | `float projectileSpeed = 10` / `GameObject projectilePrefab` | 발사체 속도 / 프리팹 |
  | `int pellets = 5` / `float spreadAngle = 60` | 키보드 샷건 부채꼴 발수 / 각도 |
  | `bool pierces = false` | 스테이플러 관통 여부(false면 첫 적중 시 소멸) |
  | `float pushRadius = 3` / `float pushForce = 8` / `float cooldown = 12` | 업무 떠넘기기 반경 / 밀치는 힘 / 쿨타임 |
  | `float fearDuration = 3` | 퇴사 통보: 일반 적 공포/도주 시간 |
  | `float ceoStunDuration = 3` | CEO 웨이브 정지 시간 |
  | `float eliteSlow = 0.5` | 정예 이동속도 감소 |
  | `float gaugePerKill = 0.05`(0~1) | 처리당 궁극기 게이지 충전량 |
- **동작**: 종류별로 사용하는 필드가 다르며(자동 무기 공통 / 샷건 / 스테이플러 / 액티브 / 궁극기 그룹), `kind`에 따라 해당 무기 로직이 필요한 값만 읽는다.
