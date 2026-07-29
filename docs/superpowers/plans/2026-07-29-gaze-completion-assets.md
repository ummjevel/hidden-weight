# Gaze Completion Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 응시 지역의 이미지 제작 범위를 100%로 마감하는 신규 투명 PNG 9종을 제작한다.

**Architecture:** 기존 응시의 환경·적·보스·VFX를 시각 참조로 사용해 역할별 시트를 별도 생성한다. `#00ff00` 배경을 제거한 뒤 모든 셀을 192×192, 가로 8프레임으로 정규화하고 RGBA·격자·투명 모서리를 검사한다.

**Tech Stack:** Built-in ImageGen, PNG/RGBA, ImageMagick, Pillow, Unity

## Global Constraints

- 기존 응시 PNG는 덮어쓰지 않는다.
- 저채도 보라, 먹색, 검은 철, 제한적인 청록을 유지한다.
- 기만·위험은 마젠타, 진실·해금은 청록으로 구분한다.
- 전경·배경 효과는 플레이어와 공격 판정보다 낮은 명도를 사용한다.
- 애니메이션 시트는 가로 8프레임, 셀 크기 192×192다.
- 결과는 투명 RGBA PNG이며 텍스트, 숫자, 워터마크, 셀 선, 완성 배경 장면, 캐릭터 본체를 포함하지 않는다.

---

