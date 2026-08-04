# HiddenWeight 음향 적용 가이드

작성 기준: 2026-07-31  
정제 음원: `/Users/ksh/Desktop/sound/Selected_Refined`  
Unity 프로젝트: `/Users/ksh/Desktop/NHN HACKERton/HiddenWeight`

## 1. 결론

전역에서 자주 쓰는 짧은 효과음은 아래 규칙으로 넣는다.

```text
Assets/Resources/Audio/SFX/<SfxCue 열거형 이름>/<파일명>.wav
```

`AudioManager.ResolveSfx()`가 이 경로를 자동으로 읽는다. 같은 폴더에 WAV를 여러 개 넣으면
직전 파일을 피해서 무작위 재생한다.

환경음, 체크포인트 대기음, 적 대기음, 돌진음처럼 시작과 정지가 필요한 루프는 전역
`AudioManager`의 단일 `_loopSource`를 공유하면 안 된다. 해당 오브젝트에 로컬 `AudioSource`를
붙이고 상태가 시작될 때 `Play()`, 끝날 때 `Stop()`한다. 환경음은 스테레오, 나머지는 모노를
기본으로 한다.

## 2. 먼저 고쳐야 하는 코드 정합성

현재 `AudioManager.cs`의 `SfxCue`에는 다음 이름이 빠져 있지만 다른 코드가 이미 사용한다.

```text
AttackHit, WallJump, Land, WallGrab, WallSlide,
FootstepWalk, FootstepRun, Death, Respawn, Heal,
ItemPickup, Reward
```

`AudioManager.ResolveSfx()` 자체도 `WallJump`를 참조하므로, 음원을 복사하기 전에
`Assets/Scripts/Core/AudioManager.cs`의 `SfxCue`에 위 이름을 다시 추가해야 한다.

현재 코드가 직접 사용하는 전체 공용 큐 권장 목록:

```csharp
public enum SfxCue
{
    UiConfirm,
    Checkpoint,
    Fragment,
    Ability,
    Attack,
    AttackHit,
    Jump,
    WallJump,
    Dash,
    Land,
    FootstepWalk,
    FootstepRun,
    WallGrab,
    WallSlide,
    Hurt,
    Death,
    Respawn,
    Heal,
    ItemPickup,
    Reward,
    RewindStart,
    RewindComplete,
    ShortcutOpen,
    EnemyHit,
    EnemyDeath,
    BossTelegraph,
    BossPhase,
    BossVictory
}
```

## 3. 이미 코드에 연결된 공용 효과음

아래 표대로 WAV를 복사하면 추가 코드 없이 기존 호출이 실제 파일을 사용한다.

