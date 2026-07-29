# Residue Completion Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 잔재 지역의 이미지 제작 범위를 100%로 마감하는 신규 투명 PNG 9종을 제작한다.

**Architecture:** 기존 잔재 환경·적·보스·VFX 시트를 시각 참조로 사용하고, 역할별 신규 시트를 `#00ff00` 배경에서 별도 생성한다. 크로마키 제거 후 모든 애니메이션을 8열 격자로 정규화하고, RGBA·격자·투명 모서리 검사를 통과한 파일만 프로젝트에 저장한다.

**Tech Stack:** Built-in ImageGen, PNG/RGBA, ImageMagick, Pillow, Unity

## Global Constraints

- 기존 잔재 PNG는 덮어쓰지 않는다.
- 회갈색 석재, 먹색 철골, 탁한 앰버, 제한적인 남색 그림자를 유지한다.
- 공격 예고는 앰버, 활성 위험은 황백색, 되감기 상태는 옅은 남색·청백색으로 구분한다.
- 전경·배경 효과는 플레이어와 공격 판정보다 낮은 명도를 사용한다.
- 애니메이션 시트는 가로 8프레임이다.
- 결과는 투명 RGBA PNG이며 텍스트, 숫자, 워터마크, 셀 선, 배경 장면, 캐릭터 본체를 포함하지 않는다.

---

### Task 1: 적·보스 공격체

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/ResidueEnemyProjectiles_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/ResidueBossProjectiles_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/CombatVFX_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/MemoryInstructorVFX_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Residue_Enemies_Atlas_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/WristWatcher_Poses_v1.png`

**Interfaces:**
- Produces: 적 공격체 8×4, 보스 공격체 8×5

- [ ] **Step 1: 적 공격체 시트를 생성한다**

```text
Create an exactly 8-column by 4-row production gameplay projectile sprite sheet for the Residue region.
Rows: walker stone splinter; hanging finger claw trail; mourning carrier amber charge trail;
hardened residue compressed ground shockwave.
Each row progresses through anticipation, active travel or extension, impact and clean dissipation.
Match brown-gray stone, black rusted iron, muted amber memory light and faint navy shadow.
Flat #00ff00 chroma-key background. No character body, scenery, text, grid or UI.
```

- [ ] **Step 2: 보스 공격체 시트를 생성한다**

```text
Create an exactly 8-column by 5-row production gameplay projectile sprite sheet for Residue bosses.
Rows: Wrist Watcher horizontal surveillance wave; Wrist Watcher falling impact ring;
Memory Instructor memory needle; Memory Instructor rewind orb; boss phase-transition rupture.
Each row has readable warning, active, impact and dissipation frames.
Match brown-gray stone, black cage metal, muted amber and pale blue rewind light.
Flat #00ff00 chroma-key background. No boss body, scenery, text, grid or UI.
```

- [ ] **Step 3: 투명화·8열 정규화·검사 후 커밋한다**

```bash
python /Users/ksh/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py \
  --input source.png --out final.png --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill
git add HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation
git commit -m "art: add residue projectile animation sprites"
```

### Task 2: 발판·충돌 효과

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/Animation/ResiduePlatformStates_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/ResidueImpactVFX_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/Residue_Platforms_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Environment/Hazards/Animation/CollapseHazards_Animation_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/CombatVFX_v1.png`

**Interfaces:**
- Produces: 발판 상태 8×4, 충돌 효과 8×4

- [ ] **Step 1: 발판 상태 시트를 생성한다**

```text
Create an exactly 8-column by 4-row platform-state sprite sheet for the Residue region.
Use one consistent side-view rusted iron and brown-gray stone platform.
Rows: subtle shaking and cracking; progressive collapse with falling fragments;
fully broken settling state; pale blue rewind reconstruction in exact reverse visual order.
Fixed bottom-center anchor. Flat #00ff00 background. No scenery, text, grid or character.
```

- [ ] **Step 2: 충돌 효과 시트를 생성한다**

```text
Create an exactly 8-column by 4-row centered impact VFX sheet for the Residue region.
Rows: compact melee hit; stone wall collision; light landing dust; heavy landing or large-object impact.
Use charcoal dust, brown-gray fragments, muted amber sparks and very restrained pale blue residue.
Each effect starts compact, peaks near the middle and fully dissipates.
Flat #00ff00 background. No scenery, text, grid or character body.
```

- [ ] **Step 3: 투명화·8열 정규화·검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Environment/Terrain/Animation
git add HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/ResidueImpactVFX_v1.png
git commit -m "art: add residue platform and impact animation sprites"
```

### Task 3: 전경·배경 모션

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/ResidueForegroundMotion_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/ResidueBackgroundMotion_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Environment/Props/Animation/AmbientMotion_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/AmbientBackgroundTransitions_Animation_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Room08/Room08_BG_Mid.png`

**Interfaces:**
- Produces: 전경 8×4, 배경 8×4 seamless loop

- [ ] **Step 1: 전경 모션 시트를 생성한다**

```text
Create an exactly 8-column by 4-row seamless foreground motion sheet for the Residue region.
Rows: close hanging chains swaying; black cage and torn shroud swaying;
enormous finger silhouette slowly passing along a screen edge; low foreground dust and fragments.
Keep low contrast and dark values so gameplay remains readable.
Flat #00ff00 background. No complete room, text, grid or character.
```

- [ ] **Step 2: 배경 모션 시트를 생성한다**

