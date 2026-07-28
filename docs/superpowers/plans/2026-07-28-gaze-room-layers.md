# Gaze Room Layers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 응시 지역 메인 룸 12개와 비밀방 3개를 `BG_Far`, `BG_Mid`, `FG_Overlay`의 45개 Unity용 패럴랙스 이미지로 제작한다.

**Architecture:** 각 룸 콘셉트를 구도 기준으로 삼아 원경은 불투명 전체 화면, 중경과 전경은 균일한 크로마키 원본에서 투명 RGBA로 만든다. 실제 이동 지형과 기물은 기존 `Assets/Art/Gaze/Environment` Sprite가 담당하며 룸 레이어에는 Collider를 만들지 않는다.

**Tech Stack:** built-in `image_gen`, Pillow, `remove_chroma_key.py`, Unity 6.0, PNG/RGBA, Unity Sprite Single

## Global Constraints

- 모든 적용본은 정확히 1672×941이다.
- `BG_Far`는 화면을 완전히 채우는 불투명 이미지다.
- `BG_Mid`와 `FG_Overlay`는 투명 RGBA PNG이며 네 모서리 중 최소 세 곳이 투명하다.
- 마스터의 먹색·푸른 흑색·저채도 보라·제한적인 냉청록 팔레트를 유지한다.
- 거대 하늘 눈, 젖은 감옥 도시, 안구 고가교, 매달린 케이지의 정체성을 유지한다.
- 룸 레이어에는 실제 발판, 활성 눈 장치, 문, 아이템, 캐릭터, 적, 보스, UI, 글자, 워터마크를 넣지 않는다.
- 잔재 지역의 앰버·갈색·뼈·거대 손 모티프를 사용하지 않는다.
- 생성 원본은 `docs/concept-art/generated/gaze-room-layers/`에 보존한다.
- 적용본은 `HiddenWeight/Assets/Art/Gaze/Room01`~`Room12`, `Secret01`~`Secret03`에 저장한다.
- 기존 파일을 덮어쓰지 않고 최초 적용본은 명세의 고정 파일명을 사용한다.

## 공통 생성 프롬프트

각 룸의 세 호출은 해당 룸 원본과
`docs/concept-art/03-gaze-sky-observer-v2-clean-master.png`를 참조한다. 두 번째 룸부터는
바로 이전 룸의 같은 레이어도 연속성 참조로 추가한다.

### `BG_Far` 공통 프롬프트

```text
Use case: stylized-concept.
Asset type: opaque far-background layer for a 2D side-scrolling gothic cosmic-horror metroidvania.
The room concept is the authoritative composition reference and the Gaze master is the authoritative palette and world-identity reference.
Create only the sky, damp low-contrast fog, giant cosmic cloud eye when visible, farthest gothic prison-city silhouettes, distant cathedral towers and distant ocular bridges.
Preserve the exact room viewpoint and landmark placement while removing every player, enemy, item, gameplay platform, foreground column, nearby railing, nearby cage, active eye device, door and readable traversal surface.
The result must fill the entire wide frame with no transparency and must remain visually quiet enough for gameplay sprites.
Palette: charcoal, blue-black, desaturated violet, restrained cold teal.
No warm amber, brown, bones, giant hands, characters, text, labels, UI, watermark, gore or bright daylight.
Strict 16:9-like wide side-view environment; no isometric or top-down view.
```

### `BG_Mid` 공통 프롬프트

```text
Use case: stylized-concept.
Asset type: transparent middle-background layer for a 2D side-scrolling gothic cosmic-horror metroidvania.
The room concept fixes the composition; the Gaze master fixes palette and architecture; the generated BG_Far fixes the empty depth behind this layer.
Create only separated middle-distance gothic buildings, arches, distant cage rails, small spectator balconies, non-playable ocular bridge silhouettes and room-defining architectural structures.
Do not include sky, full-frame fog, foreground framing, walkable platforms, continuous bright top edges, active eye devices, doors, characters, enemies, items or UI.
All visible structures must float as isolated painted elements over one perfectly flat uniform pure #00FF00 chroma-key background.
The green background has no gradient, texture, floor, cast shadow, reflection or lighting variation. Do not use green inside structures.
Palette: charcoal iron, blue-black wet stone, desaturated violet, restrained cold teal.
No text, labels, watermark, gore, warm amber, brown, bones or giant hands.
```

