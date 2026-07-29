# Gaze Animation Sprites Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 응시 지역의 적, 보스, 위험물, 장치, 아이템, 환경 및 VFX에 필요한 18종의 투명 PNG 애니메이션 시트를 제작한다.

**Architecture:** 기존 응시 환경 아틀라스와 방 배경을 시각 참조로 사용하고, 캐릭터·보스 디자인은 레벨 문서의 행동에서 새로 도출한다. 각 결과는 단색 크로마키 배경으로 생성한 뒤 RGBA로 변환하고, 요청보다 많은 열이 생성되면 동작 흐름을 보존하는 8개 프레임으로 재배열한다.

**Tech Stack:** Built-in ImageGen, PNG/RGBA, ImageMagick, Pillow, Unity

## Global Constraints

- 범위는 2맵 응시이며 플레이어 스프라이트는 제외한다.
- 저채도 보라, 청록, 먹색과 눈·홍채·철창·고딕 석조 모티브를 유지한다.
- 기존 파일을 덮어쓰지 않는다.
- 모든 최종 파일은 투명 RGBA PNG다.
- 이동 캐릭터는 Bottom Center, 중심 VFX는 Center 피벗을 사용한다.
- 공격은 예고, 유효, 회수가 최소 2프레임 간격으로 읽혀야 한다.
- 텍스트, 숫자, 셀 선, 워터마크, 배경 장면을 넣지 않는다.

---

### Task 1: 일반 적 4종

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/BlindPilgrim_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/InformingMouth_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/HangingAudience_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/FacelessJudge_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Props/Gaze_EnvironmentProps_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Gaze_EyeHazards_v1.png`

**Interfaces:**
- Produces: 각 8×6, Idle / Move / Telegraph / Attack / Hit / Death

- [ ] **Step 1: 네 적을 각각 별도 ImageGen 호출로 생성한다**

```text
Create an exactly 8-column by 6-row production-ready side-view enemy animation sprite sheet.
Rows: idle, movement, attack telegraph, attack and recovery, hit, irreversible death.
Match the referenced Gaze environment: desaturated violet, teal, charcoal, gothic cages, eyelid curves and restrained magenta iris light.
Keep anatomy, eye count, scale and bottom-center ground contact consistent.
Flat solid #00ff00 chroma-key background. No text, grid, scenery, player or extra creatures.
```

적별 행동:

- Blind Pilgrim: 지팡이로 앞을 더듬고 소리에 반응, 공격은 지팡이 찌르기.
- Informing Mouth: 입과 철창이 결합된 부유 지원형, 비명으로 시선 장치 활성화.
- Hanging Audience: 천장 관객 우리, 시선 그림자 예고 후 낙하.
- Faceless Judge: 무거운 가면 없는 재판관, 정면 방어와 느린 강공.

- [ ] **Step 2: 크로마키를 제거해 대응 최종 경로에 저장한다**

```bash
python /Users/ksh/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py \
  --input source.png --out final.png \
  --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill
```

- [ ] **Step 3: 각 결과를 8×6 격자로 정규화한다**

출력이 8열보다 많으면 각 행에서 첫 프레임, 예고 시작, 중간 변형, 최대 변형, 회수,
종료를 유지하도록 8개를 선택하고 ImageMagick `montage -tile 8x6`으로 재배열한다.

- [ ] **Step 4: RGBA·격자·투명 모서리를 검사한다**

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
for p in Path("HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation").glob("*.png"):
    im=Image.open(p)
    assert im.mode=="RGBA" and im.width%8==0 and im.height%6==0
    assert all(im.getpixel(q)[3]==0 for q in ((0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)))
    print("PASS",p,im.size)
PY
```

- [ ] **Step 5: 적 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation
git commit -m "art: add gaze enemy animation sprites"
```

### Task 2: 홍채의 문지기

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/IrisGatekeeper_Combat_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/IrisGatekeeper_Transitions_v1.png`
- Reference: `Gaze_EyeHazards_v1.png`, `Gaze_DoorsShortcuts_v1.png`, `Room10_BG_Mid.png`

