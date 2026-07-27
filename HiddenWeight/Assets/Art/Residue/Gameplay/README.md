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

## 현재 활용 범위

- 플레이어와 일반 적의 정적 배치 및 기본 상태 표시
- 아이템, 위험 요소, 숏컷 상태 교체
- 중간 보스 프로토타입 포즈 애니메이션
- 지역 보스 파츠 조립형 프로토타입

## 후속 이미지

- 일반 적별 Idle/Move/Attack/Hit/Death 중간 프레임
- 플레이어 동작별 중간 프레임
- 보스 예고·공격·복귀 중간 프레임
- 되감기, 피격, 사망, 획득 이펙트
- 지도와 UI 아이콘