### `FG_Overlay` 공통 프롬프트

```text
Use case: stylized-concept.
Asset type: transparent foreground framing overlay for a 2D side-scrolling gothic cosmic-horror metroidvania.
Use the room concept, Gaze master, BG_Far and BG_Mid as references.
Create only very close dark edge-framing silhouettes: partial gothic arches, side columns, short hanging chains, cropped iron bars, ceiling ribs, lower-corner rubble, subtle near fog and vignette structures.
Keep the central 65 percent of the frame open and never create a walkable horizontal top surface or gameplay collision object.
Every foreground element must touch or remain close to a frame edge and sit over a perfectly flat uniform pure #00FF00 chroma-key background with no gradient, texture, floor, cast shadow or reflection.
Do not use green inside objects.
Palette is near-black charcoal with faint blue-violet and minimal cold-teal reflections; darker than the middle layer.
No characters, enemies, items, eyes that look active, doors, text, labels, UI, watermark, gore, warm amber, brown, bones or giant hands.
```

---

### Task 1: 원본 보존과 출력 구조

**Files:**
- Create: `docs/concept-art/generated/gaze-room-layers/PROMPTS.md`
- Create: `docs/concept-art/generated/gaze-room-layers/contact-sheets/`
- Create: `HiddenWeight/Assets/Art/Gaze/Room01/` through `Room12/`
- Create: `HiddenWeight/Assets/Art/Gaze/Secret01/` through `Secret03/`

**Interfaces:**
- Consumes: 응시 룸 콘셉트 15장
- Produces: 생성 원본·적용본 저장 구조와 원본 보존 커밋

- [ ] **Step 1: 룸 콘셉트 15장 검증**

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image
root=Path('docs/concept-art/generated/gaze-rooms-v1')
files=sorted(root.glob('*.png'))
assert len(files)==15, len(files)
for p in files:
    with Image.open(p) as im:
        assert im.size==(1672,941), (p,im.size)
print('PASS room concepts=15 dimensions=1672x941')
PY
```

- [ ] **Step 2: 미추적 원본 보존**

`gaze-rooms-v1`의 아직 커밋되지 않은 룸 이미지와 연락판만 스테이징하고 다른 사용자 파일은
건드리지 않는다.

```bash
git add docs/concept-art/generated/gaze-rooms-v1
git commit -m "[art] 응시 지역 룸 콘셉트 15장 보존"
```

- [ ] **Step 3: 출력 폴더 생성**

```bash
mkdir -p docs/concept-art/generated/gaze-room-layers/contact-sheets
for n in 01 02 03 04 05 06 07 08 09 10 11 12; do
  mkdir -p "HiddenWeight/Assets/Art/Gaze/Room${n}"
done
for n in 01 02 03; do
  mkdir -p "HiddenWeight/Assets/Art/Gaze/Secret${n}"
done
```

- [ ] **Step 4: 프롬프트 기록 파일 생성**

공통 프롬프트와 아래 룸별 핵심 표, 생성 원본 경로, 적용본 경로, 실제 이미지 생성 호출 결과를
`PROMPTS.md`에 기록한다.

### Task 2: G01~G03 하층 진입 레이어 9장

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Room01/Room01_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room02/Room02_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room03/Room03_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: `01-entry-threshold`, `02-exposed-plaza`, `03-lower-prison-district`
- Produces: 지역 진입·노출 광장·하층 감옥의 패럴랙스 레이어

| 룸 | `BG_Far` 추가 조건 | `BG_Mid` 추가 조건 | `FG_Overlay` 추가 조건 |
| --- | --- | --- | --- |
| G01 | 먼 감시탑과 부분 하늘 눈, 입구 너머 안구교 | 낮은 도시벽과 먼 아치 | 좌측 눈꺼풀형 터널벽, 우측 얇은 철창 |
| G02 | 중앙 거대 눈과 깊은 도시 협곡 | 끊어진 비활성 다리와 먼 케이지 | 좌우 교각 일부와 상단 짧은 사슬 |
| G03 | 허브 뒤 감시탑과 상층 극장 목표 | 관객 건물군과 잠긴 경로의 배경 프레임 | 광장 좌우 아치와 하단 잔해 |

- [ ] **Step 1: 룸별 `BG_Far` 세 장 생성·저장**

각 룸 원본에 `BG_Far` 공통 프롬프트와 표의 추가 조건을 결합한다. 생성 결과를 1672×941로
정규화해 불투명 PNG로 저장한다.

- [ ] **Step 2: 룸별 `BG_Mid` 세 장 생성·크로마 제거**

각 룸 원본, 마스터, 해당 `BG_Far`를 참조한다. 크로마 제거 명령:

```bash
python "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py" \
  --input SOURCE.png --out OUTPUT.png --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill
