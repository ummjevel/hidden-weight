# Gaze Environment Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 응시 지역 15개 룸을 구성할 수 있는 모듈형 환경 아틀라스 9종을 생성하고 투명 RGBA Unity 적용본으로 저장한다.

**Architecture:** 각 아틀라스는 균일한 6×4, 6×3 또는 8×3 격자의 크로마키 원본으로 생성한다. 생성 원본은 문서 폴더에 보존하고, 로컬 크로마 제거 후 Unity 적용 폴더에 RGBA PNG로 저장하며, 충돌과 게임 상태는 이미지가 아닌 기존 Unity 컴포넌트가 담당한다.

**Tech Stack:** built-in `image_gen`, Pillow, `remove_chroma_key.py`, PNG/RGBA, Unity Sprite Multiple

## Global Constraints

- 기준 이미지는 `docs/concept-art/03-gaze-sky-observer-v2-clean-master.png`와 `docs/concept-art/generated/gaze-rooms-v1/`이다.
- 팔레트는 먹색 철골, 푸른 흑색 석재, 저채도 보라, 제한적인 냉청록으로 고정한다.
- 생성 원본 배경은 완전히 균일한 `#00FF00`이며 바닥면, 투영 그림자, 풍경, 인물, 글자, UI를 넣지 않는다.
- 각 셀은 같은 측면 시점과 8% 이상의 안전 여백을 유지한다.
- 발판 윗면은 연속적인 냉청록·회보라 명도선으로 표시한다.
- 보라 발광은 시선 위험·홍채·자각 반응에만 사용하고 장식은 더 어둡게 유지한다.
- 상태 프레임은 같은 열에서 피벗과 바깥 프레임을 고정한다.
- 생성 원본은 `docs/concept-art/generated/gaze-environment-assets/`에 저장한다.
- Unity 적용본은 `HiddenWeight/Assets/Art/Gaze/Environment/` 하위 기능 폴더에 저장한다.
- 기존 파일을 덮어쓰지 않고 최초 결과는 `_v1` 이름을 사용한다.

---

### Task 1: 출력 구조와 검증 도구 준비

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Props/`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/`

**Interfaces:**
- Consumes: `docs/superpowers/specs/2026-07-28-gaze-environment-art-design.md`
- Produces: 아틀라스 저장 폴더와 프롬프트 기록 파일

- [ ] **Step 1: 출력 폴더 생성**

Run:

```bash
mkdir -p \
  docs/concept-art/generated/gaze-environment-assets \
  HiddenWeight/Assets/Art/Gaze/Environment/{Terrain,Hazards,Interactables,Props,VFX}
```

Expected: 여섯 경로가 존재하고 기존 파일은 삭제되지 않는다.

- [ ] **Step 2: 프롬프트 기록 문서 생성**

`PROMPTS.md`에 설계 문서 경로, 공통 팔레트, 공통 크로마키 조건, 각 아틀라스의 최종 프롬프트와 생성 원본·적용본 경로를 기록한다.

- [ ] **Step 3: 크로마 제거 도구 확인**

Run:

```bash
test -f "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py"
python3 -c "from PIL import Image; print(Image.__version__)"
```

Expected: 스크립트 검사 exit 0, Pillow 버전 출력.

- [ ] **Step 4: 준비 상태 커밋**

```bash
git add docs/concept-art/generated/gaze-environment-assets/PROMPTS.md
git commit -m "[art] 응시 환경 아틀라스 제작 구조 준비"
```

### Task 2: 지형 타일 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_TerrainTiles_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: 응시 마스터와 G01, G03, G05, G11 룸 이미지
- Produces: 6×4 지형 모듈 24셀

- [ ] **Step 1: 6×4 크로마키 원본 생성**

Built-in image generation prompt:

