# 애니메이션·VFX 구현 명세

## 1. 목적

잔재 지역의 플레이어, 적, 보스, 감정 스킬을 같은 규칙으로 Unity에 연결하기 위한
구현 기준이다. 새 적과 아이템을 추가해도 상태 이름과 VFX 호출 방식은 유지한다.

## 2. 공통 임포트

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Filter Mode: `Bilinear`
- Compression: `None`
- Generate Mip Maps: Off
- Alpha Is Transparency: On
- Pivot: 플레이어·적은 `Bottom Center`, 중심 확산 VFX는 `Center`
- 프레임마다 같은 사각형을 유지하고 Tight Mesh의 외곽 변화로 충돌을 만들지 않는다.
- Collider와 공격 판정은 이미지와 분리한다.

시트별 실제 격자와 셀 크기는
`Assets/Art/Residue/Gameplay/README.md`를 단일 기준으로 사용한다.

## 3. 플레이어 상태 연결

`PlayerController.State`의 11개 상태와 클립 이름을 1:1로 맞춘다.

| PlayerState | 클립 | Loop | 종료 규칙 |
|---|---|---:|---|
| Idle | `Player_Idle` | O | 입력 또는 상위 상태 진입 |
| Walk | `Player_Walk` | O | 속도·접지 상태 변경 |
| Run | `Player_Run` | O | 속도·접지 상태 변경 |
| Jump | `Player_Jump` | X | 상승 중 입력 시 AirMove |
| AirMove | `Player_AirMove` | O | 낙하·착지 |
| Fall | `Player_Fall` | O | 착지 |
| Land | `Player_Land` | X | 상태 타이머 종료 |
| Attack | `Player_Attack` | X | 공격 타이머 종료 |
| Dash | `Player_Dash` | X | 대시 타이머 종료 |
| WallCling | `Player_WallCling` | O | 벽 이탈·벽점프 |
| WallJump | `Player_WallJump` | X | 벽점프 잠금 종료 |

Animator 파라미터는 현재 `PlayerAnimator`가 사용하는 상태 전환을 유지한다. 루트 모션은
끄고 실제 위치는 `PlayerController`와 `Rigidbody2D`만 변경한다.

### 프레임 이벤트

| 클립 | 이벤트 | 권장 프레임 | 기능 |
|---|---|---:|---|
| Attack | `AttackActive` | 3/6 | 공격 판정 1회 |
| Dash | `DashTrail` | 2/6, 4/6 | 잔상 VFX |
| Land | `LandDust` | 3/6 | 착지 먼지·약한 카메라 반응 |
| WallJump | `WallDust` | 2/6 | 벽 접점 VFX |

게임 로직이 이미 판정을 호출한다면 Animation Event는 VFX·사운드 동기화에만 사용하고
피해가 두 번 적용되지 않게 한다.

## 4. 일반 적 확장 규칙

모든 적 프리팹은 최소한 다음 상태 키를 가진다.

`Idle`, `Move`, `Telegraph`, `Attack`, `Hit`, `Death`

적에 없는 상태는 생략할 수 있지만 같은 의미에 다른 이름을 쓰지 않는다. 새 적을
추가할 때는 `EnemyData`와 행동 컴포넌트가 상태 변경 이벤트만 보내고, 실제 클립은
프리팹의 Animator Override Controller에서 교체한다.

| 적 | 행동 컴포넌트 기준 | 핵심 타이밍 |
|---|---|---|
| 잔재 보행자 | 순찰 | Attack 3/4에 판정 |
| 매달린 손가락 | 매복 | Drop 시작 전에 2프레임 예고 |
| 애도 운반자 | 돌진 | Charge 2/4 이후 이동, 충돌 시 Impact |
| 굳은 잔재 | 가드 | Block 중 정면 피해 무효, Heavy Attack 3/4 판정 |

`Hit`는 0.08~0.12초로 짧게, `Death`는 0.35~0.55초 후 풀로 반환하거나 제거한다.

## 5. 보스

### 손목 감시자

공격은 항상 `Telegraph → Active → Recover` 세 구간을 갖는다.

- Sweep: 2프레임 이상 예고, 3/6 프레임에 판정, 마지막 2프레임 회수
- Charge: 2/6 이후 이동 시작, 벽 충돌 시 Impact-Stun으로 전환
- Drop: 그림자 경고 VFX를 먼저 표시하고 4/6에 착지 판정
- Death: 입력과 피해를 즉시 끄고 클립 및 VFX 완료 후 보상 생성

### 기억의 교수자

`MemoryInstructor_Parts_v1.png`를 조립해 사용한다.

- 몸통: 위치 기준점, 크게 움직이지 않는다.
- 칼날 팔·갈고리 팔: 소켓을 피벗으로 회전한다.
- 사슬: 길이 방향 Scale Y 또는 여러 링크 파츠로 신축한다.
- 후광·핵: 낮은 속도로 역회전하며 `MemoryInstructorVFX`의 Core Pulse와 동기화한다.
- Chain Slam은 경고 원 1~3프레임 후 4프레임에 실제 충돌 판정을 낸다.
- 단계 전환은 Phase Rupture를 재생하는 동안 피해 무효로 처리한다.

## 6. VFX 호출표

| 사건 | 시트/행 | 정렬 기준 |
|---|---|---|
| 플레이어 피격 | PlayerVFX 1행 | Player + 2 |
| 플레이어 사망 | PlayerVFX 2행 | Player + 2 |
| 체크포인트 부활 | PlayerVFX 3행 | Player + 2 |
| 적 피격·방어·사망 | CombatVFX 1~3행 | Enemy + 2 |
| 되감기 홀드·완료 | EmotionVFX 1~2행 | Interactable + 2 |
| 자각 반응 | EmotionVFX 3행 | Foreground FX |
| 재화·회복·기억 획득 | PickupVFX 1~3행 | Player + 3 |
| 보스 충돌 | CombatVFX 4행 | Boss + 2 |
| 교수자 패턴·단계 전환 | MemoryInstructorVFX | Boss + 3 |

VFX 프리팹은 `Play(effectId, position, rotation, scale)` 형태의 공통 호출로 감싼다.
새 아이템이나 몬스터는 새 VFX 코드를 만들기보다 `effectId`와 시트 클립을 등록한다.

## 7. 적용 완료 판정

- 모든 시트가 정해진 격자로 잘리고 알파 배경이 보이지 않는다.
- Idle 10초 반복에서 몸 크기와 발 위치가 튀지 않는다.
- Walk/Run 중 스프라이트 내부 이동과 실제 Rigidbody 이동이 중복되지 않는다.
- Attack, Charge, Drop의 판정이 예고 프레임보다 먼저 나오지 않는다.
- 피격·사망 VFX가 공격자보다 뒤에 가려지지 않는다.
- 되감기 완료 프레임과 실제 오브젝트 복원이 같은 순간에 발생한다.
- 60 FPS 게임에서 애니메이션은 지정 FPS로 일정하게 재생된다.
