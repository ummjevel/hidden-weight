# Player 모듈

`HiddenWeight.Player`는 플레이어의 이동·점프·대시·벽클링/벽점프 상태머신, 근접 공격 판정, 체력·무적·리스폰, 입력 게이트, 애니메이터 연동을 담당한다. 튜닝 수치는 전부 `HiddenWeight.Data.PlayerData`(`GameManager.Instance.Balance.player`)에서 읽으며, World 모듈에 대해서는 `World/Interactions.cs`의 `IDamageable` 계약 하나만 예외적으로 참조한다.

## PlayerAnimator.cs

- **역할**: `PlayerController.StateChanged` 이벤트를 받아 Animator 정수 파라미터("State")로 반영하고, 매 프레임 스프라이트 좌우 반전을 처리한다. Animator 컴포넌트가 아직 없는(플레이스홀더 스프라이트 단계) 프리팹에서도 예외 없이 동작하도록 모든 참조에 null 체크가 있다.
- **상속/의존**: `MonoBehaviour`. `UnityEngine`만 사용. 같은 GameObject의 `Animator`, `PlayerController`, 자식의 `SpriteRenderer`를 `GetComponent`/`GetComponentInChildren`로 캐시한다.
- **주요 멤버**:
  - `static readonly int StateParam = Animator.StringToHash("State")` — Animator 파라미터 이름 해시 캐시.
  - `Animator _animator`, `SpriteRenderer _sprite`, `PlayerController _controller` (모두 private, `Awake`에서 할당, 없으면 `null`).
  - 공개 메서드/이벤트 없음(순수 리스너 컴포넌트).
- **동작**:
  - `Awake`: `GetComponent<Animator>()`, `GetComponentInChildren<SpriteRenderer>()`, `GetComponent<PlayerController>()`를 각각 캐시.
  - `OnEnable`/`OnDisable`: `_controller != null`이면 `StateChanged` 이벤트를 각각 구독/해제.
  - `Update`: `_sprite != null && _controller != null`이면 `_sprite.flipX = _controller.Facing < 0` — 매 프레임 `Facing` 부호로 좌우 반전을 갱신한다(이벤트가 아니라 폴링).
  - `HandleStateChanged(PlayerState state)`: `_animator != null`이면 `_animator.SetInteger(StateParam, (int)state)` — `PlayerState` enum 선언 순서가 곧 Animator 정수 값이므로 `PlayerState.cs`의 항목 순서를 바꾸면 Animator 트랜지션이 깨진다.

## PlayerAttack.cs

- **역할**: 플레이어의 근접 공격 입력·쿨다운·판정을 담당한다. 전방 부채꼴 범위 안의 `IDamageable` 대상에게 피해를 적용한다.
- **상속/의존**: `MonoBehaviour`. `UnityEngine`, `HiddenWeight.Data`(`PlayerData`), `HiddenWeight.Core`(`GameManager`), `HiddenWeight.World`(`IDamageable`)를 사용. **World 의존 예외**: `World/Interactions.cs`에 정의된 `IDamageable` 인터페이스 하나만 참조하며, Enemies 모듈의 실제 `Enemy` 구현 타입은 절대 참조하지 않는다. `Interactions.cs`는 어떤 모듈에도 의존하지 않는 순수 계약 파일이므로, 이 참조가 있어도 기획서 3.1절이 규정한 World→Player 의존 방향이 깨지지 않는다(설계상 허용된 유일한 Player→World 참조).
- **주요 멤버**:
  - `[SerializeField] LayerMask enemyLayer` (기본값 없음, 인스펙터에서 지정 필요) — 피격 판정 대상 레이어.
  - `PlayerData _data`, `PlayerController _controller` (private, `Awake`에서 할당), `float _cooldownTimer` (private).
  - `bool CanAttack { get; set; } = true` — Emotions의 `HushSkill`이 숨죽이기 중 `false`로 내려 공격을 막는다.
  - `event System.Action Attacked` — 공격이 실제로 발동될 때(쿨다운 통과 시) 발행. `PlayerController`가 구독해 `Attack` 상태 지속 시간(`_attackTimer`)을 갱신한다.
