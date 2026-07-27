# Residue Gameplay Art v1

잔재 지역 배경과 기존 Terrain/Interactables 아틀라스의 검은 철골, 회갈색 석재, 앰버 발광,
낮은 남보라 하이라이트에 맞춘 1차 게임플레이 이미지다.

## 공통 임포트

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple
- Pixels Per Unit: 32
- Filter Mode: Bilinear
- Compression: None
- Generate Mip Maps: Off
- Alpha Is Transparency: On
- Mesh Type: Tight

충돌은 스프라이트 외곽선으로 자동 생성하지 않고 별도 Collider를 사용한다.

## 시트 분할

### Player/Player_KeyPoses_v1.png

- 크기: 1536 × 1024
- 분할: 4열 × 2행
- 셀: 384 × 512
- 순서:
  - 위: Idle / Walk / Run / Jump
  - 아래: Fall / Land / Attack / Dash

기본 구현용 핵심 포즈다. 완성 애니메이션에는 동작별 중간 프레임을 추가한다.

### Enemies/Residue_Enemies_Atlas_v1.png

- 크기: 1254 × 1254
- 분할: 2열 × 2행
- 셀: 627 × 627
- 순서:
  - 위: Residue Walker / Hanging Finger
  - 아래: Mourning Carrier / Hardened Residue

### Items/Residue_ItemsHazards_Atlas_v1.png

- 크기: 1254 × 1254
- 분할: 3열 × 3행
- 셀: 418 × 418
- 순서:
  - 위: Currency / Healing Reliquary / Maximum Health Shard
  - 가운데: Memory Fragment / Spike Strip / Void Warning Growth
  - 아래: Crumbling Platform Intact / Crumbling Platform Fractured / Broken Pulley

### Props/Residue_Shortcuts_Atlas_v1.png

- 크기: 1536 × 1024
- 분할: 3열 × 2행
- 셀: 512 × 512
- 순서:
  - 위: Broken Chain Bridge / Restored Chain Bridge / Dormant Lift
  - 아래: Active Lift / Broken Pulley / Restored Pulley

### Bosses/WristWatcher_Poses_v1.png

- 크기: 1536 × 1024
- 분할: 3열 × 2행
- 셀: 512 × 512
- 순서:
  - 위: Idle / Sweep Anticipation / Charge Anticipation
  - 아래: Charge Impact / Drop Attack / Hurt

6개 핵심 포즈다. 실제 애니메이션에는 예고와 복귀 중간 프레임을 추가한다.

### Bosses/MemoryInstructor_Parts_v1.png

- 크기: 1254 × 1254
- 분할: 3열 × 3행
- 셀: 418 × 418
- 경계의 흰 분할선이 들어간 셀은 Sprite Editor에서 각 변을 2px 안쪽으로 잘라 사용한다.
- 순서:
  - 위: Torso / Caged Head / Lower Root
  - 가운데: Blade Arm / Hook Arm / Gallows Halo
  - 아래: Short Chain / Hooked Chain / Safety Platform

몸통을 중심으로 파츠를 별도 GameObject에 배치하고 회전축은 각 원형 소켓 중심에 둔다.

## 애니메이션 시트

모든 행은 왼쪽에서 오른쪽 순서로 재생한다. 아래 FPS는 시작값이며 실제 이동 거리와
공격 판정 시간에 맞춰 조정한다.

### Player/Animation

| 파일 | 격자 / 셀 | 행 순서 | 권장 FPS |
|---|---|---|---|
| `Player_Locomotion_v1.png` | 8×3 / 256×256 | Idle / Walk / Run | 8 / 12 / 14 |
| `Player_Aerial_v1.png` | 6×4 / 256×256 | Jump / AirMove / Fall / Land | 12 / 10 / 10 / 14 |
| `Player_Actions_v1.png` | 6×2 / 362×362 | Attack / Dash | 16 / 18 |
| `Player_Wall_v1.png` | 6×2 / 362×362 | WallCling / WallJump | 8 / 14 |