```

실행할 때 `SOURCE.png`와 `OUTPUT.png`를 해당 룸의 실제 경로로 치환한다.

- [ ] **Step 3: 룸별 `FG_Overlay` 세 장 생성·크로마 제거**

각 룸 원본과 앞의 두 적용본을 참조한다. 중앙 개방과 프레임 가장자리 조건을 육안 확인한다.

- [ ] **Step 4: 9장 검증·커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze/Room01 \
  HiddenWeight/Assets/Art/Gaze/Room02 \
  HiddenWeight/Assets/Art/Gaze/Room03
git commit -m "[art] 응시 G01-G03 룸 레이어 추가"
```

### Task 3: G04~G06 성소·응시 통로 레이어 9장

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Room04/Room04_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room05/Room05_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room06/Room06_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: `04-hushed-crevice`, `05-fixed-gaze-hall`, `06-rotating-gaze-arcade`
- Produces: 하층 새장원·숨죽임 성소·속삭임 통로의 레이어

| 룸 | `BG_Far` 추가 조건 | `BG_Mid` 추가 조건 | `FG_Overlay` 추가 조건 |
| --- | --- | --- | --- |
| G04 | 좁은 도시 틈과 아주 약한 보라 안개 | 철창 벽, 비활성 눈 부조, 케이지 군집 | 낮은 터널 천장과 양쪽 두꺼운 벽 |
| G05 | 성소 뒤 도시와 부분 안구교 | 큰 고정 눈의 건축 프레임과 고딕 아치 | 내부 기둥과 상단 어두운 아치 |
| G06 | 먼 교차 안구교와 감옥 도시 | 회전 홍채가 들어갈 빈 벽틀과 아케이드 | 터널 상부·좌우 암부와 짧은 사슬 |

- [ ] **Step 1: `BG_Far` 세 장 생성·정규화**
- [ ] **Step 2: `BG_Mid` 세 장 생성·크로마 제거**
- [ ] **Step 3: `FG_Overlay` 세 장 생성·크로마 제거**
- [ ] **Step 4: 해상도·알파·중앙 개방 확인 후 커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze/Room04 \
  HiddenWeight/Assets/Art/Gaze/Room05 \
  HiddenWeight/Assets/Art/Gaze/Room06
git commit -m "[art] 응시 G04-G06 룸 레이어 추가"
```

### Task 4: G07~G09 승강·상층 레이어 9장

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Room07/Room07_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room08/Room08_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room09/Room09_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: `07-hanging-cage-lift`, `08-upper-cage-transit`, `09-faceless-audience-gallery`
- Produces: 회전 홍채교·승강정·상층 관객석의 레이어

| 룸 | `BG_Far` 추가 조건 | `BG_Mid` 추가 조건 | `FG_Overlay` 추가 조건 |
| --- | --- | --- | --- |
| G07 | 수직 도시와 하늘 눈 외곽 | 승강축, 교차 안구교, 먼 레일 | 좌우 탑 벽과 아래로 떨어지는 사슬 |
| G08 | 중앙 하늘 눈과 상층 도시 | 먼 환승교와 수직 레일 군집 | 화면 양쪽 승강탑 프레임 |
| G09 | 가까워진 하늘 눈과 관객 도시 | 곡선 관객석, 케이지, 먼 아치 | 극장 상부 아치와 좌우 관객석 암부 |

- [ ] **Step 1: `BG_Far` 세 장 생성·정규화**
- [ ] **Step 2: `BG_Mid` 세 장 생성·크로마 제거**
- [ ] **Step 3: `FG_Overlay` 세 장 생성·크로마 제거**
- [ ] **Step 4: 해상도·알파·중앙 개방 확인 후 커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze/Room07 \
  HiddenWeight/Assets/Art/Gaze/Room08 \
  HiddenWeight/Assets/Art/Gaze/Room09
git commit -m "[art] 응시 G07-G09 룸 레이어 추가"
```

