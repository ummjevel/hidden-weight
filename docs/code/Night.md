# Night 모듈

밤 잠입 스텔스 씬을 구성하는 스크립트 모음이다. 플레이어는 부채꼴 시야를 가진 감시자(경비·야근자·CCTV)를 피해 조사 지점에서 무기를 조사한 뒤 출구로 탈출해야 하며, 발각되거나 제한 시간을 넘기면 즉시 실패한다. 시야 판정(`VisionCone`)을 공용 센서로 삼고, 달리기 소음(`NoiseSystem`)이 경비를 유인하는 구조로 이루어진다.

## CCTV.cs

- **역할**: 좌우로 왕복 회전하며 시야에 들어온 플레이어를 즉시 발각하는 고정 감시 카메라.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(VisionCone))]`로 같은 오브젝트의 `VisionCone`에 의존. `HanGame.Night` 네임스페이스.
- **주요 멤버**:
  - SerializeField: `rotateSpeed`(도/초), `sweepAngle`(중심 ±각도), `centerAngleDeg`(기준 방향), `viewDistance`, `viewAngle`.
  - 내부: `_vision`(VisionCone 참조), `_phase`(누적 회전 위상).
- **동작**:
  - `Awake`에서 `VisionCone`을 캐싱, `Start`에서 `Configure(viewDistance, viewAngle, "Obstacle" 레이어)`로 시야 설정.
  - `Update`에서 `_phase`를 `rotateSpeed`만큼 누적하고 `Sin(_phase)*sweepAngle`로 오프셋을 만들어 `centerAngleDeg` 기준 좌우 왕복. 계산된 각도로 `VisionCone.FacingDir`를 매 프레임 갱신.
  - 실제 발각 판정은 `VisionCone`이 담당. 해킹·정지 기능은 초기 버전 제외.

## ExitZone.cs

- **역할**: 무기 조사 완료 후 플레이어가 도달하면 탈출 이벤트를 발생시키는 출구 트리거.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Collider2D))]`. `HanGame.Common.Player` 참조.
- **주요 멤버**:
  - `event Action PlayerReached` — 플레이어 도착 시 발생.
- **동작**:
  - `OnTriggerEnter2D`에서 상대 콜라이더에 `Player` 컴포넌트가 있으면 `PlayerReached` 호출.
  - 조사 전 도착 여부와 무관하게 이벤트만 발생시키며, 성공 여부(조사 완료 확인)는 구독자인 `NightStealthManager`가 판정.

## GuardPatrol.cs

- **역할**: 정해진 경로를 반복 순찰하며 앞쪽 부채꼴 시야로 플레이어를 탐지하고, 소음을 들으면 해당 위치를 확인하러 가는 경비.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(VisionCone))]`. `HanGame.Data.GuardRouteData`(순찰 경로 데이터)에 의존. `NoiseSystem`이 `HearNoise`를 호출.
- **주요 멤버**:
  - SerializeField: `route`(GuardRouteData — waypoints, viewDistance, viewAngle, moveSpeed, waitAtWaypoint, startDelay, loop 포함).
  - 내부: `_vision`, `_index`(현재 웨이포인트), `_waitTimer`, `_startTimer`, `_investigatePoint`(소음 확인 좌표, nullable), `_investigateTimer`.
  - `public void HearNoise(Vector2 point, float checkSeconds = 2f)` — NoiseSystem이 호출하는 소음 확인 진입점.
- **동작**:
  - `Start`에서 route 기준으로 시야 설정, `startDelay` 세팅, 첫 웨이포인트로 위치 이동.
  - 시작 후 `startTimer` 동안 정지(공정성 위한 3초 대기). 동선은 매번 동일.
  - **소음 확인 우선**: `_investigatePoint`가 있으면 순찰보다 우선해 그 지점으로 이동, 타이머 소진 또는 도착 시 해제.
  - 일반 순찰: 웨이포인트 도착 시 `waitAtWaypoint`만큼 대기 후 `_index++`. 끝에 도달하면 `loop`면 0으로, 아니면 마지막 인덱스 유지.
  - `MoveTowards`는 이동 방향으로 `VisionCone.FacingDir`를 갱신하며 `MoveTowards`로 전진. `Arrived`는 거리 0.1 미만 판정.

## InvestigationPoint.cs

- **역할**: 무기(설계서·규칙·사직서) 조사 지점. 범위 안에서 E키를 1.5초 눌러 조사를 완료하는 상호작용 대상.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Collider2D))]`. `HanGame.Common.Player` 참조.
- **주요 멤버**:
  - SerializeField: `investigateSeconds`(1.5초), `key`(기본 KeyCode.E).
  - `bool Investigated { get; private set; }` — 조사 완료 여부.
  - `event Action<float> Progress`(0~1 진행률), `event Action Completed`(완료).
  - 내부: `_playerInRange`, `_timer`.