```text
Use case: stylized-concept
Asset type: modular 2D metroidvania terrain sprite atlas, exactly 6 columns by 4 rows
Reference image: the Gaze region master is authoritative for palette, gothic shapes and wet materials
Subject: row 1 short, medium and long floor, left cap, right cap, center join; row 2 outer corners left/right, inner corners left/right, straight ceiling, ceiling cap; row 3 short and long vertical wall, wall top and bottom, gothic arch support, ocular column support; row 4 shallow slopes left/right, steep slopes left/right, barred floor, broken edge
Style: hand-painted gothic cosmic-horror side-view game sprites, charcoal iron, blue-black wet stone, desaturated violet and limited cold teal
Layout: exactly 24 independent centered cells, identical square cell boundaries, 8 percent padding, nothing crossing a cell border
Gameplay readability: walkable top surfaces have one continuous pale cold-teal or gray-violet rim
Background: perfectly flat solid #00FF00 chroma key with no shadow, gradient, texture, reflection or floor plane
Constraints: no scenery, characters, enemies, eyes that look active, text, labels, UI or watermark; do not use #00FF00 in any sprite
```

Copy the generated output to the source path.

- [ ] **Step 2: Remove chroma key**

```bash
python "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py" \
  --input docs/concept-art/generated/gaze-environment-assets/Gaze_TerrainTiles_v1_Source.png \
  --out HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png \
  --auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill
```

- [ ] **Step 3: Verify RGBA and divisible grid**

```bash
python3 - <<'PY'
from PIL import Image
p='HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png'
im=Image.open(p)
assert im.mode == 'RGBA'
assert im.size[0] % 6 == 0 and im.size[1] % 4 == 0
assert im.getpixel((0,0))[3] == 0
print('PASS terrain', im.size)
PY
```