### Task 5: G10~G12 감시탑·대성당·보스 레이어 9장

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Room10/Room10_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room11/Room11_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Room12/Room12_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: `10-optic-nerve-viaduct`, `11-great-ocular-cathedral`, `12-pupil-sanctum`
- Produces: 감시탑·자각 대성당·지역 보스방의 레이어

| 룸 | `BG_Far` 추가 조건 | `BG_Mid` 추가 조건 | `FG_Overlay` 추가 조건 |
| --- | --- | --- | --- |
| G10 | 거대한 하늘 눈과 교차 고가교 | 먼 감시탑·하부 도시·비활성 교각 | 좌우 탑 난간과 사슬 비네트 |
| G11 | 깨진 성당 천장과 바로 위 하늘 눈 | 대안구 장미창·성당 기둥·먼 케이지 | 거울 회랑의 높은 좌우 기둥 |
| G12 | 동공처럼 정렬된 하늘 눈과 도시 원환 | 대형 홍채 창·먼 관객석·원형 리브 | 원형 극장 좌우 프레임과 상부 사슬 |

- [ ] **Step 1: `BG_Far` 세 장 생성·정규화**
- [ ] **Step 2: `BG_Mid` 세 장 생성·크로마 제거**
- [ ] **Step 3: `FG_Overlay` 세 장 생성·크로마 제거**
- [ ] **Step 4: 해상도·알파·중앙 개방 확인 후 커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze/Room10 \
  HiddenWeight/Assets/Art/Gaze/Room11 \
  HiddenWeight/Assets/Art/Gaze/Room12
git commit -m "[art] 응시 G10-G12 룸 레이어 추가"
```

### Task 6: 비밀방 GS1~GS3 레이어 9장

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Secret01/Secret01_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Secret02/Secret02_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Secret03/Secret03_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: `S1-flooded-observation-cell`, `S2-hidden-cage-archive`, `S3-blind-chamber`
- Produces: 침수 감시실·케이지 기록고·맹점 방의 레이어

| 룸 | `BG_Far` 추가 조건 | `BG_Mid` 추가 조건 | `FG_Overlay` 추가 조건 |
| --- | --- | --- | --- |
| GS1 | 창밖 안구교와 왜곡된 보라 반사 | 눈꺼풀 창·침수 감옥 벽·먼 철창 | 낮은 천장·수면 하단 암부 |
| GS2 | 기록고 틈 밖 케이지 축과 도시 | 감시 기록벽·빈 케이지·작은 눈 부조 | 근접 서고벽과 잘린 철창 |
| GS3 | 천장 틈의 하늘 눈 외곽만 노출 | 닫힌 눈 회랑·숨은 문틀·희미한 냉청록 선 | 거의 검은 아치와 가장자리 잔상 |

- [ ] **Step 1: `BG_Far` 세 장 생성·정규화**
- [ ] **Step 2: `BG_Mid` 세 장 생성·크로마 제거**
- [ ] **Step 3: `FG_Overlay` 세 장 생성·크로마 제거**
- [ ] **Step 4: 해상도·알파·중앙 개방 확인 후 커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze/Secret01 \
  HiddenWeight/Assets/Art/Gaze/Secret02 \
  HiddenWeight/Assets/Art/Gaze/Secret03
git commit -m "[art] 응시 비밀방 룸 레이어 추가"
```

### Task 7: Unity 임포트 자동 설정

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Editor/GazeRoomArtImporter.cs`
- Create: `HiddenWeight/Assets/Tests/EditMode/GazeRoomArtImporterTests.cs`

**Interfaces:**
- Consumes: 룸 레이어 적용본 45장
- Produces: Sprite Single, PPU 32, Bilinear, Mipmap Off, Clamp, Uncompressed 설정

- [ ] **Step 1: 실패하는 EditMode 테스트 작성**

테스트는 `Room01`~`Room12`, `Secret01`~`Secret03`의 세 고정 파일명을 순회해 45개 모두
`TextureImporterType.Sprite`, `SpriteImportMode.Single`, PPU 32, Bilinear, Mipmap Off,
Clamp, Uncompressed인지 실제 `TextureImporter`에서 확인한다.

- [ ] **Step 2: 테스트가 기본 임포트 설정 때문에 실패하는지 확인**

```bash
Unity -batchmode -nographics -projectPath HiddenWeight -runTests \
  -testPlatform EditMode \
  -testFilter HiddenWeight.Tests.GazeRoomArtImporterTests \
  -testResults /tmp/gaze-room-importer-red.xml \
  -logFile /tmp/gaze-room-importer-red.log
