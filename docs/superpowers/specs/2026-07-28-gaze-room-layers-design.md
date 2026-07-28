# 응시 지역 룸 레이어 제작 설계

## 1. 목표

`docs/concept-art/generated/gaze-rooms-v1/`의 메인 룸 12장과 비밀방 3장을 Unity 2D
횡스크롤 맵에서 패럴랙스 배경으로 사용할 수 있도록 룸마다 다음 세 레이어로 제작한다.

1. `BG_Far`: 불투명 원경
2. `BG_Mid`: 투명 중경
3. `FG_Overlay`: 투명 전경 프레임

총 산출물은 15룸 × 3레이어 = 45장이다. 실제 이동 지형·발판·눈 장치·문·기물은 이미
완료한 `Assets/Art/Gaze/Environment`의 모듈형 Sprite로 별도 배치한다.

## 2. 전체 제작 순서

응시 지역의 남은 이미지 작업은 다음 세 독립 제작 단위로 진행한다.

1. 룸 레이어 45장
2. 일반 적·정예·아이템 이미지와 애니메이션
3. 중간 보스·지역 보스·숨죽임·자각 VFX와 환경 중간 프레임

이 문서는 첫 번째 제작 단위만 확정한다. 기본 플레이어의 걷기·점프·공격 애니메이션은
잔재 지역에서 제작한 공용 세트를 재사용하고, 이후 제작 단위에서는 응시 전용 숨죽임·자각
VFX만 추가한다.

## 3. 기준 이미지와 시각 규칙

기준 이미지:

- `docs/concept-art/03-gaze-sky-observer-v2-clean-master.png`
- `docs/concept-art/generated/gaze-rooms-v1/01-entry-threshold.png`
- `docs/concept-art/generated/gaze-rooms-v1/02-exposed-plaza.png`
- `docs/concept-art/generated/gaze-rooms-v1/03-lower-prison-district.png`
- `docs/concept-art/generated/gaze-rooms-v1/04-hushed-crevice.png`
- `docs/concept-art/generated/gaze-rooms-v1/05-fixed-gaze-hall.png`
- `docs/concept-art/generated/gaze-rooms-v1/06-rotating-gaze-arcade.png`
- `docs/concept-art/generated/gaze-rooms-v1/07-hanging-cage-lift.png`
- `docs/concept-art/generated/gaze-rooms-v1/08-upper-cage-transit.png`
- `docs/concept-art/generated/gaze-rooms-v1/09-faceless-audience-gallery.png`
- `docs/concept-art/generated/gaze-rooms-v1/10-optic-nerve-viaduct.png`
- `docs/concept-art/generated/gaze-rooms-v1/11-great-ocular-cathedral.png`
- `docs/concept-art/generated/gaze-rooms-v1/12-pupil-sanctum.png`
- `docs/concept-art/generated/gaze-rooms-v1/S1-flooded-observation-cell.png`
- `docs/concept-art/generated/gaze-rooms-v1/S2-hidden-cage-archive.png`
- `docs/concept-art/generated/gaze-rooms-v1/S3-blind-chamber.png`

공통 시각 규칙:

- 먹색, 푸른 흑색, 저채도 보라, 제한적인 냉청록을 유지한다.
- 젖은 고딕 감옥 도시, 거대 하늘 눈, 안구 고가교, 매달린 케이지의 정체성을 유지한다.
- 캐릭터, 적, 보스, 아이템, UI, 글자, 워터마크를 포함하지 않는다.
- 잔재 지역의 갈색, 앰버, 뼈, 거대 손 모티프를 섞지 않는다.
- 세 레이어를 합성했을 때 해당 룸 콘셉트의 구도와 랜드마크가 복원되어야 한다.
- 실제 발판처럼 밝은 연속 상단선을 배경 레이어에 넣지 않는다.

## 4. 레이어별 규격

### 4.1 `BG_Far`

- 크기: 1672×941
- 형식: 불투명 RGB 또는 RGBA PNG
- 피벗: Center
- 포함:
  - 하늘과 안개
  - 거대 하늘 눈
  - 가장 먼 도시 실루엣
  - 먼 안구 고가교
  - 먼 성당·감시탑 랜드마크
- 제외:
  - 플레이 가능한 바닥
  - 가까운 기둥·난간·케이지
  - 눈 장치와 문
  - 캐릭터 크기로 읽히는 관객

카메라 이동량은 기본 카메라의 0.15~0.25배를 기준으로 한다.

### 4.2 `BG_Mid`

- 크기: 1672×941
- 형식: 투명 RGBA PNG
- 피벗: Center
- 포함:
  - 중거리 건물과 아치
  - 중거리 안구교와 케이지 레일
  - 작은 관객석·철창·창문
  - 룸 정체성을 만드는 핵심 구조
- 제외:
  - 하늘과 원경 전체를 덮는 불투명 면
  - 실제 이동 가능한 발판
  - 상호작용 오브젝트

카메라 이동량은 기본 카메라의 0.45~0.65배를 기준으로 한다.

### 4.3 `FG_Overlay`

- 크기: 1672×941
- 형식: 투명 RGBA PNG
- 피벗: Center
- 포함:
  - 화면 가장자리의 검은 아치·기둥
  - 가까운 사슬과 철창 일부
  - 천장·하단의 비네트형 구조
  - 깊이를 주는 짧은 안개·그림자
- 제외:
  - 화면 중앙을 가리는 큰 불투명 구조
  - 이동 지형으로 오인되는 평평한 상단
  - 상호작용 기물과 충돌 오브젝트