| 기능 | 원본 음원 | Unity 목적지 | 이미 호출하는 코드와 시점 |
|---|---|---|---|
| 공격 휘두르기 | Kenney CC0 범용 칼날·천 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/Attack/` | `PlayerAttack.Attack()` 공격 시작 |
| 공격 적중 | Kenney CC0 범용 타격 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/AttackHit/` | `PlayerAttack.Attack()`에서 실제 적중 후 |
| 점프 | Kenney CC0 범용 천·가벼운 충격 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/Jump/` | `PlayerController.Jump()` |
| 벽점프 | `Selected_Refined/Player/SFX_PLAYER_WALL_JUMP/...wav` | `Assets/Resources/Audio/SFX/WallJump/` | `PlayerController.Jump()`의 `isWallJump` 분기 |
| 대시 | `sound/HiddenWeight_Unity_SFX/Player_Dash_01.wav` | `Assets/Resources/Audio/SFX/Dash/` | `PlayerController` 대시 시작 |
| 일반 착지 | Kenney CC0 범용 콘크리트·천 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/Land/` | `PlayerController.SetState(Land)` |
| 걷기 | Kenney CC0 콘크리트 발소리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/FootstepWalk/` | `PlayerController` 이동 타이머 |
| 달리기 | Kenney CC0 콘크리트 발소리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/FootstepRun/` | `PlayerController` 이동 타이머 |
| 벽 잡기 | Kenney CC0 콘크리트·천 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/WallGrab/` | `PlayerController.SetState(WallCling)` 진입 |
| 벽 미끄러짐 | `sound/HiddenWeight_Unity_SFX/Player_Wall_Slide_Loop_01.wav` | `Assets/Resources/Audio/SFX/WallSlide/` | `PlayerController` 벽 상태 진입/종료 |
| 플레이어 피격 | Kenney CC0 무음성 몸·천 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/Hurt/` | `PlayerHealth.TakeDamage()` |
| 플레이어 사망 | Kenney CC0 무음성 몸·천·가죽 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/Death/` | `PlayerHealth.TakeDamage()`에서 체력 0 |
| 부활 | `Selected_Refined/Player/SFX_PLAYER_RESPAWN/...wav` | `Assets/Resources/Audio/SFX/Respawn/` | `PlayerHealth.Respawn()` |
| 회복 | `Selected_Refined/Player/SFX_PLAYER_HEAL/...wav` | `Assets/Resources/Audio/SFX/Heal/` | `HealingPickup.OnTriggerEnter2D()` |
| 일반 아이템 | `Assets/Resources/Audio/SFX/ItemPickup/`의 기존 CC0 | 같은 폴더 유지 | `CurrencyPickup.OnTriggerEnter2D()` |
| 보상 상자 | `Assets/Resources/Audio/SFX/Reward/`의 기존 CC0 | 같은 폴더 유지 | `RewardChest.Open()` |
| 체크포인트 | `Selected_Refined/World/SFX_CHECKPOINT_ACTIVATE/...wav` | `Assets/Resources/Audio/SFX/Checkpoint/` | `Checkpoint.OnTriggerEnter2D()` |
| 기억 파편 | `Selected_Refined/World/SFX_MEMORY_FRAGMENT_PICKUP/...wav` | `Assets/Resources/Audio/SFX/Fragment/` | `StoryFragment.OnTriggerEnter2D()` |
| UI 확정 | `Assets/Resources/Audio/SFX/UiConfirm/`의 기존 CC0 | 같은 폴더 유지 | `UIBuilder.CreateButton()` 클릭 |
| 숏컷 개방 | `Selected_Refined/World/SFX_GATE_UNLOCK/...wav` | `Assets/Resources/Audio/SFX/ShortcutOpen/` | `Shortcut.Open()` |
| 일반 적 피격 | Kenney CC0 범용 타격 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/EnemyHit/` | `Enemy.TakeDamage()` |
| 일반 적 사망 | Kenney CC0 범용 목재·둔탁 충격 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/EnemyDeath/` | `Enemy.TakeDamage()` 체력 0 |
| 보스 예고 | Kenney CC0 짧은 천·기계 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/BossTelegraph/` | `BossController.PerformMove()` |
| 보스 페이즈 | `SFX_INSTRUCTOR_PHASE_RUPTURE` | `Assets/Resources/Audio/SFX/BossPhase/` | `BossController.UpdatePhase()` |
| 보스 승리 | Kenney CC0 범용 붕괴 충격 폴리(2026-08-02 교체) | `Assets/Resources/Audio/SFX/BossVictory/` | `Encounter.Victory()` |

`PlayerController`의 발소리는 현재 애니메이션 프레임이 아니라 시간 간격으로 재생된다. 우선은
그대로 적용하고, 나중에 `SpriteAnimator.FrameDisplayed`를 구독해 발 접촉 프레임으로 옮기는
것이 정확하다.

## 4. 플레이어 추가 음향

| 음원 | 붙일 파일·메서드 | 재생 방법 |
|---|---|---|
| `SFX_PLAYER_DASH_END` | `PlayerController.cs`, 대시 타이머가 0이 되는 순간 | 원샷 |
| `SFX_PLAYER_LAND_HEAVY` | 현재 강한 착지 판정이 없으므로 연결하지 않음 | 보관 |
| `SFX_PLAYER_LOW_HEALTH_LOOP` | `PlayerHealth.cs`, 체력 1 진입/이탈 | 플레이어 로컬 루프 |
| `SFX_PLAYER_STEP_METAL` | `PlayerController.cs`, 발밑 표면이 Metal일 때 | 걸음마다 원샷 |
| `SFX_PLAYER_STEP_FINGER` | `PlayerController.cs`, 발밑 표면이 Finger/Fiber일 때 | 걸음마다 원샷 |