**Interfaces:**
- Produces: 8×7 전투, 8×3 등장·전환

- [ ] **Step 1: 전투 시트를 생성한다**

```text
Create an exactly 8-column by 7-row side-view boss sprite sheet for the Iris Gatekeeper.
Design: a tall gothic iris-door guardian with eyelid armor, rotating violet iris core, hooked stone limbs and teal gaze seams.
Rows: idle, iris sweep, eyelid close, charge judgment, dual gaze, hurt, death.
Every attack shows warning, active state and recovery. Flat #00ff00 background; no text, grid or scenery.
```

- [ ] **Step 2: 전환 시트를 생성한다**

```text
Create an exactly 8-column by 3-row Iris Gatekeeper transition sheet.
Rows: doorway entrance unfolding into boss, half-health iris overload, defeated shortcut gate opening.
Preserve the exact boss design and fixed bottom-center anchor. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 3: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/IrisGatekeeper_*.png
git commit -m "art: add iris gatekeeper animation sprites"
```

### Task 3: 만인의 시선

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeOfAll_Combat_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeOfAll_Deceptions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeOfAll_Reactions_v1.png`
- Reference: `Gaze_EnvironmentProps_v1.png`, `Gaze_EyeHazards_v1.png`, `Room12_BG_Mid.png`

**Interfaces:**
- Produces: 8×7 전투, 8×4 기만, 8×3 반응

- [ ] **Step 1: 전투 시트를 생성한다**

```text
Create an exactly 8-column by 7-row side-view region-boss sprite sheet for Gaze of All.
Design: an uncanny floating theater idol made of audience masks, nested irises, black cage ribs and violet cloth, with one hidden true teal eye.
Rows: idle, fixed gaze, rotating gaze, eye projectile, true strike, hurt, death.
Keep eye count and arrangement stable except during intentional truth reveal. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 2: 기만 시트를 생성한다**

```text
Create an exactly 8-column by 4-row deception sprite sheet matching Gaze of All.
Rows: false telegraph from many masks, true telegraph revealed by teal shadow, delayed player-free clone silhouette attack, empty-stage disappearance.
Flat #00ff00 background; no player body, text, grid or scenery.
```

- [ ] **Step 3: 반응 시트를 생성한다**

```text
Create an exactly 8-column by 3-row Gaze of All reaction sheet.
Rows: awareness exposes the single true eye, final confrontation facing forward, defeated audience masks turn away from the player.
Preserve palette, scale and center anchor. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 4: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeOfAll_*.png
git commit -m "art: add gaze of all animation sprites"
```

### Task 4: 위험물과 장치

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Animation/EyeHazardTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/CoverTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/TransitTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/AwarenessObjectTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeArenaTransitions_v1.png`
- Reference: 응시 Environment의 대응 정적 아틀라스 5종

**Interfaces:**
- Produces: 8×5 위험, 8×4 엄폐, 8×3 이동, 8×4 능력, 8×3 전장

- [ ] **Step 1: 위험물 시트를 생성한다**

```text
Create an exactly 8-column by 5-row Gaze hazard transition sheet.
Rows: fixed wall eye, rotating iris, floor gaze, ceiling eye, detection alarm burst.
Each row clearly reads dormant, opening, warning, active detection and closing.
Match violet stone, teal edges and magenta iris. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 2: 엄폐·이동·능력·전장 시트를 각각 생성한다**

```text
Match the referenced Gaze environment exactly: gothic violet stone, black cages, eyelid curves, teal memory edges and magenta iris light.
Animate fixed physical anchors and readable transitions.
Cover rows: eyelid pillar, curtain cover, cage cover, boss cover.
Transit rows: gaze lift, iris bridge, hanging platform.
Awareness rows: hush shrine, awareness mark, mirror door, hidden inner eye.
Arena rows: arena lock, rotating cover, fracture exit.
Flat #00ff00 background; no text, grid, player or scenery.
```

