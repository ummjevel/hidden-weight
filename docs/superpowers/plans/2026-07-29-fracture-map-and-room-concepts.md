# Fracture Map and Room Concepts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 균열 지역의 캐릭터 없는 전체 탐험 지도 1장과 그 지도에 연속되는 메인 룸 12장, 비밀방 3장을 제작한다.

**Architecture:** 원본 이미지를 팔레트와 건축 정체성 기준으로 삼고, 전체 지도에서 고정한 랜드마크·고도·출구 방향을 룸별 이미지가 확대해 보여준다. 모든 이미지는 1672×941로 정규화하며, Unity용 패럴랙스 레이어 분리는 룸 콘셉트 검수 이후 별도 작업으로 남긴다.

**Tech Stack:** built-in `image_gen`, Pillow, ImageMagick, PNG, Git

## Global Constraints

- 기준 이미지는 `docs/concept-art/04-fracture-liminal-paradise-v3.png`이다.
- 모든 최종 이미지는 정확히 1672×941이다.
- 파스텔 민트, 빙청색, 라벤더, 옅은 살구색, 백색 대리석 팔레트를 유지한다.
- 캐릭터, 적, 보스, 아이템, UI, 텍스트, 숫자, 라벨, 워터마크를 포함하지 않는다.
- 보라색 와이어프레임 블록아웃을 포함하지 않는다.
- 미세 공포는 잘못된 대칭, 불일치 반사, 미래 고스트, 반복 아치로만 약 5% 표현한다.
- 노골적인 살점, 피, 눈, 이빨, 괴물 실루엣을 포함하지 않는다.
- 인접 룸의 출구 높이와 방향, 보이는 랜드마크는 전체 지도와 일치해야 한다.
- 결과는 `docs/concept-art/generated/fracture-map-v1/`에 저장한다.

---

### Task 1: 출력 구조와 프롬프트 기록

**Files:**
- Create: `docs/concept-art/generated/fracture-map-v1/PROMPTS.md`
- Create: `docs/concept-art/generated/fracture-map-v1/rooms/`
- Create: `docs/concept-art/generated/fracture-map-v1/contact-sheets/`

**Interfaces:**
- Consumes: 승인된 디자인 명세와 기준 이미지
- Produces: 전체 지도와 룸 이미지의 고정 저장 구조

- [ ] **Step 1: 출력 디렉터리 생성**

`docs/concept-art/generated/fracture-map-v1/rooms`와
`docs/concept-art/generated/fracture-map-v1/contact-sheets`를 생성한다.

- [ ] **Step 2: 공통 프롬프트 기록**

`PROMPTS.md`에 기준 이미지 역할, 전체 지도 공통 프롬프트, 룸 공통 프롬프트,
F01~F12와 FS1~FS3의 개별 구도 요구사항을 기록한다.

- [ ] **Step 3: 저장 구조 검증**

```bash
test -d docs/concept-art/generated/fracture-map-v1/rooms
test -d docs/concept-art/generated/fracture-map-v1/contact-sheets
test -f docs/concept-art/generated/fracture-map-v1/PROMPTS.md
```

### Task 2: 캐릭터 없는 전체 지도 생성

**Files:**
- Create: `docs/concept-art/generated/fracture-map-v1/fracture-world-master.png`

**Interfaces:**
- Consumes: `04-fracture-liminal-paradise-v3.png`, 전체 연결 구조
- Produces: 15개 룸 이미지의 구도 기준

- [ ] **Step 1: 전체 지도 편집 생성**

기준 이미지에서 캐릭터와 보라색 와이어프레임을 제거하고 실제 대리석·유리 발판으로
대체한다. 좌측 하단 입구, 중앙 허브, 우측 온실, 중앙 승강축, 상층 감시탑, 최상층 하늘
균열이 하나의 연속된 사이드뷰 공간으로 읽히게 한다.

- [ ] **Step 2: 해상도 정규화**

```bash
magick /tmp/fracture-world-generated.png -resize '1672x941^' -gravity center -extent 1672x941 \
  docs/concept-art/generated/fracture-map-v1/fracture-world-master.png
```

- [ ] **Step 3: 육안 검수**

캐릭터, 와이어프레임, 텍스트가 없고 입구부터 종착점까지 이동 방향이 읽히는지 확인한다.

### Task 3: F01~F06 하층·성소 룸 생성

**Files:**
- Create: `rooms/01-glass-garden.png`
- Create: `rooms/02-misaligned-promenade.png`
- Create: `rooms/03-possibility-plaza.png`
- Create: `rooms/04-swaying-lower-garden.png`
- Create: `rooms/05-foresight-sanctuary.png`
- Create: `rooms/06-time-lag-greenhouse.png`