사슬 다리 발소리는
`/Users/ksh/Desktop/sound/CC0_Inbox/Footsteps_ChainBridge/445790__ddt197__wooden-chain-bridge.wav`
를 정제한 뒤 표면 분기 `Bridge`에 연결한다. 표면 판별 시스템이 생기기 전에는 돌 발소리만
유지한다.

## 5. 되감기 음향

현재 `RewindSkill.cs`에는 시작과 성공만 직접 연결돼 있다. 아래처럼 확장한다.

| 음원 | 붙일 위치 | 정확한 시점 |
|---|---|---|
| `SFX_REWIND_TARGET_FOUND` | `RewindSkill.FindNearestTarget()` 결과 변경 처리 | 대상이 null에서 유효 대상으로 바뀔 때 |
| `SFX_REWIND_TARGET_SWITCH` | 같은 위치 | 유효 대상 A에서 B로 바뀔 때 |
| `SFX_REWIND_CHANNEL_START` | `RewindSkill.OnBegin()` | 대상 확정 후 채널링 시작 |
| `SFX_REWIND_CHANNEL_LOOP` | `RewindSkill.OnBegin()` / `OnEnd()` | 시작 후 로컬 루프, 성공·취소 때 정지 |
| `SFX_REWIND_THRESHOLD` | `RewindSkill.OnTick()` | 완료 직전 임계값을 한 번 넘을 때 |
| `SFX_REWIND_CANCEL` | `RewindSkill.OnEnd()` | 피격 또는 키 해제로 완료 전에 종료 |
| `SFX_REWIND_NO_TARGET` | `RewindSkill.OnBegin()` | 대상이 없어 즉시 종료할 때 |
| `SFX_REWIND_COMPLETE` | `RewindSkill.OnTick()` | `target.Rewind()` 직후 |
| `SFX_REWIND_READY` | `RewindSkill.cs` | 쿨타임이 끝나는 순간. 현재 쿨타임 알림이 없다면 보류 |
| `SFX_REWIND_STONE` | `Rewindable.Rewind()` | 일반 석재 오브젝트 복원 |
| `SFX_REWIND_BRIDGE` | 다리용 `Rewindable` 또는 `ResidueLoopRuntime` | 다리 복원 연출 시작 |
| `SFX_REWIND_CHAIN` | 사슬 장치 복원 스크립트 | 사슬이 되감기 시작할 때 |
| `SFX_REWIND_GATE` | `Gate` 복원/개방 처리 | 문 구조가 복원될 때 |
| `SFX_REWIND_LIFT` | `LiftPlatform` 복원 처리 | 승강기 기구가 복원될 때 |

일반 `Rewindable`만으로 오브젝트 재질을 구분할 수 없으므로, `Rewindable`에 복원 음향 종류를
직렬화하거나 전용 하위 컴포넌트를 붙여야 한다.

## 6. UI 음향

| 음원 | 붙일 파일·이벤트 |
|---|---|
| `SFX_UI_MOVE` | `UIBuilder.cs`에서 버튼 `Select` 이벤트 또는 `EventTrigger` |
| `SFX_UI_CANCEL` | `ConfirmDialog.cs` 취소, `PauseMenu.cs` 뒤로가기 |
| `SFX_UI_ACTION_DENIED` | 비활성 버튼, 사용할 수 없는 스킬·문 |
| `SFX_UI_PAUSE` | `PauseMenu.Open()` |
| `SFX_UI_UNPAUSE` | `PauseMenu.Close()` |
| `SFX_UI_MAP_OPEN` | `PauseSectionPanel` 지도 탭을 열 때 |
| `SFX_UI_MAP_CLOSE` | 지도 탭을 닫거나 다른 탭으로 이동할 때 |
| `SFX_UI_FRAGMENT_RECORDED` | `StoryFragment` 텍스트가 기록 목록에 추가된 직후 |
| `SFX_UI_ROOM_DISCOVERED` | `RoomCamera.RoomChanged`에서 최초 방문 방일 때 |

