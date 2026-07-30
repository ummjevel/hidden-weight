# Fracture Complete Art Set Design

## 목표

균열 지역의 15개 룸 콘셉트를 Unity 패럴랙스 배경 45장으로 분리하고, 1·2맵과 동일한
수준으로 지형·기물·위험요소·적·보스·아이템·VFX·UI 이미지 33장을 제작한다.
플레이어 공용 이미지와 Unity 연결 코드는 이 범위에 포함하지 않는다.

## 기준 자료

- 전체 지도: `docs/concept-art/generated/fracture-map-v1/fracture-world-master.png`
- 룸 콘셉트: `docs/concept-art/generated/fracture-map-v1/rooms/*.png`
- 레벨 기획: `docs/FRACTURE_LEVEL_DESIGN.md`
- 파일 구조 기준: `HiddenWeight/Assets/Art/Gaze/`
- 시각 기준: 파스텔 민트, 빙청색, 라벤더, 옅은 살구색, 백색 대리석
- 공포 강도: 잘못된 대칭·불일치 반사·미래 고스트로 약 5%

## 1. 룸 패럴랙스 45장

각 메인 룸 12개와 비밀방 3개를 다음 세 레이어로 제작한다.

| 레이어 | 형식 | 포함 | 제외 |
| --- | --- | --- | --- |
| `BG_Far` | 1672×941 불투명 RGB PNG | 하늘, 먼 수면, 반복 아치, 먼 하늘 균열 | 플레이 지형, 가까운 기둥, 기물 |
| `BG_Mid` | 1672×941 투명 RGBA PNG | 중거리 온실, 아치, 탑, 먼 비활성 교량 | 하늘 전체, 실제 발판, 문, 장치 |
| `FG_Overlay` | 1672×941 투명 RGBA PNG | 화면 가장자리 꽃·기둥·아치·빛 굴절 | 중앙 65%, 이동 지형, 상호작용 기물 |

저장 위치:

```text
HiddenWeight/Assets/Art/Fracture/
├── Room01/Room01_BG_Far.png, Room01_BG_Mid.png, Room01_FG_Overlay.png
├── ...
├── Room12/
├── Secret01/
├── Secret02/
└── Secret03/
```

`BG_Far`는 룸 콘셉트의 분위기와 랜드마크를 유지한 조용한 원경이다. `BG_Mid`와
`FG_Overlay`는 녹색 크로마키 원본을 생성한 뒤 알파 PNG로 변환한다. 세 레이어에 캐릭터,
적, 아이템, UI, 실제 발판, 활성 장치를 포함하지 않는다.

## 2. 환경·지형 16장

| 분류 | 파일 |
| --- | --- |
| 지형 | `Environment/Terrain/Fracture_TerrainTiles_v1.png` |
| 발판 | `Environment/Terrain/Fracture_Platforms_v1.png` |
| 발판 상태 | `Environment/Terrain/Animation/FracturePlatformStates_v1.png` |
| 환경 기물 | `Environment/Props/Fracture_EnvironmentProps_v1.png` |
| 위험 요소 | `Environment/Hazards/Fracture_FutureHazards_v1.png` |
| 위험 전환 | `Environment/Hazards/Animation/FutureHazardTransitions_v1.png` |
| 예지 기물 | `Environment/Interactables/Fracture_ForesightObjects_v1.png` |
| 예지 전환 | `Environment/Interactables/Animation/ForesightObjectTransitions_v1.png` |
| 문·숏컷 | `Environment/Interactables/Fracture_DoorsShortcuts_v1.png` |
| 문·숏컷 전환 | `Environment/Interactables/Animation/DoorShortcutTransitions_v1.png` |
| 이동 구조 | `Environment/Interactables/Fracture_TransitStructures_v1.png` |
| 이동 구조 전환 | `Environment/Interactables/Animation/TransitTransitions_v1.png` |
| 환경 VFX | `Environment/VFX/Fracture_AmbientVFX_v1.png` |
| 환경 모션 | `Environment/VFX/Animation/FractureAmbientMotion_v1.png` |
| 원경 모션 | `Environment/VFX/Animation/FractureBackgroundMotion_v1.png` |
| 전경 모션 | `Environment/VFX/Animation/FractureForegroundMotion_v1.png` |