### Task 1: 적·보스 공격체

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/GazeEnemyProjectiles_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/GazeBossProjectiles_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/GazeSecondaryVFX_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/*.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/*.png`

**Interfaces:**
- Produces: 적 공격체 8×4, 보스 공격체 8×5

- [ ] **Step 1: 적 공격체 시트를 생성한다**

```text
Create exactly 8 columns by 4 rows.
Rows: Blind Pilgrim sound ripple; Informing Mouth scream bolt;
Hanging Audience gaze-shadow marker; Faceless Judge verdict slash.
Show warning, active, impact and dissipation. Violet-black, magenta danger, tiny teal truth accents.
Flat #00ff00 background. No character body, scenery, text or grid.
```

- [ ] **Step 2: 보스 공격체 시트를 생성한다**

```text
Create exactly 8 columns by 5 rows.
Rows: Iris Gatekeeper scan beam; eyelid shard rain; cage-chain lash;
Gaze of All false-eye projectile; true teal revelation strike.
Show warning, active, impact and dissipation.
Flat #00ff00 background. No boss body, scenery, text or grid.
```

- [ ] **Step 3: 투명화·격자 검사 후 커밋한다**

```bash
python /Users/ksh/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py \
  --input source.png --out final.png --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill
git add HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation
git commit -m "art: add gaze projectile animation sprites"
```

### Task 2: 발판·충돌 효과

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Animation/GazePlatformStates_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/GazeImpactVFX_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Gaze_Platforms_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Animation/EyeHazardTransitions_v1.png`

**Interfaces:**
- Produces: 발판 8×4, 충돌 VFX 8×4

- [ ] **Step 1: 발판 상태 시트를 생성한다**

```text
Create exactly 8 columns by 4 rows using one consistent eye-carved violet stone platform.
Rows: stable platform reacting to observation; watched platform disassembling;
fully dissolved empty state; cyan true-sight reconstruction.
Use a fixed bottom-center anchor. Flat #00ff00 background. No scenery, text or grid.
```

- [ ] **Step 2: 충돌 효과 시트를 생성한다**

```text
Create exactly 8 columns by 4 rows.
Rows: basic violet hit; gaze-beam scorch; light landing dust;
mask-and-eyelid guard break.
Each effect peaks near the middle and disappears by frame eight.
Flat #00ff00 background. No character, scenery, text or grid.
```

- [ ] **Step 3: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Animation
git add HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/GazeImpactVFX_v1.png
git commit -m "art: add gaze platform and impact animation sprites"
```

### Task 3: 전경·배경 모션

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/GazeForegroundMotion_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/GazeBackgroundMotion_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/GazeAmbientMotion_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Room12/Room12_BG_Mid.png`

**Interfaces:**
- Produces: 전경·배경 seamless loop 각 8×4

- [ ] **Step 1: 전경 시트를 생성한다**

```text
Create exactly 8 columns by 4 seamless rows.
Rows: close chain and cage sway; torn theater curtain;
audience mask entering the screen edge; low violet mist with tiny blinking eyes.
Keep dark and low contrast. Flat #00ff00 background. No full scene, text or grid.
```

- [ ] **Step 2: 배경 시트를 생성한다**

```text
Create exactly 8 columns by 4 seamless rows.
Rows: colossal sky iris rotating; distant window-eyes blinking;
hanging cages swaying; audience silhouettes turning their heads together.
Keep soft distant edges and low contrast. Flat #00ff00 background. No full scene, text or grid.
```

- [ ] **Step 3: 투명화·루프 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/Gaze*Motion_v1.png
git commit -m "art: add gaze foreground and background motion sprites"
```

### Task 4: 방 전환

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/GazeRoomTransitions_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_DoorsShortcuts_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_TransitStructures_v1.png`

**Interfaces:**
- Produces: 방 전환 8×4

- [ ] **Step 1: 방 전환 시트를 생성한다**

```text
Create exactly 8 columns by 4 rows.
Rows: iris door sealing; iris door opening; cage-and-bridge shortcut opening;
mirror-and-curtain secret passage revealing.
Use fixed bottom-center anchors and readable endpoints.
Flat #00ff00 background. No room scene, text or grid.
```

- [ ] **Step 2: 투명화·격자 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/GazeRoomTransitions_v1.png
git commit -m "art: add gaze room transition sprites"
```

### Task 5: 지도·상태 UI

**Files:**
- Create: `HiddenWeight/Assets/Art/Gaze/UI/GazeUIIcons_v1.png`
- Create: `HiddenWeight/Assets/Art/Gaze/UI/Animation/GazeStatusUI_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Gaze_AbilityObjects_v1.png`
- Reference: `HiddenWeight/Assets/Art/Gaze/Gameplay/Items/Animation/GazeCollectibleTransitions_v1.png`

**Interfaces:**
- Produces: 정적 아이콘 8×4, 상태 UI 8×3

- [ ] **Step 1: 아이콘 아틀라스를 생성한다**

```text
Create exactly 8 columns by 4 rows of 32 distinct icons.
Row 1: entrance, exit, up, down, door, locked door, shortcut, secret passage.
Row 2: checkpoint, healing, currency, memory, awareness, true sight, gaze hazard, cover.
Row 3: four Gaze enemies, miniboss, region boss, NPC, lore record.
Row 4: undiscovered, discovered, completed, revisit, current position, objective, boss defeated, region complete.
No text, letters or numbers. Flat #00ff00 background.
```

- [ ] **Step 2: 상태 UI를 생성한다**

```text
Create exactly 8 columns by 3 rows.
Rows: awareness and true-sight charge/use; detection and gaze buildup/clear;
memory acquired, boss warning and region complete.
Use magenta danger and cyan truth signals. No text or numbers. Flat #00ff00 background.
```

- [ ] **Step 3: 투명화·축소 검사 후 커밋한다**

```bash
git add HiddenWeight/Assets/Art/Gaze/UI
git commit -m "art: add gaze map and status UI assets"
```

### Task 6: 문서·컨택트시트·전체 검수

**Files:**
- Create: `docs/concept-art/generated/gaze-completion-assets/PROMPTS.md`
- Create: `docs/concept-art/generated/gaze-completion-assets/contact-sheets/gaze-completion-assets.jpg`

**Interfaces:**
- Produces: 9종의 프롬프트·격자·권장 FPS 기록과 전체 미리보기

- [ ] **Step 1: 9개 파일의 프롬프트·격자·FPS를 기록한다**
- [ ] **Step 2: 9개 결과를 이름표가 있는 3열 컨택트시트로 만든다**
- [ ] **Step 3: 9개 파일의 RGBA·격자·투명 모서리를 Pillow로 검사한다**
- [ ] **Step 4: Unity EditMode·PlayMode 테스트를 실행한다**
- [ ] **Step 5: 문서와 Unity 메타데이터를 커밋한다**