- [ ] **Step 4: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Terrain
git commit -m "[art] 응시 지형 타일 아틀라스 추가"
```

### Task 3: 이동 발판 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_Platforms_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Gaze_Platforms_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G02, G07, G08, G10 룸 이미지
- Produces: 6×3 발판 18셀

- [ ] **Step 1: 6×3 크로마키 원본 생성**

```text
Use case: stylized-concept
Asset type: modular 2D side-view platform sprite atlas, exactly 6 columns by 3 rows
Subject: row 1 three small wet stone platforms and three small iron platforms; row 2 two medium stone, two medium iron and two eye-bridge fragments; row 3 three hanging platforms, two cage platforms and one thin floating stone slab
Style and palette: exact Gaze region visual language, charcoal gothic iron, blue-black wet stone, desaturated violet, restrained cold teal edges, subtle closed-eye engravings
Layout: 18 isolated sprites in a precise 6x3 grid, identical square cells, bottom-center alignment, 8 percent padding, no overlap
Gameplay readability: every walkable top edge is continuous and lighter than decorative surfaces; chains stop inside their own cell
Background: perfectly flat #00FF00, no shadows, gradient, floor or reflection
Avoid: scenery, characters, enemies, active eye glow, labels, text, UI, watermark and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Use the Task 2 helper command with the platform source and output paths. Verify `RGBA`, width divisible by 6, height divisible by 3, and transparent corner alpha.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Terrain
git commit -m "[art] 응시 이동 발판 아틀라스 추가"
```

### Task 4: 시선 위험 장치 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_EyeHazards_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Gaze_EyeHazards_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G02, G07, G10, G12 룸 이미지와 `GazeHazard`
- Produces: 6개 장치의 휴면·예고·활성·회복 24셀

- [ ] **Step 1: 6×4 상태 아틀라스 생성**

```text
Use case: stylized-concept
Asset type: 2D metroidvania animated hazard sprite atlas, exactly 6 columns by 4 rows
Columns fixed by device: small fixed architectural eye, large fixed eye, rotating iris mechanism, ceiling surveillance eye, floor-sweeping eye, boss arena eyelid shutter
Rows fixed by state: dormant closed, warning half-open, active fully open, recovery closing
Invariants: each column depicts the exact same device from the exact same side view, scale, pivot and outer frame across all four states; only eyelid, pupil, iris rotation and violet intensity change
Style: Gaze region gothic stone-and-iron architecture, non-gory mechanical-organic eyes, blue-black stone, charcoal metal, desaturated violet and minimal cold teal
Readability: warning has a narrowed brighter iris; active is brightest; dormant is darkest; do not include vision cones or beams
Layout: exact 6x4 grid, centered isolated objects, 8 percent padding
Background: perfectly flat #00FF00 with no floor, shadow, reflection or texture
Avoid: scenery, flesh, blood, characters, enemies, text, labels, UI, watermark and green in the objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×4 divisibility, transparent corners and identical opaque bounding-box center within a tolerance of 5% for each column.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Hazards
git commit -m "[art] 응시 시선 위험 장치 아틀라스 추가"
```

### Task 5: 엄폐 구조물 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_CoverObjects_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_CoverObjects_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G02, G07, G10, G12 룸 이미지
- Produces: 6×3 시선 차단 기물 18셀

- [ ] **Step 1: 6×3 원본 생성**

```text
Use case: stylized-concept
Asset type: modular 2D side-view gaze-blocking cover atlas, exactly 6 columns by 3 rows
Subject: row 1 two short columns, two tall columns, broken columns left/right; row 2 two closed-eyelid screens, two barred screens, two movable shielding plates; row 3 two statue covers, two cage covers, two low rubble covers
Style: exact Gaze region gothic prison-cathedral materials and palette
Readability: full-cover objects have strong vertical silhouettes; decorative top edges are broken so they do not look walkable; no eye is actively glowing
Layout: 18 isolated bottom-aligned sprites in exact 6x3 equal cells with 8 percent padding
Background: perfectly uniform #00FF00 without shadow, floor, reflection, scenery or texture
Avoid: characters, enemies, text, labels, UI, watermark, gore and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×3 divisibility and transparent corners.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Interactables
git commit -m "[art] 응시 엄폐 구조물 아틀라스 추가"
```

### Task 6: 승강·수송 구조물 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_TransitStructures_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_TransitStructures_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G08 룸 이미지와 `MovingPlatform`
- Produces: 6개 구조물의 정지·흔들림 A·흔들림 B 18셀

- [ ] **Step 1: 6×3 상태 원본 생성**

```text
Use case: stylized-concept
Asset type: 2D side-view gothic transit mechanism animation atlas, exactly 6 columns by 3 rows
Columns: small hanging cage, large cage elevator, suspended platform, vertical rail platform, pulley-counterweight assembly, cage transfer bridge
Rows: idle, moving sway A, moving sway B and settling
Invariants: same mechanism, exact scale, bottom-center pivot, platform top and outer support frame fixed in every row; only hanging chains, cage body and small secondary parts sway
Style: Gaze master palette and prison-cathedral ironwork, subtle violet ocular hardware, cold teal playable top rim
Layout: exact 6x3 equal grid with 8 percent padding and no cross-cell chains
Background: perfectly flat #00FF00, no floor, shadow, reflection or scenery
Avoid: characters, passengers, enemies, text, labels, UI, watermark, gore and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×3 divisibility, transparent corners and stable opaque bounding-box center per column.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Interactables
git commit -m "[art] 응시 승강 수송 구조물 아틀라스 추가"
```

### Task 7: 문·숏컷 구조물 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_DoorsShortcuts_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_DoorsShortcuts_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G03, G05, G08, G10, G11, G12 룸 이미지와 게이트 상태
- Produces: 6개 문 구조의 닫힘·열림 초기·열림 후기·완전 개방 24셀

- [ ] **Step 1: 6×4 상태 원본 생성**

```text
Use case: stylized-concept
Asset type: 2D side-view animated door and shortcut atlas, exactly 6 columns by 4 rows
Columns: chain curtain A, elevator gate B, iris door C, low hush passage door, awareness mirror door, region-exit pupil door
Rows: closed, opening early, opening late, fully open
Invariants: exact same outer doorway frame, side view, scale and bottom-center pivot throughout each column; only chains, bars, iris blades, mirror surface or pupil aperture move
Style: Gaze region charcoal gothic iron and blue-black wet stone, desaturated violet mechanisms, cold teal only on awareness mirror outlines
Layout: exact 6x4 grid, centered isolated doors, 8 percent padding
Background: perfectly flat #00FF00, no scenery, wall, floor plane, shadow or reflection beyond the mirror surface contained inside its frame
Avoid: characters, enemies, readable writing, labels, UI, watermark, gore and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×4 divisibility, transparent corners and fixed outer-frame bounding boxes.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Interactables
git commit -m "[art] 응시 문과 숏컷 아틀라스 추가"
```

### Task 8: 환경 장식 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_EnvironmentProps_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Props/Gaze_EnvironmentProps_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: 전체 룸 콘셉트
- Produces: 6×4 장식·데칼 24셀

- [ ] **Step 1: 6×4 원본 생성**

```text
Use case: stylized-concept
Asset type: modular 2D side-view gothic environment prop atlas, exactly 6 columns by 4 rows
Subject: row 1 two empty cages, chain coil, pulley debris, iron-bar pile, stone rubble; row 2 two faceless audience statues, kneeling statue, downward-looking bust, hanging cloth, mask pile; row 3 two mirrors, closed-eye relief, iris window, violet lamp, blank surveillance record plaque; row 4 two floor crack decals, two wet stain decals, impossible reverse shadow decal, closed eyelid decal
Style: exact Gaze master city palette and hand-painted cosmic-horror gothic materials
Readability: props are darker and less saturated than playable platforms and active hazards; statues and reliefs do not glow like enemies
Layout: 24 isolated sprites in exact 6x4 equal cells, 8 percent padding; first three rows bottom aligned, decals centered
Background: perfectly flat #00FF00, no floor plane, cast shadow or scenery
Avoid: characters, living people, enemies, readable text, labels, UI, watermark, gore and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×4 divisibility, transparent corners and non-empty alpha in every cell.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Props
git commit -m "[art] 응시 환경 장식 아틀라스 추가"
```

### Task 9: 능력·보상 오브젝트 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_AbilityObjects_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_AbilityObjects_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: G01, G05, G11과 비밀방 3개
- Produces: 6개 오브젝트의 비활성·접근 예고·활성·사용 완료 24셀

- [ ] **Step 1: 6×4 상태 원본 생성**

```text
Use case: stylized-concept
Asset type: 2D side-view ability, checkpoint and reward object atlas, exactly 6 columns by 4 rows
Columns: checkpoint shrine, Hush ability shrine, Awareness awakening altar, memory-fragment pedestal without the fragment, hidden Awareness glyph, secret-room entrance glyph
Rows: inactive, proximity warning, active, consumed or completed
Invariants: exact same object, scale, side view, bottom-center pivot and outer silhouette in each column; only inner iris, violet light, cold-teal double outline and small shutters change
Style: Gaze gothic prison-cathedral stone and iron, restrained cosmic eye geometry, non-gory
Readability: violet indicates usable or active; cold teal only indicates Awareness-reactive states; completed states are visibly dim and open
Layout: exact 6x4 equal grid, isolated objects, 8 percent padding
Background: perfectly flat #00FF00 without floor, cast shadow, scenery or texture
Avoid: reward items floating above pedestals, characters, enemies, readable text, labels, UI, watermark, gore and green inside objects
```

- [ ] **Step 2: Save, remove chroma and verify**

Verify `RGBA`, 6×4 divisibility, transparent corners and stable pivot per column.

- [ ] **Step 3: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/Interactables
git commit -m "[art] 응시 능력 오브젝트 아틀라스 추가"
```