- **동작**:
  - `OnTriggerEnter2D/Exit2D`로 플레이어 범위 진입·이탈 추적. 이탈 시 미완료면 타이머 리셋하고 `Progress(0)` 통지.
  - `Update`: 범위 안에서 키를 누르는 동안 `_timer` 누적, `Progress`로 진행률 방출. `investigateSeconds` 도달 시 `Investigated=true`, `Completed` 호출.
  - 키를 떼면 타이머 0으로 리셋(진행 중단). 조사만으로 성공은 아니며 이후 출구 탈출 필요.

## NightStealthManager.cs

- **역할**: 밤 잠입 씬 전체를 조율하는 오케스트레이터. 조사→출구 탈출로 성공, 발각·시간초과로 실패 처리.
- **상속/의존**: `MonoBehaviour`. `HanGame.Common`(GameManager, GameState, Player, AudioManager, Sfx), `HanGame.Data.NightConfig`에 의존. `InvestigationPoint`, `ExitZone`, `VisionCone[]`, `NoiseSystem`을 참조.
- **주요 멤버**:
  - SerializeField: `nightConfigs[]`(층별 설정, index0=1층), `objective`(InvestigationPoint), `exit`(ExitZone), `watchers`(VisionCone[] — 경비·야근자·CCTV), `noise`(NoiseSystem).
  - 프로퍼티: `TimeRemaining`, `WeaponAcquired`, `Finished`.
  - 이벤트: `Action<float> TimeTicked`, `WeaponInvestigated`, `Failed`, `Succeeded`.
- **동작**:
  - `Start`: `GameManager.Run.Floor`로 층 인덱스를 clamp해 `NightConfig` 선택, 상태를 `GameState.Night`로 전환, `TimeRemaining` 초기화(config 없으면 60초).
  - 플레이어에게 달리기 허용(`SetCanRun(true)`), 공격은 밤 씬에 배치하지 않음. `noiseEnabled`에 따라 NoiseSystem 활성화.
  - 모든 `watchers`의 `PlayerSpotted`, `objective.Completed`, `exit.PlayerReached`를 구독.
  - `Update`: 매 프레임 `TimeRemaining` 감소 및 `TimeTicked` 방출, 0 이하 시 `Fail`.
  - `OnSpotted`: `Sfx.GuardSpotted` 재생 후 `Fail`. `OnObjectiveDone`: `WeaponAcquired=true`, `Sfx.ApprovalStamp` 재생. `OnExitReached`: 무기 획득 상태면 `Succeed`.
  - `Succeed`: `GameManager.OnNightCleared(rewardWeaponId)`. `Fail`: `GameManager.OnNightFailed()`(즉시 진행 초기화, 1층 회귀). 둘 다 `Finished` 가드로 중복 방지.

## NightWorker.cs

