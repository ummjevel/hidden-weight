# Player 모듈 — 플레이어 이동·전투·생존 상태머신

> 기획서 5.1(PlayerController), 5.4(전투) 대응.
> 입력은 `PlayerInput` static 클래스 하나로만 읽고, 튜닝 수치는 전부 `Data.PlayerData`
> (`GameManager.Instance.Balance.player`)에서 읽어온다. Player는 Core(GameManager)와
> Data(PlayerData/BalanceData)에만 의존하며, World는 `IDamageable` 계약 하나만 예외적으로
> 참조한다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| PlayerController.cs | 이동/점프/대시/벽클링/벽점프 상태머신, `PlayerState` 계산·전이 | 5.1 |
| PlayerInput.cs | 전 키 입력을 한 곳에 모은 static 게이트 (`Enabled`) | 5.1 |
| PlayerState.cs | `PlayerController`가 갖는 상태 enum 정의 | 5.1 |
| PlayerAnimator.cs | `StateChanged` 이벤트 → Animator 정수 파라미터, 스프라이트 좌우 반전 | 5.1 |
| PlayerAttack.cs | 근접 공격 판정(부채꼴 OverlapCircle), `IDamageable`에 피해 적용 | 5.4 |
| PlayerHealth.cs | HP, 무적 시간, 피격 넉백·점멸, 리스폰 수신. `Damaged` 이벤트(되감기 피격 캔슬용)와 `GrantInvulnerability(초)`(숨죽이기 해제 무적용) 공개 | 5.4 |
| VoidRespawn.cs | 맵 경계 밖 낙하(y < -15) 시 마지막 체크포인트로 리스폰 — 무한 낙하 소프트락 방지 | (2026-07-26 추가) |

## 핵심 규칙 구현

- **이동**: 걷기 6, 달리기(Shift) 9 (`walkSpeed`/`runSpeed`), 여기에 `ExternalSpeedMultiplier`(기본 1)가 곱해진다.
- **점프**: `jumpVelocity` 14, 중력 `gravityScale` 3.5, 하강 시 `fallGravityMultiplier` 1.6배 가중. 코요테 타임 `coyoteTime` 0.1s, 점프 버퍼 `jumpBufferTime` 0.1s — 기획서 5.1 수치와 일치. 가변 점프는 상승 중 점프 키를 떼면 `variableJumpCut` 0.5배로 수직 속도를 잘라낸다.
- **대시**: `dashDuration` 0.15s 동안 `dashDistance / dashDuration`(≈26.7 유닛/초)의 등속 이동, 중력 0, 쿨다운 `dashCooldown` 0.8s.
- **벽 (2026-07-26 표준 방식으로 재작업 — Celeste·Hollow Knight식)**: 양쪽 벽을 동시에 판정(`_wallDir` +1/-1/0)하고, **낙하 중 벽에 닿으면 방향키 없이 자동으로 붙는다**(벽 반대 방향을 밀 때만 떼짐, 붙으면 벽을 향해 봄). 벽 슬라이드 `wallSlideSpeed` 2, 벽점프 속도 `wallJumpVelocity` (9, 13) — 점프 시 몸이 자동으로 벽 반대편을 향하고 좌우 입력 잠금 `wallJumpLockTime` 0.15s. 벽에서 떨어진 직후에도 `wallCoyoteTime` 0.1s 안에는 벽점프 허용. 공중에서 입력이 없으면 수평 관성을 유지한다(예전에는 0으로 덮어써서 벽점프 비행이 뚝 끊겼음). 결과적으로 굴뚝 구간은 Space만 번갈아 눌러도 오를 수 있다.
- **그 외 하드코딩 리터럴** (PlayerData에 없고 코드에 상수로 박혀 있음, 기획서엔 명시 안 됨): 착지 직후 Land 상태 유지 0.12초, 피격 넉백 후 좌우 입력 무시 0.2초.
- **공격**(5.4): 반경 `attackRadius` 1.2, 부채꼴 각도 `attackAngle` 90도, 피해 `attackDamage` 1, 쿨다운 `attackCooldown` 0.35s, 판정(Attack 상태 유지) 시간 `attackActiveTime` 0.1s.
- **생존**: 최대 체력 `maxHealth` 3, 무적 시간 `invulnerableTime` 0.8s(그 사이 `blinkInterval` 0.1s 간격으로 점멸), 피격 넉백 `knockbackForce` 8.
- **상태머신**: `PlayerState` = Idle/Walk/Run/Jump/AirMove/Fall/Land/Attack/Dash/WallCling/WallJump 11개, 기획서 5.1과 동일. 우선순위는 Attack > Dash > WallJump > WallCling > Land > AirMove > Jump > Fall > Run > Walk > Idle 순으로 `PlayerController.DetermineState`에서 매 FixedUpdate 결정된다.