### Task 10: 환경 VFX 아틀라스

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/Gaze_AmbientVFX_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Gaze_AmbientVFX_v1.png`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: 전체 룸 팔레트
- Produces: 8×3 루프 애니메이션 24프레임

- [ ] **Step 1: 8×3 원본 생성**

```text
Use case: stylized-concept
Asset type: 2D metroidvania ambient VFX sprite sheet, exactly 8 columns by 3 rows
Rows are three seamless eight-frame loops: low flowing violet-cold-teal fog; floating surveillance dust with a very thin dim light; impossible reverse-shadow and eyelid afterimage
Invariants: each row stays centered and equal in scale, frame 8 flows naturally back to frame 1, no hard edge crosses a cell boundary
Style: subtle translucent Gaze-region cosmic-horror atmosphere, much dimmer than gameplay hazard vision cones
Layout: exact 8x3 equal grid, centered effects, 10 percent cell padding
Background: perfectly flat #00FF00, no floor, scenery, object, character, text or watermark
Avoid: opaque smoke masses, bright lasers, fire, gore and green effect colors
```

- [ ] **Step 2: Save and remove chroma**

Run the chroma helper with `--soft-matte`, `--despill` and `--edge-feather 0.25`. If a green fringe remains, rerun once with `--edge-contract 1`.

- [ ] **Step 3: Verify**

Verify `RGBA`, 8×3 divisibility, transparent corners and non-empty but non-opaque alpha coverage in every cell.

- [ ] **Step 4: Commit**

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment/VFX
git commit -m "[art] 응시 환경 VFX 아틀라스 추가"
```