- **역할**: 한 책상에 머물다 정해진 시간이 지나면 예고 없이 다른 책상으로 이동하는 야근자. 경비보다 좁은 시야.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(VisionCone))]`. `HanGame.Night` 네임스페이스.
- **주요 멤버**:
  - SerializeField: `desks`(Vector2[] 책상 위치), `stayDuration`(4초), `moveSpeed`(2.5), `viewDistance`(3), `viewAngle`(45).
  - 내부: `_vision`, `_index`, `_stayTimer`, `_moving`.
- **동작**:
  - `Start`에서 시야 설정, `_stayTimer` 세팅, 첫 책상으로 위치 이동.
  - `Update`: 이동 중이면 목표 책상 방향으로 `FacingDir`를 돌리며 `MoveTowards`, 도착(거리 0.05 미만) 시 정지하고 체류 타이머 리셋.
  - 체류 중이면 `_stayTimer` 감소, 0 이하 시 `_index`를 다음(순환)으로 넘기고 이동 시작 — 방향을 예고 없이 즉시 전환. 층당 최대 1명(초기 버전).

## NoiseSystem.cs

- **역할**: 달리기 소음 시스템. 플레이어가 달리면 반경 안의 경비에게 위치를 알려 확인하러 오게 함.
- **상속/의존**: `MonoBehaviour`. `HanGame.Common.Player` 참조. `GuardPatrol`을 씬에서 찾아 `HearNoise` 호출.
- **주요 멤버**:
  - SerializeField: `systemEnabled`(기본 true), `noiseRadius`(3), `alertInterval`(0.5초).
  - `public void SetEnabled(bool)`, 프로퍼티 `NoiseRadius`, `Enabled`.
  - 내부: `_guards`(GuardPatrol[]), `_timer`.
- **동작**:
  - `Start`에서 `FindObjectsOfType<GuardPatrol>()`로 경비 목록 캐싱.
  - `Update`: 비활성이거나 플레이어가 달리는 중(`IsRunning`)이 아니면 무시. `alertInterval` 간격으로만 경보를 발동해, 반경 내 모든 경비에게 `HearNoise(플레이어 위치)` 호출.
  - `NoiseRadius`/`Enabled`는 밤 HUD에서 달릴 때 소음 범위를 원으로 표시하는 데 사용. 개발 여유가 없으면 비활성화해 달리기만 유지 가능.

## VisionCone.cs

- **역할**: 부채꼴 시야 판정의 공용 센서. 경비·야근자·CCTV가 공유하며, 시야각·거리 안이고 벽에 가리지 않으면 발각.
- **상속/의존**: `MonoBehaviour`. `HanGame.Common.Player` 참조. `Physics2D.Raycast`로 벽 차단 검사.
- **주요 멤버**:
  - SerializeField: `viewDistance`, `viewAngle`, `obstacleMask`(벽·책상 레이어), `origin`(시야 시작점, 없으면 자기 자신).
  - 프로퍼티: `Vector2 FacingDir`(바라보는 방향, 감시자가 매 프레임 갱신), `bool Active`.
  - `event Action PlayerSpotted`.
  - `public void Configure(float distance, float angle, LayerMask mask)`, `public bool CanSeePlayer()`, `public void ResetTrigger()`.
  - 내부: `_fired`(1회성 발각 플래그).
- **동작**:
  - `Update`: 활성이고 아직 발각 안 됐을 때 `CanSeePlayer()`가 참이면 `_fired=true`로 잠그고 `PlayerSpotted` 1회 방출.
  - `CanSeePlayer` 판정 순서: (1) 거리 > `viewDistance`면 실패, (2) `FacingDir`와의 각도가 `viewAngle/2` 초과면 실패, (3) 원점→플레이어 방향으로 `obstacleMask` 레이캐스트해 벽에 막히면 실패, 셋 다 통과해야 발각. 판정을 명확·공정하게 하기 위한 벽 차단 검사.
  - `OnDrawGizmosSelected`로 시야 좌우 경계선을 에디터에 시각화. `Rotate`는 2D 벡터 회전 유틸.