```

Expected: 새 룸 PNG의 기본 `Texture` 설정 또는 PPU 100 때문에 FAIL.

- [ ] **Step 3: 최소 임포터 구현**

`GazeRoomArtImporter.ConfigureAll()`은 `Assets/Art/Gaze/Room*`와 `Secret*`의 PNG만 찾아
명세의 고정 설정을 적용하고 `SaveAndReimport()`한다. `BG_Mid`와 `FG_Overlay`만
`alphaIsTransparency = true`로 설정한다.

- [ ] **Step 4: 임포터 실행 후 테스트 통과 확인**

`-executeMethod HiddenWeight.EditorTools.GazeRoomArtImporter.ConfigureAll`로 적용한 뒤 같은
테스트를 실행한다. Expected: 1 test passed, 0 failed.

- [ ] **Step 5: 커밋**

```bash
git add HiddenWeight/Assets/Art/Gaze \
  HiddenWeight/Assets/Scripts/Editor/GazeRoomArtImporter.cs \
  HiddenWeight/Assets/Tests/EditMode/GazeRoomArtImporterTests.cs
git commit -m "[feat] 응시 룸 레이어 Unity 임포트 설정"
```

### Task 8: 합성 미리보기와 최종 검증

**Files:**
- Create: `docs/concept-art/generated/gaze-room-layers/contact-sheets/<Room>_Composite.jpg` 15장
- Create: `docs/concept-art/generated/gaze-room-layers/gaze-room-layers-contact-sheet.jpg`
- Modify: `docs/concept-art/generated/gaze-room-layers/PROMPTS.md`

**Interfaces:**
- Consumes: 45개 적용본
- Produces: 룸별 합성 미리보기, 전체 연락판, 검증 결과

- [ ] **Step 1: 45장 파일·해상도·알파 검증**

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image
root=Path('HiddenWeight/Assets/Art/Gaze')
rooms=[f'Room{i:02d}' for i in range(1,13)]+[f'Secret{i:02d}' for i in range(1,4)]
count=0
for room in rooms:
    for layer in ('BG_Far','BG_Mid','FG_Overlay'):
        p=root/room/f'{room}_{layer}.png'
        im=Image.open(p)
        assert im.size==(1672,941), (p,im.size)
        if layer=='BG_Far':
            assert im.mode in ('RGB','RGBA')
            if im.mode=='RGBA':
                assert im.getchannel('A').getextrema()==(255,255)
        else:
            assert im.mode=='RGBA', (p,im.mode)
            corners=[(0,0),(1671,0),(0,940),(1671,940)]
            assert sum(im.getpixel(q)[3]==0 for q in corners)>=3, p
        count+=1
print('PASS room layers',count)
assert count==45
PY
```

- [ ] **Step 2: 룸별 합성 미리보기 15장 생성**

Pillow로 `BG_Far` 위에 `BG_Mid`, `FG_Overlay`를 순서대로 알파 합성한다. 합성본은 원본
콘셉트와 나란히 배치해 `contact-sheets/<Room>_Composite.jpg`로 저장한다.

- [ ] **Step 3: 전체 5×3 연락판 생성**

15개 합성본을 G01~G12, GS1~GS3 순서로 배치하고 룸 이름을 표시한다.

- [ ] **Step 4: 프로젝트 전체 테스트**

Unity EditMode와 PlayMode 전체 테스트를 실행하고 실패가 0인지 확인한다.

- [ ] **Step 5: 기록·검증 결과 커밋**

```bash
git add docs/concept-art/generated/gaze-room-layers \
  HiddenWeight/Assets/Art/Gaze
git commit -m "[art] 응시 룸 레이어 45장 제작 완료"
```