- [ ] **Step 3: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Animation
git add HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation
git add HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/GazeArenaTransitions_v1.png
git commit -m "art: add gaze hazard and mechanism transitions"
```

### Task 5: 아이템과 체크포인트

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Items/Animation/GazeCollectibleTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/Items/Animation/GazeCheckpointTransitions_v1.png`
- Reference: `Gaze_AbilityObjects_v1.png`, `Gaze_EnvironmentProps_v1.png`

**Interfaces:**
- Produces: 8×4 획득, 8×3 체크포인트

- [ ] **Step 1: 수집물 시트를 생성한다**

```text
Create an exactly 8-column by 4-row centered Gaze collectible acquisition sheet.
Rows: violet currency, teal healing reliquary, memory fragment, awareness fragment.
Each item lifts, compresses into light, streams away and disappears by frame eight.
Flat #00ff00 background; no text, grid, player or scenery.
```

- [ ] **Step 2: 체크포인트 시트를 생성한다**

```text
Create an exactly 8-column by 3-row Gaze checkpoint shrine transition sheet.
Design: closed eyelid shrine in violet stone and black cage metal.
Rows: activation and iris opening, teal healing pulse, respawn release.
Keep base fixed. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 3: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Gameplay/Items/Animation
git commit -m "art: add gaze item and checkpoint transitions"
```

### Task 6: 환경 루프·VFX·전체 검수

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/GazeAmbientMotion_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/GazeSecondaryVFX_v1.png`
- Create: `docs/concept-art/generated/gaze-animation-sprites/PROMPTS.md`
- Create: `docs/concept-art/generated/gaze-animation-sprites/contact-sheets/gaze-animation-sprites.jpg`

**Interfaces:**
- Produces: 8×4 환경 루프, 8×4 VFX, 기록 문서, 연락 시트

- [ ] **Step 1: 환경 루프를 생성한다**

```text
Create an exactly 8-column by 4-row seamless ambient Gaze animation sheet.
Rows: hanging cage sway, audience cloth breathing flutter, low teal fog drift, distant background eyes blinking asynchronously.
Keep motion subtle and gameplay-readable. Flat #00ff00 background; no text or grid.
```

- [ ] **Step 2: 보조 VFX를 생성한다**

```text
Create an exactly 8-column by 4-row centered Gaze gameplay VFX sheet.
Rows: detection warning, gaze hit, judge guard break, boss truth reveal.
Effects start compact, peak near the middle and dissipate. Violet, teal, charcoal and restrained magenta.
Flat #00ff00 background; no text, grid, character body or scenery.
```

- [ ] **Step 3: 18개 파일의 프롬프트·격자·권장 FPS를 기록한다**

`PROMPTS.md`에 파일명, 참조 이미지, 최종 프롬프트, 격자, 셀 크기, 권장 FPS를 기록한다.

- [ ] **Step 4: 전체 연락 시트를 생성한다**

```bash
mkdir -p docs/concept-art/generated/gaze-animation-sprites/contact-sheets
montage $(find HiddenWeight/Assets/Art/Gaze -type f -path '*/Animation/*.png' | sort) \
  -thumbnail 460x360 -tile 4x -geometry +12+28 \
  -background '#17151c' -fill '#e5dfd0' -pointsize 15 -set label '%t' \
  docs/concept-art/generated/gaze-animation-sprites/contact-sheets/gaze-animation-sprites.jpg
```

- [ ] **Step 5: 18개 파일의 RGBA·격자·투명 모서리를 검사한다**

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
files=list(Path("HiddenWeight/Assets/Art/Gaze").rglob("Animation/*.png"))
assert len(files)==18, len(files)
for p in files:
    im=Image.open(p)
    assert im.mode=="RGBA" and im.width%8==0
    assert all(im.getpixel(q)[3]==0 for q in ((0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)))
    print("PASS",p,im.size)
PY
```

- [ ] **Step 6: 전체 결과를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze docs/concept-art/generated/gaze-animation-sprites
git commit -m "art: complete gaze animation sprite set"
```