- **동작**:
  - `Awake`: `_data = GameManager.Instance.Balance.player`, `_controller = GetComponent<PlayerController>()`.
  - `Update`: `_cooldownTimer > 0f`면 `Time.deltaTime`만큼 감소. 이어서 `!CanAttack || _cooldownTimer > 0f || !PlayerInput.AttackPressed`이면 즉시 리턴(가드 3중 — 숨죽이기 중, 쿨다운 중, 입력 없음). 조건을 통과하면 `_cooldownTimer = _data.attackCooldown`(0.35s)로 재설정하고 `PerformAttack()` 호출.
  - `PerformAttack`: `Physics2D.OverlapCircleAll(transform.position, _data.attackRadius(1.2), enemyLayer)`로 반경 내 콜라이더를 모두 가져온 뒤, 각 대상에 대해 `facingVec`(플레이어 바라보는 방향)과 `대상 방향` 사이 각도가 `_data.attackAngle(90) * 0.5`(45도) 이하인 것만(부채꼴 판정) 필터링한다. 통과한 대상은 `GetComponentInParent<IDamageable>()`로 인터페이스를 찾아 `damageable != null && damageable.IsAlive`일 때만 `TakeDamage(_data.attackDamage(1), transform.position)`을 호출한다. 마지막에 `Attacked?.Invoke()`로 공격 발동을 알린다.

## PlayerController.cs