UI 큐는 `SfxCue`에 위 이름을 추가하고 같은 이름의 Resources 폴더를 만들거나, UI 전용
`AudioClip` 필드로 연결한다. 전역 설정과 무작위 피치 정책을 공유하려면 `SfxCue` 방식이 낫다.

## 7. 월드·장치·아이템

| 음원 | 붙일 파일·메서드 | 시점 |
|---|---|---|
| `SFX_CHECKPOINT_ACTIVATE` | `Checkpoint.OnTriggerEnter2D()` | 처음 활성화 |
| `SFX_CHECKPOINT_HEAL` | `Checkpoint.OnTriggerEnter2D()` | 전체 회복 직후 |
| `SFX_CHECKPOINT_IDLE_LOOP` | 체크포인트 프리팹 로컬 `AudioSource` | 활성 체크포인트만 루프 |
| `SFX_CURRENCY_PICKUP` | `CurrencyPickup.OnTriggerEnter2D()` | 획득 |
| `SFX_CURRENCY_IDLE` | 재화 프리팹 로컬 `AudioSource` | 화면 근처에 있을 때만 루프 |
| `SFX_HEALING_PICKUP` | `HealingPickup.OnTriggerEnter2D()` | 회복 적용 |
| `SFX_HEALTH_SHARD_PICKUP` | `RewardChest.Open()` | `healthShard == true` |
| `SFX_MEMORY_FRAGMENT_IDLE` | 기억 파편 프리팹 로컬 `AudioSource` | 미획득 상태 루프 |
| `SFX_MEMORY_FRAGMENT_PICKUP` | `StoryFragment.OnTriggerEnter2D()` | 기록 직전 |
| `SFX_GATE_UNLOCK` | `Shortcut.Open()` 또는 조건 충족 순간 | 잠금 해제 |
| `SFX_GATE_OPEN` | `Gate.cs`에 상태 변화 감지 추가 | 닫힘→열림 |
| `SFX_GATE_CLOSE` | `Gate.cs`에 상태 변화 감지 추가 | 열림→닫힘 |
| `SFX_LIFT_START` | `LiftPlatform.OnCollisionEnter2D()` | `_leg = 0` 직후 |
| `SFX_LIFT_LOOP` | `LiftPlatform.FixedUpdate()` 상태 | 이동 중 로컬 루프 |
| `SFX_LIFT_STOP` | `LiftPlatform.FixedUpdate()` | `_finished = true` 직전 |
| `SFX_FLOOR_CRACK` | `CrumblingPlatform.CrumbleRoutine()` | `PlatformCrack` 시작 |
| `SFX_FLOOR_COLLAPSE` | `CrumblingPlatform.CrumbleRoutine()` | 콜라이더 비활성화 순간 |
| `SFX_BRIDGE_SETTLE` | 다리 복원 완료 처리 | 마지막 조각 고정 후 |
| `SFX_CRUSHER_WARNING` | 분쇄기 위험 코루틴 | 공격 전 예고 시작 |
| `SFX_CRUSHER_IMPACT` | 분쇄기 위험 코루틴 | 바닥 충돌 순간 |
| `SFX_SPIKES_WARNING` | 가시 위험 코루틴 | 바닥 경고 시작 |
| `SFX_SPIKES_EXTEND` | 가시 위험 코루틴 | 판정 활성화 순간 |
| `SFX_TENDRIL_WARNING` | 촉수 위험 코루틴 | 재/먼지가 안쪽으로 모일 때 |
| `SFX_TENDRIL_ATTACK` | 촉수 위험 코루틴 | 판정 활성화 순간 |
| `SFX_SECRET_EYE_OPEN` | `AwarenessRevealed` 또는 비밀 눈 오브젝트 | 자각으로 눈이 열릴 때 |
| `SFX_SECRET_EYE_REVEAL` | `HiddenFragment`/비밀 방 처리 | 숨겨진 경로가 확정 노출될 때 |

분쇄기·가시·촉수는 현재 전용 런타임 클래스보다 에디터 빌더에서 `Hazard`와 애니메이션을
조합하는 부분이 많다. 빌더에 직접 재생 코드를 넣지 말고, 프리팹에 재사용 가능한
`HazardAudioEmitter`를 붙여 `Warning`과 `Impact` 두 이벤트를 받게 한다.