## 씬 배치

- 플레이어 프리팹 루트에 `Rigidbody2D` + `CapsuleCollider2D`(`PlayerController`가 `[RequireComponent]`로 강제), `PlayerController`, `PlayerAttack`, `PlayerHealth`, `PlayerAnimator`, 자식에 `SpriteRenderer`(+ `Animator`, 있으면)를 둔다.
- `PlayerController`에 접지 판정용 `groundCheck`, 벽 판정용 `wallCheck` 빈 Transform을 자식으로 만들어 인스펙터에 연결하고, `groundLayer`/`wallLayer` LayerMask를 지정한다. `PlayerAttack`의 `enemyLayer`에는 Enemy 레이어를 지정한다.
- 레이어: 평상시 플레이어 GameObject는 `Player` 레이어. Emotions의 HushSkill이 숨죽이기 중 레이어를 `PlayerHushed`로 바꿔치기하므로(Player 모듈 코드 자체에는 없음, Emotions 쪽 책임) 레이어 충돌 매트릭스에서 `PlayerHushed`↔`Enemy`는 충돌하지 않도록, 그리고 `GazeHazard`의 감지 레이어에는 `PlayerHushed`를 포함하지 않도록 프로젝트 설정이 되어 있어야 한다(`Editor/ProjectSetup.cs`, `Editor/PrefabBuilder.cs` 참고).

## 다른 모듈과의 연결

- **Emotions → Player**: `EmotionSkill`/`AwarenessSystem`이 `PlayerController.ExternalSpeedMultiplier`를 읽고 써서 Hush/Awareness 중 이동 속도를 늦추고, `moveSpeedMultiplier == 0`일 때 `PlayerController.MovementLocked`를 켜서 이동을 완전히 멈춘다(예: RewindSkill 채널링). `HushSkill`은 `PlayerAttack.CanAttack`을 false로 내려 숨죽이기 중 공격을 막는다. Player는 이 프로퍼티들을 노출만 할 뿐 Emotions를 참조하지 않는다.
- **Player → World 예외**: `PlayerAttack`은 World 모듈 중 `World/Interactions.cs`에 정의된 `IDamageable` 계약 하나만 참조한다. `Interactions.cs`는 어떤 모듈에도 의존하지 않는 순수 계약 파일이라, 이를 참조해도 기획서 3.1절이 규정한 모듈 의존 방향(World가 Player를 모르는 방향)은 깨지지 않는다. Enemies 모듈의 실제 `Enemy` 구현 타입은 절대 참조하지 않는다.
- **Core**: `PlayerHealth`, `PlayerAttack`, `PlayerController`는 모두 `GameManager.Instance.Balance.player`(`PlayerData`)로 튜닝 수치를 읽는다. 리스폰은 역방향 훅 구조 — `PlayerHealth`는 HP 0 시 `GameManager.Instance.RespawnPlayer()`를 호출만 하고, 실제 좌표 결정과 통지는 `GameManager`가 `RespawnRequested` 이벤트(payload: 마지막 체크포인트 `Vector3`)를 발행해서 되돌려준다. `PlayerHealth.HandleRespawn`이 이를 구독해 `PlayerController.Instance.TeleportTo`와 `RestoreFull`을 호출한다. Player는 Core를 직접 호출해 좌표를 요청하지 않는다.

## 의존성 주의

- 새 스크립트에서 키 입력이 필요하면 반드시 `PlayerInput`을 거칠 것 — `Input.Get*`를 직접 호출하면 `Enabled` 게이트(일시정지/엔딩 시퀀스 이동 잠금)를 우회하게 되어 버그가 난다.
- `PausePressed`와 `AwarenessHeld`는 의도적으로 `Enabled` 게이트를 무시한다. 이 둘을 게이트에 새로 묶으면 일시정지 해제 불능, 엔딩 중 자각 입력 불능 버그가 생긴다.
- `PlayerAttack`/`PlayerController`에 Enemies 관련 코드를 추가할 때 `Enemy` 구체 타입을 직접 참조하지 말 것 — 반드시 `IDamageable`을 통해서만 상호작용한다.
- `PlayerController.Instance`, `groundCheck`/`wallCheck` 인스펙터 필드가 비어 있으면 `NullReferenceException`이 나므로 프리팹 설정을 빠뜨리지 않는다.
