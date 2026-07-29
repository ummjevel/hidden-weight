# Residue Animation Sprites Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 잔재 지역의 일반 적, 보스, 위험물, 장치, 아이템, 환경 및 VFX에 자연스러운 움직임을 제공하는 16종의 확장 PNG 스프라이트 시트를 제작한다.

**Architecture:** 기존 잔재 시트를 각 대상의 정체성과 재질 참조로 사용하고, 신규 시트는 단색 크로마키 배경으로 생성한 뒤 로컬에서 RGBA PNG로 변환한다. 기존 파일은 보존하며 신규 파일은 `_v2` 또는 신규 역할 이름으로 추가하고, 모든 시트는 정해진 등분 격자와 피벗 규칙을 따른다.

**Tech Stack:** Built-in ImageGen, PNG/RGBA, ImageMagick, Pillow, Unity Sprite Editor

## Global Constraints

- 범위는 1맵 잔재 지역이며 플레이어 스프라이트는 제외한다.
- 기존 잔재 지역의 검은 철골, 회갈색 석재, 앰버 발광, 낮은 남보라 하이라이트를 유지한다.
- 기존 파일을 덮어쓰지 않는다.
- 최종 파일은 투명 RGBA PNG다.
- 적과 보스는 Bottom Center, 중심 확산 VFX는 Center 피벗을 사용한다.
- 텍스트, 번호, 셀 선, 워터마크를 넣지 않는다.
- 공격은 예고, 유효, 회수가 실루엣으로 구분되어야 한다.

---

### Task 1: 일반 적 4종 확장

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/ResidueWalker_v2.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/HangingFinger_v2.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/MourningCarrier_v2.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/HardenedResidue_v2.png`
- Reference: 같은 폴더의 대응 `_v1.png`

**Interfaces:**
- Consumes: 기존 적의 실루엣, 재질, 앰버 발광 위치
- Produces: 각 `8열 × 6행`, Idle / Move / Telegraph / Attack / Hit / Death

- [ ] **Step 1: 각 기존 시트를 시각 참조로 불러온다**

각 `_v1.png`는 동일 대상의 정체성 참조이며 편집 대상은 아니다.

- [ ] **Step 2: 적마다 별도 ImageGen 호출로 8×6 시트를 생성한다**

공통 프롬프트:

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game animation sprite sheet
Primary request: preserve the referenced Residue enemy exactly and create a coherent 8-column by 6-row animation sheet
Rows: idle loop, locomotion loop, attack telegraph, attack action and recovery, hit reaction, irreversible death
Style: dark hand-painted gothic cosmic horror, black iron scaffolding, ash-gray bone and stone, restrained amber glow, tiny indigo memory highlights
Composition: every frame centered inside an equal invisible cell; bottom-center ground contact locked; generous spacing; no overlap
Scene: perfectly flat solid #00ff00 chroma-key background
Constraints: same creature anatomy, scale, materials, glow placement and viewing angle in all 48 frames; readable silhouettes; telegraph precedes impact by at least two frames
Avoid: text, numbers, grid lines, borders, watermark, cast shadow, floor plane, camera motion, background texture, green inside the creature
```

- [ ] **Step 3: 크로마키를 제거하고 최종 경로에 저장한다**

선택된 생성 원본을 다음 이름으로 `tmp/imagegen/`에 복사한 뒤 실행한다:
`ResidueWalker-source.png`, `HangingFinger-source.png`,
`MourningCarrier-source.png`, `HardenedResidue-source.png`.

```bash
mkdir -p tmp/imagegen
for asset in ResidueWalker HangingFinger MourningCarrier HardenedResidue; do
  python "${CODEX_HOME:-$HOME/.codex}/skills/.system/imagegen/scripts/remove_chroma_key.py" \
    --input "tmp/imagegen/${asset}-source.png" \
    --out "HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/${asset}_v2.png" \
    --auto-key border --soft-matte \
    --transparent-threshold 12 --opaque-threshold 220 --despill
done
```