## 8. 일반 적

공통 `Enemy.cs`의 `EnemyHit`/`EnemyDeath`만 사용하면 모든 적이 같은 소리를 낸다. `Enemy`에
종류별 오디오 프로필을 직렬화하거나, `clipPrefix`에 따라 큐를 선택하는 방식으로 분리한다.

### 보행자

| 음원 | 붙일 위치 |
|---|---|
| `SFX_WALKER_IDLE` | `EnemyPatrol`, 정지 상태의 간헐 원샷 |
| `SFX_WALKER_STEP` | `EnemyPatrol` 또는 `SpriteAnimator.FrameDisplayed`, 발 접촉 프레임 |
| `SFX_WALKER_NOTICE` | `RangedAttackBehavior`, 최초 감지 |
| `SFX_WALKER_TELEGRAPH` | `RangedAttackBehavior.ThrowRoutine()` 예고 시작 |
| `SFX_WALKER_ATTACK` | 같은 코루틴의 `ProjectileSpawner.Fire()` 직전 |
| `SFX_WALKER_HIT` | `Enemy.TakeDamage()` |
| `SFX_WALKER_DEATH` | `Enemy.TakeDamage()` 체력 0 |

### 애도 운반자

| 음원 | 붙일 위치 |
|---|---|
| `SFX_CARRIER_IDLE` | `ChargerBehavior`, Idle 상태 간헐 원샷 |
| `SFX_CARRIER_STEP` | 이동 애니메이션 발 접촉 프레임 |
| `SFX_CARRIER_NOTICE` | 최초 `DistanceToPlayer <= detectRange` |
| `SFX_CARRIER_CHARGE_TELEGRAPH` | `ChargeRoutine()`의 `Phase.Telegraph` 진입 |
| `SFX_CARRIER_CHARGE_LOOP` | `Phase.Charge` 동안 오브젝트 로컬 루프 |
| `SFX_CARRIER_WALL_IMPACT` | `Physics2D.Raycast`가 벽을 감지한 순간 |
| `SFX_CARRIER_DEATH` | `Enemy.TakeDamage()` 체력 0 |

### 매달린 손가락

| 음원 | 붙일 위치 |
|---|---|
| `SFX_FINGER_IDLE` | `AmbusherBehavior`, 천장 대기 중 간헐 원샷 |
| `SFX_FINGER_NOTICE` | `DropRoutine()` 시작 |
| `SFX_FINGER_DROP_TELEGRAPH` | 그림자 표시 직후 |
| `SFX_FINGER_DROP` | `Body.bodyType = Dynamic` 직후 |
| `SFX_FINGER_IMPACT` | 바닥 충돌 이벤트 추가 후 최초 충돌 |
| `SFX_FINGER_CRAWL` | 착지 후 이동 발 접촉 프레임 |
| `SFX_FINGER_DEATH` | `Enemy.TakeDamage()` 체력 0 |

### 굳은 잔재

| 음원 | 붙일 위치 |
|---|---|
| `SFX_HARDENED_IDLE` | `GuardBehavior`, 대기 상태 간헐 원샷 |
| `SFX_HARDENED_STEP` | 이동 발 접촉 프레임 |
| `SFX_HARDENED_NOTICE` | 플레이어 최초 감지 |
| `SFX_HARDENED_TELEGRAPH` | `AttackRoutine()` 시작 |
| `SFX_HARDENED_ATTACK` | 충격파 발사 직전 |
| `SFX_HARDENED_BLOCK` | `Enemy.TakeDamage()`의 `guard.BlocksFrom()` 분기 |
| `SFX_HARDENED_DEATH` | `Enemy.TakeDamage()` 체력 0 |

## 9. 보스

보스별 음향은 `BossController`에 직렬화된 오디오 프로필을 추가하는 것이 안전하다. 공용
`BossTelegraph` 하나에 모든 파일을 섞으면 손목 감시자와 교수자의 소리가 무작위로 뒤바뀐다.

### 손목의 감시자

