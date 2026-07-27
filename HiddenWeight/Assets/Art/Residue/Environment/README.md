# 잔재 환경 이미지 적용표

기존 방별 원경·중경·전경 배경 위에 배치하는 실제 게임플레이 전경 Sprite다. 모든 파일은
투명 RGBA PNG이며 충돌과 피해 판정은 이미지 외곽선이 아니라 별도 Collider로 구성한다.

## 공통 Unity 임포트

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Pixels Per Unit: `32`
- Filter Mode: `Bilinear`
- Compression: `None`
- Generate Mip Maps: Off
- Alpha Is Transparency: On
- Mesh Type: `Full Rect`

## 파일별 분할

| 파일 | 격자 | 셀 크기 | 피벗 |
|---|---:|---:|---|
| `Terrain/Residue_TerrainTiles_v2.png` | 6×4 | 209×313 | Bottom Center |
| `Terrain/Residue_Platforms_v1.png` | 6×3 | 256×341 | Bottom Center |
| `Interactables/Residue_RewindStructures_v1.png` | 6×4 | 256×256 | Bottom Center |
| `Hazards/Residue_Hazards_v1.png` | 6×3 | 296×296 | Bottom Center |
| `Props/Residue_EnvironmentProps_v1.png` | 6×4 | 256×256 | Bottom Center |
| `VFX/Residue_AmbientVFX_v1.png` | 6×3 | 286×305 | Center |

Unity Sprite Editor에서 `Grid by Cell Count`로 위 격자를 지정하거나, `Grid by Cell Size`로
표의 셀 크기를 입력한다. Padding과 Offset은 모두 0이다.

## 지형 타일

`Residue_TerrainTiles_v2.png`는 충돌 Tilemap을 대체하지 않는 장식형 전경 모듈이다.

- 1행: 직선 바닥과 양 끝
- 2행: 외곽·안쪽 모서리, 천장, 파손 천장
- 3행: 세로 벽, 벽 캡, 아치·기둥 지지대
- 4행: 좌우 경사, 균열 바닥, 무너진 끝

직선 충돌은 BoxCollider2D, 경사는 PolygonCollider2D를 사용한다. 지형 Sprite의 작은 사슬과
장식 돌출부에는 충돌을 추가하지 않는다.

## 발판

`Residue_Platforms_v1.png`:

- 1행: 소형 돌 3종, 소형 철골 3종
- 2행: 중형 돌·철골 구조 6종
- 3행: 매달린 발판 3종, 뼈·갈비형 발판 3종

일방통행 발판은 PlatformEffector2D를 사용한다. 사슬은 시각 장식이며 물리 로프가 아니다.

## 되감기 구조물

`Residue_RewindStructures_v1.png`의 열은 구조물 종류, 행은 상태다.

| 열 | 구조물 |
|---:|---|
| 1 | 소형 발판 |
| 2 | 중형 발판 |
| 3 | 사슬다리 |
| 4 | 철문 |
| 5 | 승강기 |
| 6 | 도르래 |

| 행 | 상태 |
|---:|---|
| 1 | 파손 |
| 2 | 복원 완료 |
| 3 | 복원 초기 |
| 4 | 복원 후기 |

재생 순서는 `1행 → 3행 → 4행 → 2행`이다. 8~10 FPS로 재생하고 마지막 복원 프레임에서
실제 Collider와 통행 상태를 전환한다. 구조물 크기 차이가 있으므로 각 열의 Sprite 피벗은
Sprite Editor에서 같은 월드 접점에 맞춰 미세 조정한다.

## 위험물

`Residue_Hazards_v1.png`:

- 1행: 바닥 가시 대기·예고·공격, 천장 가시, 좌우 벽 가시
- 2행: 심연 촉수 대기·예고·공격, 압착기 열림·예고·닫힘
- 3행: 붕괴 바닥 정상·균열·붕괴, 낙하물 대기·예고·낙하

가시와 심연 촉수는 10~12 FPS, 압착기와 붕괴는 8~10 FPS를 기준으로 한다. 공격 Collider는
마지막 공격 프레임에만 켜고, 예고 상태에서 피해를 주지 않는다.

## 환경 장식

`Residue_EnvironmentProps_v1.png`:

- 1행: 기둥·철골·돌·사슬·우리
- 2행: 교수대·갈비뼈·손가락뼈·천·제단
- 3행: 잔재 시체·벽 부조·봉인 눈·석상·앰버 등
- 4행: 바닥 균열·벽 얼룩·재 더미·남색 기억 균열 데칼

1~3행은 Background Props 또는 Foreground Props 정렬층에 배치한다. 4행 데칼은 지형보다
0.01~0.03유닛 앞에 배치해 Z-fighting을 피한다. 장식에는 Collider를 넣지 않는다.

## 환경 VFX

`Residue_AmbientVFX_v1.png`:

- 1행: 재·먼지 6프레임, 8 FPS Loop
- 2행: 남색 안개 6프레임, 6 FPS Loop
- 3행: 앰버 불씨·낙하 파편 6프레임, 10 FPS Loop

같은 클립을 여러 개 배치할 때 시작 프레임, 재생속도 0.85~1.15배, 크기 0.8~1.3배를
무작위화한다. 안개는 캐릭터를 가리지 않도록 알파를 0.25~0.45 범위로 제한한다.

## 1차 적용 권장 순서

1. R02에서 직선 바닥·소형 발판·가시를 시험한다.
2. R03에서 지형 모서리·사슬다리 복원 상태를 시험한다.
3. R05에서 되감기 중간 프레임과 Collider 전환을 맞춘다.
4. R09에서 압착기·낙하물의 예고 시간을 검증한다.
5. R12에서 교수대 장식·남색 안개·앰버 불씨 밀도를 조정한다.