- **역할**: 플레이어 이동·점프·대시·벽클링·벽점프를 담당하는 물리 상태머신. 매 `FixedUpdate`마다 접지/벽 판정 → 타이머 갱신 → 이동/대시/벽/점프 처리 → `PlayerState` 결정 순으로 실행되며, 실제 상태 전이는 `PlayerAnimator` 등 외부에 `StateChanged` 이벤트로 통지한다.
- **상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]`. `HiddenWeight.Data`(`PlayerData`), `HiddenWeight.Core`(`GameManager`) 사용. 같은 GameObject의 `PlayerAttack`을 참조해 `Attacked` 이벤트를 구독한다.
- **주요 멤버**:
  - 인스펙터 필드: `Transform groundCheck`, `Transform wallCheck`, `LayerMask groundLayer`, `LayerMask wallLayer`, `Vector2 groundCheckSize = new Vector2(0.6f, 0.15f)`, `Vector2 wallCheckSize = new Vector2(0.15f, 0.8f)` — 마지막 두 개는 밸런스 수치가 아니라 판정 상자 지오메트리라 `PlayerData`가 아닌 이 컴포넌트의 인스펙터 필드로 둔다는 주석이 있음.
  - private 필드: `Rigidbody2D _rb`, `PlayerData _data`, `PlayerAttack _attack`, 타이머 — `_coyoteTimer`, `_wallCoyoteTimer`(2026-07-26 추가, 벽에서 떨어진 직후 벽점프 유예), `_jumpBufferTimer`, `_dashTimer`, `_dashCooldownTimer`, `_wallJumpLockTimer`, `_knockbackLockTimer`(하드코딩 0.2s), `_landTimer`(하드코딩 0.12s), `_attackTimer`. 벽 상태 — `int _wallDir`(지금 닿아 있는 벽 방향 +1/-1/0), `int _lastWallDir`(벽 코요테용 마지막 벽 방향).
  - `static PlayerController Instance { get; private set; }` — 전역 접근점(`Awake`에서 설정, 중복 파괴 처리 없음 — 씬에 하나만 존재한다고 가정).
  - `PlayerState State { get; private set; }`, `bool IsGrounded { get; private set; }`, `bool IsOnWall { get; private set; }`, `int Facing { get; private set; } = 1`.
  - `float ExternalSpeedMultiplier { get; set; } = 1f` — Emotions의 `EmotionSkill`/`AwarenessSystem`이 Hush/Awareness 중 이동 속도를 늦추는 데 사용.
  - `bool MovementLocked { get; set; }` — Emotions의 `EmotionSkill`(RewindSkill 채널링 등 `moveSpeedMultiplier == 0`인 경우)이 이동을 완전히 멈추는 데 사용.
  - `event System.Action<PlayerState> StateChanged` — 상태가 실제로 바뀔 때만(no-op 가드 있음) 발행.
  - `void ApplyKnockback(Vector2 direction, float force)` — 피격 넉백 적용, `_knockbackLockTimer`를 0.2초로 설정.
  - `void TeleportTo(Vector3 position)` — 위치 즉시 이동 + 속도 0 초기화(리스폰용).
- **동작**:
  - `Awake`: `Instance = this`, `_rb`/`_attack` 캐시, `_data = GameManager.Instance.Balance.player`, `_rb.gravityScale = _data.gravityScale`(3.5).
  - `OnEnable`/`OnDisable`: `_attack.Attacked`를 `HandleAttacked`에 구독/해제. `HandleAttacked`는 `_attackTimer = _data.attackActiveTime`(0.1s)로 설정 — 이 값이 `Attack` 상태 우선순위를 최상위로 유지하는 지속 시간이다.
  - `FixedUpdate` 순서:
    1. `UpdateGroundAndWallChecks()` — 접지/벽 판정 갱신.
    2. `_coyoteTimer`: 접지 중이면 `_data.coyoteTime`(0.1s)로 리필, 아니면 `Time.fixedDeltaTime`만큼 감소. `_jumpBufferTimer`: `PlayerInput.JumpPressed`면 `_data.jumpBufferTime`(0.1s)로 리필, 아니면 감소. 이어서 `_dashCooldownTimer`, `_knockbackLockTimer`, `_landTimer`, `_attackTimer`를 각각 0보다 클 때만 감소.
    3. `MovementLocked`면 수평 속도를 0으로 만들고(`_rb.linearVelocity = (0, y)`) `SetState(PlayerState.Idle)` 후 즉시 `return` — 그 아래 대시/이동/점프/벽 로직을 전부 건너뛴다.
    4. `UpdateDash()` 호출 후 `dashing = _dashTimer > 0f`.
    5. 벽점프 잠금(`wallJumpLocked = _wallJumpLockTimer > 0f`, 참이면 타이머 감소).
    6. `!dashing`일 때만: `!wallJumpLocked && _knockbackLockTimer <= 0f`이면 `ApplyHorizontalMovement()`(대시/벽점프 직후/넉백 직후에는 좌우 입력 무시) → `ApplyWallCling()`(결과를 `wallClinging`에 저장) → `TryJump(wallClinging)` → `ApplyVariableJumpCut()` → `ApplyFallGravity()`.
    7. `DetermineState(wallClinging)` 호출로 `PlayerState` 결정 및 `StateChanged` 발행.
  - `UpdateGroundAndWallChecks` (2026-07-26 재작업): `Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer)`로 `IsGrounded` 갱신. 벽은 `wallCheck` 로컬 오프셋을 좌우로 뒤집은 두 위치에서 **양쪽을 동시에** `OverlapBox`로 판정해 `_wallDir`(+1/-1/0)을 얻고 `IsOnWall = _wallDir != 0`. (예전에는 `Facing` 방향 한쪽만 판정해서, 벽점프로 반대편 벽에 날아가도 방향키를 다시 누르기 전까지 벽에 닿은 것으로 치지 않았다 — 핑퐁이 안 되던 핵심 원인.) 방금 착지했으면(`!wasGrounded && IsGrounded`) `_landTimer = 0.12f`(하드코딩 리터럴, `PlayerData`에 없음).
  - `UpdateDash`: 대시 중이 아니고 `PlayerInput.DashPressed`이고 쿨다운이 끝났으면 `_dashTimer = _data.dashDuration`(0.15s), `_dashCooldownTimer = _data.dashCooldown`(0.8s)로 시작. 대시 중이면 `_rb.linearVelocity = (Facing * dashDistance/dashDuration, 0)`(등속, 약 26.7유닛/초)로 고정하고 `gravityScale = 0`, 타이머 감소.
  - `ApplyHorizontalMovement` (2026-07-26 수정): `PlayerInput.Horizontal`이 0이 아니면 `Facing`을 부호로 갱신. **공중에서 입력이 없으면 수평 관성을 유지하고 리턴한다** (예전에는 매 프레임 0으로 덮어써서 벽점프 잠금이 풀리는 0.15초 시점에 비행이 뚝 끊겼다). 지상 또는 입력이 있으면 속도는 `(PlayerInput.RunHeld ? runSpeed(9) : walkSpeed(6)) * ExternalSpeedMultiplier`.
  - `ApplyWallCling` (2026-07-26 재작업, Celeste·Hollow Knight식 자동 벽잡기): 접지 중이거나 `_wallDir == 0`이거나 상승 중(`vy > 0`)이면 `false`. 벽 반대 방향을 밀고 있을 때만 놓아주고, **그 외에는 입력 없이도 자동으로 붙는다**. 붙으면 `Facing = _wallDir`(벽을 향해 봄), 수직 속도를 `-_data.wallSlideSpeed`(2)로 고정, `_wallCoyoteTimer = _data.wallCoyoteTime`(0.1)과 `_lastWallDir`을 갱신하고 `true` 반환.
  - `TryJump` (2026-07-26 재작업): `canWallJump = wallClinging || _wallCoyoteTimer > 0f`(벽 코요테). 점프 버퍼가 소진됐거나 (코요테 타임도 끝났고 `canWallJump`도 아니면) 리턴. 지상(코요테 포함) 점프가 우선이고, 공중에서 `canWallJump`면 `dir`(벽 코요테 시 `_lastWallDir`) 기준 `(-dir * wallJumpVelocity.x(9), wallJumpVelocity.y(13))`로 튕겨나가며 **`Facing = -dir`로 몸을 벽 반대편으로 돌리고** `_wallJumpLockTimer = _data.wallJumpLockTime`(0.15s) 설정. 어느 경우든 점프 버퍼·코요테 타이머를 0으로 소진.
  - `ApplyVariableJumpCut`: 상승 중(`vy > 0`)에 점프 키를 뗀 상태(`!JumpHeld`)면 수직 속도에 `_data.variableJumpCut`(0.5)를 곱해 짧게 끊는다.
  - `ApplyFallGravity`: 하강 중(`vy < 0`)이면 `gravityScale = gravityScale(3.5) * fallGravityMultiplier(1.6)`, 아니면 기본 `gravityScale`.
  - `DetermineState`: 우선순위대로 분기 — `_attackTimer > 0` → Attack, `_dashTimer > 0` → Dash, `_wallJumpLockTimer > 0` → WallJump, `!IsGrounded && wallClinging` → WallCling, `_landTimer > 0` → Land, `!IsGrounded && vy > 0 && 수평입력있음` → AirMove, `!IsGrounded && vy > 0` → Jump, `!IsGrounded && vy <= 0` → Fall, `IsGrounded && 수평입력 && RunHeld` → Run, `IsGrounded && 수평입력` → Walk, 그 외 → Idle. `SetState`는 값이 바뀔 때만 이벤트를 발행(no-op 가드).
  - `ApplyKnockback(direction, force)`: 속도를 `direction.normalized * force`로 즉시 덮어쓰고 `_knockbackLockTimer = 0.2f`(하드코딩 리터럴)로 설정해 잠시 좌우 입력을 무시시킨다.
  - `TeleportTo(position)`: 위치를 즉시 옮기고 속도를 0으로 초기화(리스폰 시 관성 잔류 방지).
  - **기획서 5.1 대비 검증**: 코요테 타임 0.1s, 점프 버퍼 0.1s, 벽 잠금 ~0.15s는 기획서 수치와 일치. 착지 유지 0.12초, 넉백 후 입력잠금 0.2초는 `PlayerData`가 아닌 코드 리터럴로 박혀 있으며 기획서에 명시된 값은 아니다(실제 코드 값 기준으로 문서화).

## PlayerHealth.cs

- **역할**: 플레이어 체력, 무적 시간, 피격 넉백·점멸 연출, 리스폰 수신을 담당한다.
- **상속/의존**: `MonoBehaviour`. `System.Collections`(코루틴), `HiddenWeight.Core`(`GameManager`) 사용. 리스폰 좌표 결정은 Core(`GameManager`)가 하고, 이 클래스는 `GameManager.Instance.RespawnRequested` 이벤트를 구독하기만 하는 역방향 훅 구조 — Core를 직접 호출해 좌표를 요청하지 않는다.
- **주요 멤버**:
  - private: `int _maxHealth`, `float _invulnerableTime`, `float _blinkInterval`, `float _knockbackForce`(모두 `Awake`에서 `PlayerData`로부터 캐시), `SpriteRenderer _sprite`, `Coroutine _blinkRoutine`.
  - `int Current { get; private set; }` — 초기값 `_maxHealth`.
  - `int Max => _maxHealth`.
  - `bool IsInvulnerable { get; private set; }`.
  - `event System.Action<int, int> HealthChanged` — `(현재체력, 최대체력)`로 발행.
  - `event System.Action Damaged` (2026-07-26 추가) — 실제로 피해가 들어간 순간 발행(무적으로 흡수된 피격 제외). Emotions의 `RewindSkill`이 구독해 채널링 중 피격 캔슬(기획서 EMOTION_SYSTEM 1.2절)에 쓴다.
  - `void GrantInvulnerability(float seconds)` (2026-07-26 추가) — 피격 외의 이유로 짧은 무적을 거는 공개 API. 이미 더 긴 무적이 돌고 있으면 남은 시간을 줄이지 않는다(`_invulnRemaining = max(...)`). `HushSkill`이 숨죽이기 해제 무적 0.2초(2.2절)에 쓴다.
  - `void TakeDamage(int amount, Vector2 sourcePosition)` — 외부(적, 함정 등)에서 호출하는 피해 진입점.
  - `void RestoreFull()` — 체력을 최대치로 회복하고 이벤트 발행.
- **동작**:
  - `Awake`: `GameManager.Instance.Balance.player`에서 `maxHealth(3)`, `invulnerableTime(0.8s)`, `blinkInterval(0.1s)`, `knockbackForce(8)`를 캐시. `_sprite = GetComponentInChildren<SpriteRenderer>()`. `Current = _maxHealth`.
  - `OnEnable`: `GameManager.Instance.RespawnRequested += HandleRespawn`.
  - `OnDisable`: `GameManager.Instance != null`일 때만 구독 해제 — 종료/파괴 순서상 `GameManager`가 먼저 사라져 있을 수 있어 null 체크 후 해제한다는 주석이 있다.
  - `TakeDamage`: `IsInvulnerable`이면 즉시 리턴(무적 중 피해 무효). 아니면 `Current`를 `amount`만큼 깎되 0 미만으로 내려가지 않게(`Mathf.Max(0, ...)`), `HealthChanged` 발행. `sourcePosition` 반대 방향으로 `PlayerController.Instance.ApplyKnockback(direction, _knockbackForce)` 호출. 이어서 `StartInvulnerability()` 시작. `Current <= 0`이면 게임오버 화면 없이 **`GameManager.Instance.RespawnPlayer()`를 호출**한다(플레이어→Core 방향 호출). `GameManager.RespawnPlayer()`는 내부적으로 `RespawnRequested?.Invoke(Progress.LastCheckpoint)`를 발행하고, 그것을 이 클래스의 `HandleRespawn`이 구독해서 받는다 — 즉 "피격 사망 통지"는 Player→Core로 직접 호출하지만, "리스폰 좌표 결정과 실제 이동 지시"는 Core→Player 이벤트로 되돌아오는 왕복 구조다.
  - `RestoreFull`: `Current = _maxHealth` 후 `HealthChanged` 발행.
  - `HandleRespawn(Vector3 position)`: `PlayerController.Instance.TeleportTo(position)` 호출 후 `RestoreFull()` — 위치 이동과 체력 회복을 함께 처리.
  - `StartInvulnerability(duration)` (2026-07-26 재작업): `_invulnRemaining = Mathf.Max(_invulnRemaining, duration)`로 남은 시간만 갱신하고, 코루틴이 없을 때만 새로 시작 — 피격 무적(0.8s)과 숨죽이기 해제 무적(0.2s)이 겹쳐도 긴 쪽이 유지된다.
  - `InvulnerabilityRoutine`: `IsInvulnerable = true`로 진입, `_invulnRemaining`이 소진될 때까지 `_blinkInterval`(0.1s)마다 `_sprite.enabled`를 토글(점멸)하며 차감. 종료 시 `_sprite.enabled = true`로 강제 복구, `IsInvulnerable = false`, `_blinkRoutine = null`.

## VoidRespawn.cs (2026-07-26 신규)

- **역할**: 맵 경계 밖 허공으로 떨어졌을 때의 소프트락 방지. 일정 깊이 아래로 내려가면 마지막 체크포인트로 리스폰시킨다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`)에 의존. `PrefabBuilder`가 Player 프리팹에 부착한다.
- **주요 멤버**: `[SerializeField] float voidY = -15f` — 지역의 "추락 시 안전 바닥"들이 y -8 부근이므로 그보다 훨씬 아래로 잡아 정상 플레이와 겹치지 않는다.
- **동작**: `Update`에서 `transform.position.y < voidY`이고 `GameManager.Instance`가 있으면 `GameManager.Instance.RespawnPlayer()` 호출 — 리스폰 좌표 결정·이동은 기존 `RespawnRequested` 이벤트 왕복 구조(위 `PlayerHealth` 참고)를 그대로 탄다. 씬 좌우의 보이지 않는 경계벽(`ZoneSceneBuilder.BuildBoundary`)이 1차 방어선이고 이 컴포넌트는 안전망이다.