### Task 11: 전체 연락판과 최종 검증

**Files:**
- Create: `docs/concept-art/generated/gaze-environment-assets/gaze-environment-assets-contact-sheet.jpg`
- Modify: `docs/concept-art/generated/gaze-environment-assets/PROMPTS.md`

**Interfaces:**
- Consumes: 최종 RGBA PNG 9개
- Produces: 시각 검토용 연락판과 검증 결과

- [ ] **Step 1: 파일 수와 알파 검증**

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image
root=Path('HiddenWeight/Assets/Art/Gaze/Environment')
files=sorted(root.rglob('Gaze_*_v1.png'))
assert len(files)==9, len(files)
grids={
'Gaze_TerrainTiles_v1.png':(6,4),'Gaze_Platforms_v1.png':(6,3),
'Gaze_EyeHazards_v1.png':(6,4),'Gaze_CoverObjects_v1.png':(6,3),
'Gaze_TransitStructures_v1.png':(6,3),'Gaze_DoorsShortcuts_v1.png':(6,4),
'Gaze_EnvironmentProps_v1.png':(6,4),'Gaze_AbilityObjects_v1.png':(6,4),
'Gaze_AmbientVFX_v1.png':(8,3)}
for p in files:
    im=Image.open(p)
    assert im.mode=='RGBA', (p,im.mode)
    c,r=grids[p.name]
    assert im.width%c==0 and im.height%r==0, (p,im.size)
    assert all(im.getpixel(x)[3]==0 for x in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)])
    print('PASS',p,im.size,c,r)
PY
```

Expected: 9개 모두 `PASS`.

- [ ] **Step 2: 셀 비어 있음과 크로마 잔류 검사**

각 아틀라스를 셀로 순회해 알파가 있는 픽셀을 확인한다. 불투명 픽셀에서 `G > R*1.35`,
`G > B*1.35`, `G > 150`인 픽셀 비율이 0.5%를 넘으면 실패로 처리하고 해당 아틀라스의
크로마 제거를 `--edge-contract 1`로 다시 수행한다.

- [ ] **Step 3: 연락판 생성**

Pillow로 9개 적용본을 체크무늬 배경 위에 축소 배치하고 파일명·격자 크기를 하단에 표시한다.
연락판은 3열 × 3행 JPG로 저장한다.

- [ ] **Step 4: 룸 기준 육안 검토**

G02, G05, G07, G08, G11, G12 룸과 연락판을 비교한다.

- 발판 상단선이 배경보다 읽힌다.
- 환경 장식은 위험 장치보다 어둡다.
- 시선 장치는 휴면·예고·활성·회복이 구분된다.
- 청록색은 자각 반응과 일부 안전 가장자리에만 제한된다.
- 잔재 지역의 갈색·앰버·뼈 모티프가 섞이지 않는다.

- [ ] **Step 5: 최종 기록과 커밋**

`PROMPTS.md`에 각 생성 원본과 적용본, 실제 생성 프롬프트, 검증 결과를 기록한다.

```bash
git add docs/concept-art/generated/gaze-environment-assets HiddenWeight/Assets/Art/Gaze/Environment
git commit -m "[art] 응시 전체 환경 아틀라스 제작 완료"
```