**Interfaces:**
- Consumes: 전체 지도와 바로 이전 룸 이미지
- Produces: 입구에서 예지 획득과 온실까지 이어지는 하층 탐험 배경

- [ ] **Step 1: F01~F03 생성**

F01은 좌측 입구와 온전한 유리 정원, F02는 무너질 상부 길과 안전한 하부 우회,
F03은 네 방향 출구와 감시탑이 보이는 중앙 허브로 생성한다.

- [ ] **Step 2: F04~F06 생성**

F04는 낮은 수면과 흔들리는 화단, F05는 세로 광선과 예지 제단,
F06은 유리 온실과 서로 다른 주기의 이동 발판 배치가 읽히게 생성한다.

- [ ] **Step 3: 여섯 장 정규화·검수**

각 이미지를 1672×941로 정규화하고 인접 출구 높이와 팔레트 연속성을 확인한다.

### Task 4: F07~F12 상층·종착점 룸 생성

**Files:**
- Create: `rooms/07-floating-architecture.png`
- Create: `rooms/08-reversing-elevator-shaft.png`
- Create: `rooms/09-mirrored-possibility-hall.png`
- Create: `rooms/10-second-hand-watchtower.png`
- Create: `rooms/11-not-yet-ruins.png`
- Create: `rooms/12-tomorrows-fracture.png`

**Interfaces:**
- Consumes: 전체 지도, F06, 바로 이전 룸 이미지
- Produces: 부유 건축군에서 하늘 균열까지 상승하는 상층 탐험 배경

- [ ] **Step 1: F07~F09 생성**

F07은 교차 이동 건축물과 넓은 안전 섬, F08은 수직 승강축과 층별 안전 포켓,
F09는 거의 대칭이지만 반사와 기둥 수가 어긋난 거울 회랑으로 생성한다.

- [ ] **Step 2: F10~F12 생성**

F10은 원형 시계바늘 감시탑, F11은 현재 기초와 미래 폐허 윤곽,
F12는 세 갈래 하늘 균열 아래의 넓은 종착 전장으로 생성한다.

- [ ] **Step 3: 여섯 장 정규화·검수**

각 이미지를 1672×941로 정규화하고 하늘 균열이 진행할수록 가까워지는지 확인한다.

### Task 5: 비밀방 FS1~FS3 생성

**Files:**
- Create: `rooms/S1-abandoned-possibility.png`
- Create: `rooms/S2-still-afternoon.png`
- Create: `rooms/S3-unselected-door.png`

**Interfaces:**
- Consumes: 전체 지도와 각각 F04, F06, F11
- Produces: 세 개의 선택 탐험 배경

- [ ] **Step 1: FS1 생성**

수면 아래에서 반복 기둥이 한순간 정렬되는 침수 통로를 생성한다.

- [ ] **Step 2: FS2 생성**

빛과 꽃가루가 멈춘 작은 온실 정원과 역방향 발판의 도착점을 생성한다.

- [ ] **Step 3: FS3 생성**

빈 벽에 미래 고스트 문 하나만 반복해서 나타나는 미완성 회랑을 생성한다.

- [ ] **Step 4: 세 장 정규화·검수**

각 이미지를 1672×941로 정규화하고 부모 룸과 출입구 및 재질이 일치하는지 확인한다.

### Task 6: 전체 품질 검증과 연락판

**Files:**
- Create: `docs/concept-art/generated/fracture-map-v1/contact-sheets/fracture-map-and-rooms.jpg`

**Interfaces:**
- Consumes: 전체 지도 1장과 룸 15장
- Produces: 최종 검수 증거와 한눈에 보는 결과

- [ ] **Step 1: 파일·크기 검증**

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image
root=Path('docs/concept-art/generated/fracture-map-v1')
files=[root/'fracture-world-master.png', *sorted((root/'rooms').glob('*.png'))]
assert len(files)==16, len(files)
for p in files:
    with Image.open(p) as im:
        assert im.size==(1672,941), (p,im.size)
print('PASS fracture map and rooms 16/16')
PY
```

- [ ] **Step 2: 연락판 생성**

전체 지도를 첫 행에 크게 배치하고 F01~F12, FS1~FS3을 진행 순서로 배열한
1452px 폭 JPEG 연락판을 생성한다.

- [ ] **Step 3: 최종 육안 검수**

전체 지도와 룸별 랜드마크, 출구 방향, 색상, 캐릭터 제거, 미세 공포 강도를 확인한다.

- [ ] **Step 4: 결과 커밋**

```bash
git add docs/concept-art/generated/fracture-map-v1
git commit -m "art: add fracture world map and room concepts"
```