## PlayerInput.cs

- **역할**: 모든 키 입력을 이 static 클래스 한 곳에만 모은다. 다른 스크립트는 `UnityEngine.Input`을 직접 호출하지 않고 반드시 이 클래스를 통해서만 입력을 읽는다.
- **상속/의존**: 없음(순수 static 클래스). `UnityEngine`(`Input`, `KeyCode`)만 사용.
- **주요 멤버** (모두 `static`):
  - `bool Enabled { get; set; } = true` — 입력 게이트. 일시정지 화면에서도 Escape로 해제할 수 있어야 하고, Ending 시퀀스에서도 자각(L) 입력만은 받아야 하므로 아래 두 멤버는 이 게이트를 무시한다.
  - `float Horizontal => Enabled ? Input.GetAxisRaw("Horizontal") : 0f`.
  - `bool RunHeld => Enabled && Input.GetKey(KeyCode.LeftShift)`.
  - `bool JumpPressed => Enabled && Input.GetKeyDown(KeyCode.Space)`.
  - `bool JumpHeld => Enabled && Input.GetKey(KeyCode.Space)`.
  - `bool DashPressed => Enabled && Input.GetKeyDown(KeyCode.LeftControl)`.
  - `bool AttackPressed => Enabled && Input.GetKeyDown(KeyCode.J)`.
  - `bool SkillPressed => Enabled && Input.GetKeyDown(KeyCode.K)`.
  - `bool SkillHeld => Enabled && Input.GetKey(KeyCode.K)`.
  - `bool AwarenessHeld => Input.GetKey(KeyCode.L)` — **`Enabled`와 무관하게 항상 동작**. Ending 시퀀스가 `PlayerInput.Enabled = false`로 이동/공격을 잠가둔 채로도 자각 홀드만은 계속 읽을 수 있어야 하기 때문.
  - `bool PausePressed => Input.GetKeyDown(KeyCode.Escape)` — **`Enabled`와 무관하게 항상 동작**. 일시정지 해제(Esc) 자체가 막히면 게임이 멈춘 채 풀리지 않으므로.
