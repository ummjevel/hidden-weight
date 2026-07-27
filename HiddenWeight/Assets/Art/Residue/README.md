# Residue Art Import Guide

## Unity 기준

- Pixels Per Unit: `32`
- Filter Mode: `Bilinear`
- Compression: `None`
- Wrap Mode: `Clamp`
- Generate Mip Maps: `Off`
- Alpha Is Transparency: 투명 PNG에서 `On`
- Mesh Type: 배경은 `Full Rect`, 개별 오브젝트는 `Tight`

## 권장 Sorting Order

| 레이어 | Sorting Order | 충돌 |
| --- | ---: | --- |
| Far Background | -30 | 없음 |
| Mid Background | -20 | 없음 |
| Terrain Visual | 0 | 별도 Tilemap Collider 사용 |
| Interactables | 5 | 오브젝트별 Collider 사용 |
| Player | 10 | Player Collider 사용 |
| Foreground Overlay | 20 | 없음 |

## 파일 구성

| 파일 | 용도 |
| --- | --- |
| `Room01/Room01_BG_Far.png` | 불투명 원경 배경 |
| `Room01/Room01_BG_Mid.png` | 투명 중경 구조물 |
| `Room01/Room01_FG_Overlay.png` | 투명 전경 가림 장식 |
| `Residue_TerrainAtlas.png` | 발판·벽·계단·아치 모듈 |
| `Residue_InteractablesAtlas.png` | 문·체크포인트·되감기 오브젝트·기억 파편 |
| `Gameplay/Player/Player_KeyPoses_v1.png` | 플레이어 8개 핵심 포즈 |
| `Gameplay/Enemies/Residue_Enemies_Atlas_v1.png` | 잔재 일반 적·정예 4종 |
| `Gameplay/Items/Residue_ItemsHazards_Atlas_v1.png` | 아이템·위험 요소 9종 |
| `Gameplay/Props/Residue_Shortcuts_Atlas_v1.png` | 다리·승강기·도르래 상태 6종 |
| `Gameplay/Bosses/WristWatcher_Poses_v1.png` | R10 중간 보스 핵심 포즈 6종 |
| `Gameplay/Bosses/MemoryInstructor_Parts_v1.png` | R12 지역 보스 조립 파츠 9종 |

각 Gameplay 시트의 분할 크기와 셀 순서는 `Gameplay/README.md`를 따른다.

## 아틀라스 분할

### Residue_TerrainAtlas

- Sprite Mode: `Multiple`
- 4열 × 2행 고정 그리드로 분할
- 원본 크기: `1672 × 941`
- 셀은 배치 편의를 위한 큰 모듈이며 실제 충돌은 이미지 외곽선을 자동 추적하지 않는다.
- `TilemapCollider2D` 또는 별도 `BoxCollider2D`로 단순한 충돌 지형을 만든다.

### Residue_InteractablesAtlas

- Sprite Mode: `Multiple`
- 3열 × 2행 고정 그리드
- 원본 크기: `1536 × 1024`
- 셀 크기: `512 × 512`

위쪽 행:

1. 닫힌 게이트
2. 열린 게이트
3. 체크포인트

아래쪽 행:

1. 파손된 되감기 오브젝트
2. 복원된 되감기 오브젝트
3. 기억 파편

## 맵 제작 원칙

실제 이동·점프 지형은 `Grid + Tilemap + Collider`로 만든다. 생성된 발판 스프라이트는
시각 표현이고, 게임플레이 판정은 단순한 별도 콜라이더를 사용한다. 방 전체를 한 장의
스프라이트와 하나의 PolygonCollider로 만들지 않는다.