```text
Create an exactly 8-column by 4-row seamless distant-background motion sheet for the Residue region.
Rows: distant smoke and ash; ruined windows blinking irregular muted amber;
far colossal hand changing position almost imperceptibly; clusters of petrified human silhouettes
making a synchronized uncanny micro-movement.
Keep soft edges, low contrast and distant scale.
Flat #00ff00 background. No complete room, text, grid or foreground character.
```

- [ ] **Step 3: 투명화·루프 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/Residue*Motion_v1.png
git commit -m "art: add residue foreground and background motion sprites"
```

### Task 4: 방 전환

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/ResidueRoomTransitions_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/Props/Animation/SecretEntrances_Animation_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/Props/Animation/ShortcutOperations_Animation_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Residue_RewindStructures_v1.png`

**Interfaces:**
- Produces: 방 전환 8×4

- [ ] **Step 1: 방 전환 시트를 생성한다**

```text
Create an exactly 8-column by 4-row side-view room-transition mechanism sheet for the Residue region.
Rows: rusted chain-and-stone room seal closing; the same seal opening;
broken shortcut mechanism rewinding into an open route; secret wall formed from rigid fingers
separating into a narrow passage.
Fixed bottom-center anchor, readable locked and open endpoints.
Flat #00ff00 background. No room scene, text, grid or character.
```

- [ ] **Step 2: 투명화·8열 정규화·검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/ResidueRoomTransitions_v1.png
git commit -m "art: add residue room transition sprites"
```

### Task 5: 지역 전용 UI

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/UI/ResidueUIIcons_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/UI/Animation/ResidueStatusUI_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Gameplay/Items/Residue_ItemsHazards_Atlas_v1.png`
- Reference: `HiddenWeight/Assets/Art/Residue/Residue_InteractablesAtlas.png`

**Interfaces:**
- Produces: 정적 아이콘 8×4, 상태 UI 8×3

- [ ] **Step 1: 32개 지도·진행 아이콘 아틀라스를 생성한다**

```text
Create an exactly 8-column by 4-row atlas of 32 distinct Residue-region UI icons.
Row 1: entrance, exit, upward path, downward path, normal door, locked door, shortcut, secret passage.
Row 2: checkpoint, healing, currency, memory fragment, health shard, rewind object, hazard, collapse platform.
Row 3: walker, hanging finger, mourning carrier, hardened residue, miniboss, region boss, NPC, lore record.
Row 4: undiscovered, discovered, completed, revisit, current position, objective, boss defeated, region complete.
Use simple silhouettes, consistent circular bounds, brown-gray metal, muted amber and pale blue accents.
Flat #00ff00 background. No text, letters, numbers, grid lines or scenery.
```

- [ ] **Step 2: 상태 UI 애니메이션 시트를 생성한다**

```text
Create an exactly 8-column by 3-row centered animated status UI sheet for the Residue region.
Row 1: rewind energy charging, ready, activating and emptying.
Row 2: damage warning accumulating, critical danger, then clearing.
Row 3: memory acquired, boss warning and region-complete seal response.
Use compact readable emblem shapes, muted amber danger and pale blue rewind light.
Flat #00ff00 background. No text, letters, numbers, grid lines or scenery.
```

- [ ] **Step 3: 투명화·축소 가독성 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/UI
git commit -m "art: add residue map and status UI assets"
```

### Task 6: 기록·컨택트시트·전체 검수

**Files:**
- Create: `docs/concept-art/generated/residue-completion-assets/PROMPTS.md`
- Create: `docs/concept-art/generated/residue-completion-assets/contact-sheets/residue-completion-assets.jpg`

**Interfaces:**
- Produces: 9종 프롬프트·격자·셀 크기·권장 FPS 기록과 전체 미리보기

- [ ] **Step 1: 9개 파일의 생성 프롬프트와 규격을 기록한다**

- [ ] **Step 2: 9개 결과를 이름표가 있는 컨택트시트로 만든다**

```bash
magick montage <nine files> -thumbnail 460x360 -tile 3x -geometry +12+28 \
  -background '#171513' -fill '#e8dfcf' -pointsize 15 -set label '%t' \
  docs/concept-art/generated/residue-completion-assets/contact-sheets/residue-completion-assets.jpg
```

- [ ] **Step 3: 9개 파일의 RGBA·격자·투명 모서리를 검사한다**

```python
from PIL import Image
from pathlib import Path

expected = {
    "ResidueEnemyProjectiles_v1.png": (8, 4),
    "ResidueBossProjectiles_v1.png": (8, 5),
    "ResiduePlatformStates_v1.png": (8, 4),
    "ResidueImpactVFX_v1.png": (8, 4),
    "ResidueForegroundMotion_v1.png": (8, 4),
    "ResidueBackgroundMotion_v1.png": (8, 4),
    "ResidueRoomTransitions_v1.png": (8, 4),
    "ResidueUIIcons_v1.png": (8, 4),
    "ResidueStatusUI_v1.png": (8, 3),
}

found = {p.name: p for p in Path("HiddenWeight/Assets/Art/Residue").rglob("*.png")
         if p.name in expected}
assert set(found) == set(expected)
for name, (cols, rows) in expected.items():
    im = Image.open(found[name])
    assert im.mode == "RGBA"
    assert im.width % cols == 0 and im.height % rows == 0
    alpha = im.getchannel("A")
    corners = ((0, 0), (im.width - 1, 0),
               (0, im.height - 1), (im.width - 1, im.height - 1))
    assert all(alpha.getpixel(point) == 0 for point in corners)
```

- [ ] **Step 4: 전체 결과를 커밋한다**

```bash
git add docs/concept-art/generated/residue-completion-assets
git commit -m "art: complete residue image production set"
```