| 음원 | 붙일 위치 |
|---|---|
| `SFX_WRIST_INTRO` | `BossController.OnEnable()` 전투 시작 지연 전 |
| `SFX_WRIST_IDLE` | 회복 구간 로컬 루프 또는 간헐 원샷 |
| `SFX_WRIST_STEP` | 보행/접근 발 접촉 프레임 |
| `SFX_WRIST_SWEEP_TELEGRAPH` | `Move.GroundSweep` 예고 시작 |
| `SFX_WRIST_SWEEP` | `Move.GroundSweep` 판정 직전 |
| `SFX_WRIST_CHARGE_TELEGRAPH` | `Move.Charge` 예고 시작 |
| `SFX_WRIST_CHARGE_LOOP` | `Move.Charge` 이동 동안 로컬 루프 |
| `SFX_WRIST_WALL_IMPACT` | 돌진 Raycast 벽 감지 |
| `SFX_WRIST_STUN` | 벽 충돌 후 경직 진입 |
| `SFX_WRIST_DROP_WARNING` | `Move.Slam` 그림자 생성 직후 |
| `SFX_WRIST_DROP_IMPACT` | `Move.Slam` 피해 판정과 동시에 |
| `SFX_WRIST_HURT` | `Enemy.TakeDamage()`에서 해당 보스일 때 |
| `SFX_WRIST_DEATH` | 해당 보스 체력 0 |

### 기억의 교수자

| 음원 | 붙일 위치 |
|---|---|
| `SFX_INSTRUCTOR_INTRO` | 전투 시작 |
| `SFX_INSTRUCTOR_CORE_LOOP` | 보스 생존 중 코어 오브젝트 로컬 루프 |
| `SFX_INSTRUCTOR_ARM_MOVE` | 큰 팔이 공격 자세로 회전할 때 |
| `SFX_INSTRUCTOR_PHASE_WARNING` | `BossController.UpdatePhase()` 페이즈 변경 직전 |
| `SFX_INSTRUCTOR_PHASE_RUPTURE` | 페이즈 전환 VFX와 동시에 |
| `SFX_INSTRUCTOR_BLADE_SWEEP` | 칼날 쓸기 판정 직전 |
| `SFX_INSTRUCTOR_CHAIN_WARNING` | 사슬 낙하 예고 시작 |
| `SFX_INSTRUCTOR_CHAIN_DROP` | 사슬이 실제로 내려오기 시작할 때 |
| `SFX_INSTRUCTOR_CHAIN_IMPACT` | 사슬이 바닥에 닿을 때 |
| `SFX_INSTRUCTOR_HOOK_PULL` | 갈고리 끌기 시작 |
| `SFX_INSTRUCTOR_PLATFORM_BREAK` | `arenaRewindables[index].BreakForEncounter()` 직전 |
| `SFX_INSTRUCTOR_PLATFORM_REWIND` | 전장 발판 복원 시작 |
| `SFX_INSTRUCTOR_HURT` | 해당 보스 피격 |
| `SFX_INSTRUCTOR_DEATH` | 해당 보스 체력 0 |

## 10. 환경음

환경음은 `AudioManager`의 BGM과 분리한다. `ResidueAmbientAudio.cs`가 현재 절차 생성한 임시
환경음을 재생하므로, 이 클래스를 `AreaAmbienceController` 형태로 확장하거나 방마다
`AudioSource`를 배치한다.

| 음원 | 권장 배치 |
|---|---|
| `AMB_ENTRY_BRIDGE` | 잔재 입구·대교 방 |
| `AMB_LOWER_RUINS` | 하부 폐허 |
| `AMB_INSIDE_FINGERS` | 손가락 내부 구조 |
| `AMB_LIFT_SHAFT` | 승강기 수직 공간 |
| `AMB_UPPER_TOWER` | 상층 감시탑 |
| `AMB_GALLOWS` | 교수자 보스 전장 |
| `AMB_SECRET_ROOM` | 비밀 기억 방 |

아래 원샷은 한 번에 계속 재생하지 않는다. 각 방에 10~35초 무작위 간격 스케줄러를 두고,
동시에 최대 하나만 재생한다.