모든 아틀라스는 투명 RGBA PNG다. 정적 아틀라스는 1536×1024 또는 1536×768,
애니메이션은 192×192 셀의 8열 시트로 정규화한다.

## 3. 적·보스·아이템·VFX·UI 17장

| 분류 | 파일 |
| --- | --- |
| 적 | `Gameplay/Enemies/Animation/AnxiousSprout_v1.png` |
| 적 | `Gameplay/Enemies/Animation/LeadingShadow_v1.png` |
| 적 | `Gameplay/Enemies/Animation/PossibilityCollector_v1.png` |
| 정예 | `Gameplay/Enemies/Animation/SplitSelf_v1.png` |
| 중간 보스 | `Gameplay/Bosses/Animation/SecondHandWatcher_Combat_v1.png` |
| 중간 보스 전환 | `Gameplay/Bosses/Animation/SecondHandWatcher_Transitions_v1.png` |
| 지역 보스 | `Gameplay/Bosses/Animation/UnarrivedSelf_Combat_v1.png` |
| 지역 보스 가능성 | `Gameplay/Bosses/Animation/UnarrivedSelf_Possibilities_v1.png` |
| 지역 보스 반응 | `Gameplay/Bosses/Animation/UnarrivedSelf_Reactions_v1.png` |
| 아이템 | `Gameplay/Items/Animation/FractureCollectibleTransitions_v1.png` |
| 일반 투사체 | `Gameplay/VFX/Animation/FractureEnemyProjectiles_v1.png` |
| 보스 투사체 | `Gameplay/VFX/Animation/FractureBossProjectiles_v1.png` |
| 타격 VFX | `Gameplay/VFX/Animation/FractureImpactVFX_v1.png` |
| 보조 VFX | `Gameplay/VFX/FractureSecondaryVFX_v1.png` |
| 룸 전환 | `Environment/Interactables/Animation/FractureRoomTransitions_v1.png` |
| UI | `UI/FractureUIIcons_v1.png` |
| 상태 UI | `UI/Animation/FractureStatusUI_v1.png` |

적과 보스는 균열 지역의 밝은 재질과 미래 고스트를 사용하되 실루엣이 배경보다 진해야 한다.
정면·측면 포즈를 한 시트에 섞지 않고, 모든 이동·공격 프레임은 동일한 바닥선과 셀 중심을
유지한다.

## 4. 투명 이미지 규칙

- built-in `image_gen`에서 순수 `#00FF00` 단색 배경으로 생성한다.
- `remove_chroma_key.py`의 `--auto-key border --soft-matte --despill`로 알파를 만든다.
- 최종 모드는 RGBA다.
- 네 모서리는 모두 알파 0이어야 한다.
- 셀 경계를 넘는 잔상, 그림자, 텍스트, 라벨, 워터마크를 금지한다.

## 5. 제작 순서

1. 15개 룸의 `BG_Far`를 생성한다.
2. 15개 룸의 `BG_Mid`를 생성·크로마 제거한다.
3. 15개 룸의 `FG_Overlay`를 생성·크로마 제거한다.
4. 환경·지형 16장을 제작한다.
5. 적·보스·아이템·VFX·UI 17장을 제작한다.
6. 전체 컨택트시트와 프롬프트 기록을 만든다.
7. 크기·모드·알파·셀 정렬을 검사한다.

## 완료 기준

- 룸 패럴랙스 45장.
- 환경·지형 이미지 16장.
- 적·보스·아이템·VFX·UI 이미지 17장.
- 총 78장 모두 지정 경로에 존재.
- 불투명 배경 15장을 제외한 63장은 RGBA이며 투명 모서리를 갖는다.
- 모든 이미지가 균열 전체 지도와 동일한 팔레트·랜드마크·미세 공포 강도를 유지한다.
- 프롬프트 기록과 전체 컨택트시트가 존재한다.