- Idle, Walk, Run, AirMove, Fall, WallCling은 Loop Time을 켠다.
- Jump, Land, Attack, Dash, WallJump는 한 번 재생하고 상태 전환으로 빠져나간다.
- 공격 판정은 Attack 3번 프레임, 대시 잔상은 Dash 2번 프레임에서 켠다.
- 좌우 방향은 별도 이미지를 만들지 않고 `SpriteRenderer.flipX`로 처리한다.

### Enemies/Animation

일반 적 4종은 모두 `4×4`, 셀 `314×314`다.

| 파일 | 1행 | 2행 | 3행 | 4행 |
|---|---|---|---|---|
| `ResidueWalker_v1.png` | Idle | Walk | Attack | Hit → Death |
| `HangingFinger_v1.png` | Hang Idle | Crawl | Drop Attack | Hit → Death |
| `MourningCarrier_v1.png` | Idle | Walk | Charge | Impact → Death |
| `HardenedResidue_v1.png` | Idle | Walk | Heavy Attack | Block → Hit → Death |

- Idle 6 FPS, 이동 10 FPS, 공격 12 FPS, 반응·사망 10 FPS를 기준으로 한다.
- Attack 마지막 프레임을 공격 판정으로 쓰지 않는다. 예고와 타격 사이의 3번 프레임에
  Animation Event를 배치해 회피할 시간을 남긴다.
- 4행은 상태 수가 다른 혼합 행이다. Sprite 이름을 `Hit_00`, `Death_00`처럼 직접 나눈다.

### Bosses/Animation

| 파일 | 격자 / 셀 | 행 순서 | 권장 FPS |
|---|---|---|---|
| `WristWatcher_Combat_v1.png` | 6×4 / 256×256 | Idle / Sweep / Charge / Impact-Stun | 8 / 12 / 14 / 10 |
| `WristWatcher_Reactions_v1.png` | 6×3 / 256×341 | Drop / Hurt / Death | 14 / 12 / 10 |

`MemoryInstructor_Parts_v1.png`는 프레임 시트가 아니라 조립형 보스다. 몸통, 팔, 사슬,
후광을 각각 자식 오브젝트로 만들고 피벗 회전·위치 이동으로 애니메이션한다. 큰 보스를
통짜 프레임으로 만들면 해상도와 메모리 사용량이 급증하고 파츠가 흔들리기 쉽다.

### VFX

| 파일 | 격자 / 셀 | 행 순서 | 권장 FPS |
|---|---|---|---|
| `CombatVFX_v1.png` | 6×4 / 256×256 | Hit / Block / Enemy Death / Boss Impact | 18 / 16 / 14 / 16 |
| `EmotionVFX_v2.png` | 6×3 / 296×296 | Rewind Channel / Rewind Complete / Awareness Pulse | 12 / 18 / 10 |
| `PickupVFX_v1.png` | 6×3 / 296×296 | Currency / Healing / Memory | 16 / 16 / 12 |
| `PlayerVFX_v1.png` | 6×3 / 256×341 | Hit / Death / Respawn | 18 / 12 / 12 |
| `MemoryInstructorVFX_v1.png` | 6×3 / 296×296 | Chain Slam / Core Pulse / Phase Rupture | 16 / 12 / 12 |

VFX는 마지막 프레임에서 오브젝트를 반환하는 풀링 방식으로 재생한다. 피격 효과는
공격 방향에 맞춰 회전하고, 획득·되감기·핵 파동은 회전하지 않는다.

## PNG와 부드러움

투명 PNG 또는 투명 PNG 스프라이트 시트가 현재 프로젝트에 맞는 원본 포맷이다. PNG가
애니메이션을 부드럽게 만드는 것은 아니다. 부드러움은 다음 네 요소로 결정한다.

1. 프레임 사이 실루엣·체적·발 위치가 일정한가
2. 게임 상태 시간과 클립 재생 시간이 맞는가
3. 공격 판정·발소리·착지 같은 이벤트가 정확한 프레임에 있는가
4. 이동은 물리 좌표가 담당하고 스프라이트 안에서 캐릭터가 미끄러지지 않는가

현재 이미지는 1차 게임 적용용 시트다. Unity Animator에서 반복 재생해 발 고정과 체적
떨림을 확인한 뒤 필요한 프레임만 수작업 보정하는 것을 최종 마감 단계로 둔다.