| 음원 | 적합한 방 |
|---|---|
| `AMB_ONESHOT_CHAIN_CREAK` | 다리·승강기·교수자 전장 |
| `AMB_ONESHOT_CAGE_SWAY` | 우리·감시탑 |
| `AMB_ONESHOT_PEBBLES` | 폐허·수직 공간 |
| `AMB_ONESHOT_DISTANT_COLLAPSE` | 폐허·다리 |
| `AMB_ONESHOT_FINGER_GROAN` | 손가락 내부 |
| `AMB_ONESHOT_MEMORY_WHISPER` | 기억 방·교수자 전장 |
| `AMB_ONESHOT_BELL` | 상층·교수자 전장, 매우 낮은 빈도 |
| `AMB_ONESHOT_PUDDLE_DROP` | 하부 폐허 |
| `AMB_ONESHOT_WINDOW_DIE` | 상층 감시탑 |
| `AMB_ONESHOT_DISTANT_FOOTSTEP` | 다리·거대 내부 공간 |

`AudioSource` 권장값:

```text
Loop: 환경 베드만 ON
Play On Awake: OFF
Spatial Blend: 0 (2D)
Ambience Volume: 0.08~0.18
Random One-shot Volume: 0.05~0.14
```

## 11. BGM

`GameManager`가 `ZoneData.bgm`을 재생한다. 아래 ScriptableObject의 `bgm` 필드에 음악을 넣는다.

| 지역 | 에셋 | 현재 상태 |
|---|---|---|
| 프롤로그 | `Assets/ScriptableObjects/Zone_Prologue.asset` | 미지정, 임시 절차음 |
| 잔재 | `Assets/ScriptableObjects/Zone_Residue.asset` | `Assets/Audio/Residue_BGM.mp3` 연결됨 |
| 응시 | `Assets/ScriptableObjects/Zone_Gaze.asset` | 미지정, 임시 절차음 |
| 균열 | `Assets/ScriptableObjects/Zone_Fracture.asset` | `Assets/Audio/Fracture_BGM.mp3` 연결됨 |
| 엔딩 | Ending 씬의 `EndingSequence.endingBgm` | 전용 필드에 직접 할당 |

타이틀 음악은 현재 `TitleScreen`에서 재생 요청을 하지 않는다. 타이틀 BGM을 만들면
`TitleScreen.Start()`에서 `AudioManager.PlayBgm()`을 호출하고, 씬 또는 ScriptableObject에
클립을 직렬화한다.

## 12. 아직 만들지 않았거나 보류할 음향

다음은 음원을 먼저 붙이는 것이 아니라 기능 또는 이벤트를 먼저 구현해야 한다.

- 강한 착지: 현재 판정 없음. `SFX_PLAYER_LAND_HEAVY`는 보관한다.
- 발밑 재질별 발소리: 지면 재질 판별이 먼저 필요하다.
- 숨죽이기·예지·자각 전용 시작/루프/종료음: 현재 범용 `Ability`만 호출한다.
- 엔딩 단계별 연출음: `EndingSequence`에 오디오 이벤트가 없다.
- 보스별 전투 BGM: 현재 지역 BGM을 계속 사용한다.

## 13. 권장 적용 순서

1. `SfxCue` 누락 이름을 복구해 컴파일을 정상화한다.
2. 3절의 이미 연결된 공용 효과음을 Resources 폴더에 복사한다.
3. 플레이어를 직접 조작하며 공격·적중·점프·대시·착지·벽·피격·사망·부활을 확인한다.
4. 체크포인트·아이템·UI·숏컷을 연결한다.
5. 되감기 시작/루프/취소/성공을 분리한다.
6. 일반 적 4종에 종류별 오디오 프로필을 붙인다.
7. 보스 두 종류에 보스별 프로필을 붙인다.
8. 월드 장치와 환경음 스케줄러를 연결한다.
9. 마지막에 BGM과 전체 믹스 음량을 조정한다.

## 14. 품질 주의 목록

아래 5개는 현재 최선 후보를 정제해 두었지만 재생성 검토 대상으로 표시돼 있다.

```text
SFX_CARRIER_CHARGE_LOOP
SFX_HARDENED_DEATH
SFX_WALKER_ATTACK
SFX_WRIST_DROP_WARNING
SFX_WRIST_WALL_IMPACT
```

개발 중에는 그대로 연결해도 되지만 최종 빌드 전 교체를 권장한다.