카메라 이동량은 기본 카메라의 1.05~1.2배를 기준으로 한다. `FG_Overlay` 자체에는 Collider를
붙이지 않는다.

## 5. 룸별 레이어 핵심

| 룸 | `BG_Far` 핵심 | `BG_Mid` 핵심 | `FG_Overlay` 핵심 |
| --- | --- | --- | --- |
| G01 눈꺼풀 경계 | 먼 감시탑·부분 하늘 눈 | 입구 아치·먼 안구교 | 좌측 눈꺼풀형 암벽 |
| G02 고정된 시선교 | 거대 눈·도시 협곡 | 끊긴 다리·먼 케이지 | 좌우 교각·사슬 |
| G03 관객 광장 | 허브 도시·상층 목표 | 관객 건물·사슬막 배경틀 | 광장 가장자리 아치 |
| G04 하층 새장원 | 좁은 도시 틈 | 철창 벽·케이지 군집 | 낮은 천장·측면 기둥 |
| G05 숨죽임 성소 | 성소 뒤 도시·부분 안구교 | 큰 벽 눈·고딕 아치 | 두꺼운 내부 기둥 |
| G06 속삭임 통로 | 먼 도시와 교차교 | 회전 눈 장치용 벽틀 | 터널 천장·측면 암부 |
| G07 회전 홍채교 | 수직 도시·거대 눈 외곽 | 승강축·교차 안구교 | 좌우 탑 벽·사슬 |
| G08 시선 승강정 | 하늘 눈·상층 도시 | 승강 레일·먼 환승교 | 수직 레일 프레임 |
| G09 상층 관객석 | 거대 눈·관객 도시 | 곡선 관객석·케이지 | 극장형 상부 프레임 |
| G10 홍채 감시탑 | 하늘 눈·교차 고가교 | 감시탑·아래 도시 | 탑 난간·사슬 비네트 |
| G11 자기 초상의 회랑 | 깨진 천장·하늘 눈 | 대안구 창·성당 구조 | 거울 회랑 기둥 |
| G12 만인의 극장 | 하늘 눈·도시 원환 | 대형 홍채 창·관객석 | 원형 극장 프레임 |
| GS1 무대 뒤편 | 틈 밖 안구교 | 눈꺼풀 창·침수 벽 | 낮은 천장·수면 암부 |
| GS2 무언의 우리 | 케이지 축 밖 도시 | 기록벽·매달린 우리 | 서고 벽·근접 철창 |
| GS3 안쪽 눈 | 하늘 눈의 바깥 테두리 | 닫힌 눈 회랑·숨은 문틀 | 암흑 아치·냉청록 잔상 |

## 6. 저장 구조와 이름

게임 적용본:

```text
HiddenWeight/Assets/Art/Gaze/
├── Room01/
│   ├── Room01_BG_Far.png
│   ├── Room01_BG_Mid.png
│   └── Room01_FG_Overlay.png
├── ...
├── Room12/
└── Secret01/ ... Secret03/
```

생성 원본과 프롬프트:

```text
docs/concept-art/generated/gaze-room-layers/
├── PROMPTS.md
└── contact-sheets/
```

룸 매핑:

- `01`~`12` → `Room01`~`Room12`
- `S1`~`S3` → `Secret01`~`Secret03`

## 7. Unity 적용

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: 32
- Filter Mode: Bilinear
- Compression: None
- Mip Maps: Off
- Wrap Mode: Clamp
- `BG_Far`: Alpha Is Transparency는 이미지 모드에 따라 설정
- `BG_Mid`, `FG_Overlay`: Alpha Is Transparency On
- 정렬 순서: `BG_Far` < `BG_Mid` < Gameplay < `FG_Overlay`
- 세 레이어 모두 Collider 없음

기존 `ParallaxLayer`를 사용하고, 룸 전환 시 해당 룸의 세 레이어만 활성화한다.

## 8. 제작과 품질 검증

각 룸은 다음 순서로 완성한다.

1. 원본 콘셉트와 마스터를 참조해 `BG_Far` 생성
2. 같은 룸과 `BG_Far`를 참조해 `BG_Mid` 생성
3. 같은 룸과 앞의 두 레이어를 참조해 `FG_Overlay` 생성
4. 3장 합성 연락판과 원본 콘셉트를 나란히 비교
5. 크기·알파·빈 모서리·색상 범위를 검사

검증 기준:

- 45장이 모두 1672×941이다.
- `BG_Mid`, `FG_Overlay`는 RGBA이며 네 모서리 중 최소 세 곳이 투명하다.
- `BG_Far`는 화면 전체를 채우며 투명 구멍이 없다.
- 합성본이 해당 룸 원본과 같은 랜드마크·색상·시점으로 읽힌다.
- 배경 레이어에 플레이 가능한 발판, 활성 눈 장치, 문, 아이템이 없다.
- G01→G12에서 거대 눈과 상층 도시가 점점 가까워지는 공간 진행이 유지된다.

## 9. 완료 기준

- `BG_Far` 15장
- `BG_Mid` 15장
- `FG_Overlay` 15장
- 룸별 합성 미리보기 15장
- 전체 연락판 1장
- 프롬프트 기록
- Unity 메타 설정
- 해상도·알파·합성·프로젝트 테스트 검증

위 항목이 모두 존재하면 응시 지역 룸 레이어 제작이 완료된다.