- **동작**: 전부 프로퍼티 getter 한 줄로 구현된 순수 입력 폴링이며 내부 상태나 코루틴은 없다. `Enabled = false`가 되면 `Horizontal`/`RunHeld`/`JumpPressed`/`JumpHeld`/`DashPressed`/`AttackPressed`/`SkillPressed`/`SkillHeld` 8개는 전부 0/`false`로 고정되고, `AwarenessHeld`(키: L)와 `PausePressed`(키: Escape)만 게이트를 우회해 실제 키 상태를 그대로 반환한다.

## PlayerState.cs

- **역할**: `PlayerController`가 매 프레임 계산해 갖는 상태를 나타내는 열거형. 별도의 상태 로직이나 클래스가 아니라 순수 `enum` 하나뿐이다.
- **상속/의존**: 없음(순수 enum, `HiddenWeight.Player` 네임스페이스만 사용).
- **주요 멤버**:
  - `enum PlayerState { Idle, Walk, Run, Jump, AirMove, Fall, Land, Attack, Dash, WallCling, WallJump }` — 선언 순서대로 0~10.
- **동작**: 값 자체에 로직은 없다. `PlayerAnimator.HandleStateChanged`가 `(int)state`를 그대로 Animator 정수 파라미터에 넘기므로, 선언 순서를 바꾸면 Animator 컨트롤러의 트랜지션 조건(정수 비교)이 어긋난다 — 순서 변경 시 Animator 쪽도 함께 수정해야 한다는 점이 주석으로 명시되어 있다.