- [ ] **Step 4: 4개 시트의 알파와 격자를 검사한다**

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
root = Path("HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation")
for p in sorted(root.glob("*_v2.png")):
    im = Image.open(p)
    assert im.mode == "RGBA", (p, im.mode)
    assert im.width % 8 == 0 and im.height % 6 == 0, (p, im.size)
    assert all(im.getpixel(xy)[3] == 0 for xy in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]), p
    print(p, im.size, "cell", im.width//8, im.height//6)
PY
```

- [ ] **Step 5: 적 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/*_v2.png
git commit -m "art: add smooth residue enemy sprite sheets"
```

### Task 2: 손목 감시자 확장

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/WristWatcher_Combat_v2.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/WristWatcher_Transitions_v1.png`
- Reference: `WristWatcher_Combat_v1.png`, `WristWatcher_Reactions_v1.png`, `WristWatcher_Poses_v1.png`

**Interfaces:**
- Consumes: 기존 손목 감시자의 장검형 팔, 감시탑 재질, 앰버 핵
- Produces: 8×7 전투 시트와 8×3 등장·전환 시트

- [ ] **Step 1: 기존 보스 시트 세 장을 시각 참조로 불러온다**

- [ ] **Step 2: `WristWatcher_Combat_v2.png`를 생성한다**

```text
Create a coherent 8-column by 7-row 2D boss animation sprite sheet of the exact referenced Wrist Watcher.
Rows: idle, wide blade sweep, forward charge, wall-impact stun, vertical drop attack, hurt reaction, irreversible death collapse.
Keep bottom-center contact locked and preserve its black iron wrist-tower body, long blade limbs and restrained amber core.
Each attack row must clearly show anticipation, active impact and recovery.
Flat solid #00ff00 chroma-key background, equal cells, no text, no grid, no shadows, no scene.
```

- [ ] **Step 3: `WristWatcher_Transitions_v1.png`를 생성한다**

```text
Create a coherent 8-column by 3-row transition sprite sheet of the exact referenced Wrist Watcher.
Rows: ominous entrance unfolding from a tower-like silhouette, phase transition with amber core overload, interaction with an arena mechanism.
Preserve anatomy, scale and material across all frames. Flat solid #00ff00 chroma-key background, equal cells, no text, no grid, no shadows.
```

- [ ] **Step 4: 두 시트를 투명화하고 8열 격자를 검사한다**

`remove_chroma_key.py`를 각각 실행하고 높이가 7행 또는 3행으로 등분되는지 검사한다.

- [ ] **Step 5: 보스 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/WristWatcher_*.png
git commit -m "art: expand wrist watcher animation sprites"
```

### Task 3: 기억의 교수자 동작 세트

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/MemoryInstructor_Attacks_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/MemoryInstructor_Reactions_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/MemoryInstructor_CoreHalo_v1.png`
- Reference: `MemoryInstructor_Parts_v1.png`, `MemoryInstructorVFX_v1.png`

**Interfaces:**
- Consumes: 기존 분리형 몸통, 칼날 팔, 갈고리 팔, 사슬, 후광, 핵
- Produces: 8×4 공격, 8×3 반응, 8×3 핵·후광 시트

- [ ] **Step 1: 공격 시트를 생성한다**

```text
Create an 8-column by 4-row side-view animation sprite sheet for the exact referenced Memory Instructor assembled boss.
Rows: blade-arm sweep, hook-arm pull, chain slam, recovery to neutral.
Keep the torso anchored while arms and chains move around their visible sockets. Telegraph must precede every strike. Flat #00ff00 background, equal cells, no text or grid.
```

- [ ] **Step 2: 반응 시트를 생성한다**

```text
Create an 8-column by 3-row sprite sheet for the exact referenced Memory Instructor.
Rows: short hurt recoil, violent indigo-and-amber phase rupture, irreversible death disassembly.
Preserve the boss body and socket layout until the final death frames. Flat #00ff00 background, equal cells, no text or grid.
```

- [ ] **Step 3: 핵·후광 시트를 생성한다**

```text
Create an 8-column by 3-row centered effect-and-part sprite sheet matching the referenced Memory Instructor.
Rows: seamless gallows halo rotation, seamless caged-core pulse, one-shot core overload.
Perfectly centered, constant scale, restrained amber and indigo light, flat #00ff00 background, no text or grid.
```

- [ ] **Step 4: 투명화, 격자 및 중심점 일관성을 검사한다**

- [ ] **Step 5: 교수자 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/MemoryInstructor_*.png
git commit -m "art: add memory instructor animation sprites"
```

### Task 4: 위험물과 되감기 장치

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Hazards/Animation/HazardTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/RewindObjectTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/BossArenaTransitions_v1.png`
- Reference: 기존 Hazards, RewindPlatforms, RewindMechanisms, BossArenaDevices 시트

**Interfaces:**
- Produces: 8×5 위험물, 8×4 되감기 장치, 8×3 보스 전장 전환

- [ ] **Step 1: 위험물 전환 시트를 생성한다**

```text
Create an 8-column by 5-row 2D hazard transition sprite sheet matching the referenced Residue environment.
Rows: floor spikes, abyss tendril, iron crusher, collapsing floor, falling debris.
Every row reads as idle, warning, activation, active hold, recovery. Keep the physical anchor fixed. Flat #00ff00 background, no text, grid or shadows.
```

- [ ] **Step 2: 되감기 장치 전환 시트를 생성한다**

```text
Create an 8-column by 4-row 2D interactable sprite sheet matching the referenced Residue structures.
Rows: broken platform restoration, chain bridge restoration, lift restoration, pulley restoration.
Show fractured debris reversing into place with restrained indigo temporal traces and an amber completion pulse. Flat #00ff00 background, no text or grid.
```

- [ ] **Step 3: 보스 전장 전환 시트를 생성한다**

```text
Create an 8-column by 3-row 2D arena-device sprite sheet matching the Residue boss arena.
Rows: arena lock closing, safety platform restoration, final seal rupture.
Dark iron and ash stone, restrained amber mechanics and indigo rewind traces. Flat #00ff00 background, equal cells, no text or grid.
```

- [ ] **Step 4: 투명화와 바닥 기준점 검사를 수행한다**

- [ ] **Step 5: 위험물·장치 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Environment/Hazards/Animation/HazardTransitions_v1.png
git add HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/RewindObjectTransitions_v1.png
git add HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/BossArenaTransitions_v1.png
git commit -m "art: add residue hazard and mechanism transitions"
```

### Task 5: 아이템과 체크포인트

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Items/Animation/CollectibleTransitions_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/Items/Animation/CheckpointTransitions_v1.png`
- Reference: `CollectibleIdle_Animation_v1.png`, `CheckpointShrine_Animation_v1.png`

**Interfaces:**
- Produces: 8×5 획득 시트, 8×3 체크포인트 시트

- [ ] **Step 1: 수집물 전환 시트를 생성한다**

```text
Create an 8-column by 5-row centered pickup transition sprite sheet matching the referenced Residue collectibles.
Rows: currency, healing reliquary, maximum-health shard, memory fragment, rewind core.
Each row rises slightly, compresses into light, streams toward the player and disappears completely by frame eight. Flat #00ff00 background, no text, grid or shadow.
```

- [ ] **Step 2: 체크포인트 전환 시트를 생성한다**

```text
Create an 8-column by 3-row centered shrine transition sprite sheet matching the referenced Residue checkpoint.
Rows: first activation, healing pulse, respawn release.
Keep the shrine base fixed; animate only mechanisms, amber light and restrained indigo memory energy. Flat #00ff00 background, no text or grid.
```

- [ ] **Step 3: 투명화하고 마지막 획득 프레임의 피사체 소멸을 검사한다**

- [ ] **Step 4: 아이템 시트를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Gameplay/Items/Animation/*Transitions_v1.png
git commit -m "art: add residue item and checkpoint transitions"
```

### Task 6: 환경 루프와 보조 VFX

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Props/Animation/AmbientMotion_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/SecondaryGameplayVFX_v1.png`
- Create: `docs/concept-art/generated/residue-animation-sprites/contact-sheets/residue-animation-sprites.jpg`
- Create: `docs/concept-art/generated/residue-animation-sprites/PROMPTS.md`

**Interfaces:**
- Produces: 8×4 환경 루프, 8×4 보조 VFX, 전체 검수용 연락 시트와 프롬프트 기록

- [ ] **Step 1: 환경 루프 시트를 생성한다**

```text
Create an 8-column by 4-row seamless ambient animation sprite sheet matching the Residue environment.
Rows: hanging chain sway, torn funeral cloth flutter, falling ash, low indigo fog drift.
First and last frames must loop naturally. Keep effects subtle enough not to obscure gameplay. Flat #00ff00 background, no text or grid.
```

- [ ] **Step 2: 보조 VFX 시트를 생성한다**

```text
Create an 8-column by 4-row centered gameplay VFX sprite sheet matching the Residue palette.
Rows: heavy hit burst, guard break, enemy amber-ash dissolve, boss indigo-and-amber phase burst.
Each row starts empty, expands to a readable peak, then dissipates to empty. Flat #00ff00 background, no text or grid.
```

- [ ] **Step 3: 투명화하고 모든 최종 PNG를 연락 시트로 합친다**

```bash
mkdir -p docs/concept-art/generated/residue-animation-sprites/contact-sheets
montage \
  HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/*_v2.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/*Transitions_v1.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/MemoryInstructor_*.png \
  HiddenWeight/Assets/Art/Residue/Environment/Hazards/Animation/HazardTransitions_v1.png \
  HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/RewindObjectTransitions_v1.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/Items/Animation/*Transitions_v1.png \
  HiddenWeight/Assets/Art/Residue/Environment/Props/Animation/AmbientMotion_v1.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/VFX/SecondaryGameplayVFX_v1.png \
  -thumbnail 480x480 -tile 4x -geometry +12+12 -background '#17151c' \
  docs/concept-art/generated/residue-animation-sprites/contact-sheets/residue-animation-sprites.jpg
```

- [ ] **Step 4: 프롬프트와 파일별 격자를 `PROMPTS.md`에 기록한다**

16개 파일명, 참조 이미지, 최종 프롬프트, 격자, 권장 FPS를 기록한다.

- [ ] **Step 5: 전체 파일의 RGBA·투명 모서리·격자를 자동 검사한다**

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
targets = list(Path("HiddenWeight/Assets/Art/Residue").rglob("*Transitions_v1.png"))
targets += list(Path("HiddenWeight/Assets/Art/Residue").rglob("*_v2.png"))
targets += list(Path("HiddenWeight/Assets/Art/Residue").rglob("MemoryInstructor_*_v1.png"))
targets += list(Path("HiddenWeight/Assets/Art/Residue").rglob("AmbientMotion_v1.png"))
targets += list(Path("HiddenWeight/Assets/Art/Residue").rglob("SecondaryGameplayVFX_v1.png"))
for p in sorted(set(targets)):
    im = Image.open(p)
    assert im.mode == "RGBA", (p, im.mode)
    assert im.width % 8 == 0, (p, im.size)
    corners = [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]
    assert all(im.getpixel(xy)[3] == 0 for xy in corners), p
    print("PASS", p, im.size)
PY
```

- [ ] **Step 6: 전체 결과를 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Residue/Environment/Props/Animation/AmbientMotion_v1.png
git add HiddenWeight/Assets/Art/Residue/Gameplay/VFX/SecondaryGameplayVFX_v1.png
git add docs/concept-art/generated/residue-animation-sprites
git commit -m "art: complete residue animation sprite extension"
```
