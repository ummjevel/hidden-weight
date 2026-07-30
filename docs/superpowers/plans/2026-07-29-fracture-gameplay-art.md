# Fracture Gameplay Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 균열 지역의 지형·기물·적·보스·아이템·VFX·UI 이미지 33장을 투명 PNG 아틀라스와 애니메이션 시트로 제작한다.

**Architecture:** 정적 오브젝트는 카테고리별 아틀라스, 움직이는 오브젝트는 192×192 셀 기반 애니메이션 시트로 분리한다. 모든 이미지가 같은 팔레트와 바닥선, 셀 중심, 광원 방향을 공유하게 한다.

**Tech Stack:** built-in `image_gen`, `remove_chroma_key.py`, Pillow, ImageMagick, PNG/RGBA

## Global Constraints

- 모든 최종 이미지는 투명 RGBA PNG다.
- 네 모서리 알파는 0이다.
- 애니메이션 시트는 192×192 셀의 정수 배수다.
- 정적 아틀라스는 1536×1024 또는 1536×768이다.
- 셀마다 한 개의 완전한 오브젝트나 프레임만 존재한다.
- 텍스트, 라벨, 번호, 워터마크, 배경 장면, 캐릭터 공용 플레이어 이미지를 포함하지 않는다.
- 결과는 `HiddenWeight/Assets/Art/Fracture/Environment`, `Gameplay`, `UI`에 저장한다.

---

### Task 1: 지형·발판·기물 12장

**Files:**
- Create: `Environment/Terrain/Fracture_TerrainTiles_v1.png`
- Create: `Environment/Terrain/Fracture_Platforms_v1.png`
- Create: `Environment/Terrain/Animation/FracturePlatformStates_v1.png`
- Create: `Environment/Props/Fracture_EnvironmentProps_v1.png`
- Create: `Environment/Interactables/Fracture_ForesightObjects_v1.png`
- Create: `Environment/Interactables/Animation/ForesightObjectTransitions_v1.png`
- Create: `Environment/Interactables/Fracture_DoorsShortcuts_v1.png`
- Create: `Environment/Interactables/Animation/DoorShortcutTransitions_v1.png`
- Create: `Environment/Interactables/Fracture_TransitStructures_v1.png`
- Create: `Environment/Interactables/Animation/TransitTransitions_v1.png`
- Create: `Environment/Hazards/Fracture_FutureHazards_v1.png`
- Create: `Environment/Hazards/Animation/FutureHazardTransitions_v1.png`

**Interfaces:**
- Consumes: 균열 전체 지도와 룸 콘셉트
- Produces: 레벨 조립에 필요한 충돌 지형과 상호작용 시각 요소

- [ ] 12개 녹색 크로마키 원본을 개별 생성한다.
- [ ] 크로마를 제거하고 지정 해상도로 정규화한다.
- [ ] 모서리 알파, 셀 간 여백, 실루엣 가독성을 검사하고 커밋한다.

### Task 2: 환경 VFX 4장

**Files:**
- Create: `Environment/VFX/Fracture_AmbientVFX_v1.png`
- Create: `Environment/VFX/Animation/FractureAmbientMotion_v1.png`
- Create: `Environment/VFX/Animation/FractureBackgroundMotion_v1.png`
- Create: `Environment/VFX/Animation/FractureForegroundMotion_v1.png`

**Interfaces:**
- Consumes: 패럴랙스 팔레트와 미래 고스트 규칙
- Produces: 꽃가루, 빛 굴절, 수면 잔상, 미래 건축 잔상

- [ ] 환경 VFX 네 시트를 생성한다.
- [ ] 크로마 제거 후 알파와 반복 프레임 연결을 검사한다.
- [ ] 네 시트를 커밋한다.

### Task 3: 적 4장

**Files:**
- Create: `Gameplay/Enemies/Animation/AnxiousSprout_v1.png`
- Create: `Gameplay/Enemies/Animation/LeadingShadow_v1.png`
- Create: `Gameplay/Enemies/Animation/PossibilityCollector_v1.png`
- Create: `Gameplay/Enemies/Animation/SplitSelf_v1.png`

**Interfaces:**
- Consumes: `FRACTURE_LEVEL_DESIGN.md` 적 행동
- Produces: 일반 적 3종과 정예 1종의 핵심 애니메이션

- [ ] 각 적의 대기·이동·공격·피격·사망 프레임을 개별 시트로 생성한다.
- [ ] 바닥선, 크기, 좌우 방향, 셀 여백을 정규화한다.
- [ ] 네 시트를 검사하고 커밋한다.

### Task 4: 보스 5장

**Files:**
- Create: `Gameplay/Bosses/Animation/SecondHandWatcher_Combat_v1.png`
- Create: `Gameplay/Bosses/Animation/SecondHandWatcher_Transitions_v1.png`
- Create: `Gameplay/Bosses/Animation/UnarrivedSelf_Combat_v1.png`
- Create: `Gameplay/Bosses/Animation/UnarrivedSelf_Possibilities_v1.png`
- Create: `Gameplay/Bosses/Animation/UnarrivedSelf_Reactions_v1.png`

**Interfaces:**
- Consumes: 균열 중간 보스와 지역 보스 패턴
- Produces: 전투·위상 전환·피격·사망 애니메이션

- [ ] 중간 보스 전투·전환 두 시트를 생성한다.
- [ ] 지역 보스 전투·가능성·반응 세 시트를 생성한다.
- [ ] 셀 중심과 보스 크기, 잔상 분리를 검사하고 커밋한다.

### Task 5: 아이템·전투 VFX·룸 전환 8장

**Files:**
- Create: `Gameplay/Items/Animation/FractureCollectibleTransitions_v1.png`
- Create: `Gameplay/VFX/Animation/FractureEnemyProjectiles_v1.png`
- Create: `Gameplay/VFX/Animation/FractureBossProjectiles_v1.png`
- Create: `Gameplay/VFX/Animation/FractureImpactVFX_v1.png`
- Create: `Gameplay/VFX/FractureSecondaryVFX_v1.png`
- Create: `Environment/Interactables/Animation/FractureRoomTransitions_v1.png`
- Create: `UI/FractureUIIcons_v1.png`
- Create: `UI/Animation/FractureStatusUI_v1.png`

**Interfaces:**
- Consumes: 지역 능력·전투·진행 규칙
- Produces: 수집·공격·피격·전환·지도 UI 마감 이미지

- [ ] 여덟 시트를 개별 생성한다.
- [ ] 크로마 제거와 크기·셀 정렬 검사를 수행한다.
- [ ] 여덟 시트를 커밋한다.

### Task 6: 전체 검증

**Files:**
- Create: `docs/concept-art/generated/fracture-gameplay-art/PROMPTS.md`
- Create: `docs/concept-art/generated/fracture-gameplay-art/contact-sheets/fracture-gameplay-art.jpg`

**Interfaces:**
- Consumes: 게임플레이 아트 33장
- Produces: 프롬프트 기록, 연락판, 최종 검증 결과

- [ ] 33개 파일 수, 경로, 크기, RGBA, 모서리 알파를 검사한다.
- [ ] 정적 아틀라스와 애니메이션 시트를 분리한 연락판을 만든다.
- [ ] 원본 프롬프트와 실제 저장 경로를 기록한다.
