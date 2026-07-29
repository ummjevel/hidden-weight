# Chibi Player Art Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `docs/chibi.png`의 캐릭터를 기준으로 기존 플레이어 애니메이션·VFX를 전부 교체하고 숨죽이기·자각 신규 시트를 추가한다.

**Architecture:** 새 캐릭터의 정체성은 먼저 4×2 핵심 포즈 시트로 고정한다. 이후 각 동작 시트는 `chibi.png`, 승인된 핵심 포즈, 기존 동작 시트를 함께 참조해 포즈 흐름은 유지하고 외형만 교체한다. 생성 원본은 문서 폴더에 보존하고, 균일 크로마키를 제거한 RGBA PNG만 Unity 런타임 경로에 저장한다.

**Tech Stack:** built-in `image_gen`, Pillow, `remove_chroma_key.py`, PNG/RGBA, Unity Sprite Multiple, Unity EditMode/PlayMode tests

## Global Constraints

- 기준 이미지는 `docs/chibi.png`이며 흰 단발머리, 청록색 눈, 크림색 터틀넥 원피스, 짙은 회색 레깅스, 흰 부츠, 청록 보석과 식물 장식을 유지한다.
- 게임용 외곽선은 먹색·회보라로 강화하고 옷 그림자는 차가운 회보라, 보석 발광은 제한적인 청록으로 처리한다.
- 모든 동작은 오른쪽을 보는 2D 횡스크롤 측면 시점이며 프레임마다 머리 높이, 몸통 길이, 발바닥 기준선과 조명 방향을 유지한다.
- 생성 배경은 완전히 균일한 `#00FF00`이며 캐릭터 내부에는 같은 색을 사용하지 않는다.
- 적용본은 투명 RGBA PNG, PPU 32, Bilinear, Mipmap Off, Clamp, Uncompressed다.
- 기존 이동 수치, 콜라이더, 공격 범위, Sprite 이름과 애니메이션 클립 이름은 변경하지 않는다.
- 기존 런타임 PNG 교체는 사용자 요청에 따라 허용하며 이전 버전은 Git 기록으로 보존한다.

---

### Task 1: 출력 구조와 기준 검증

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/PROMPTS.md`
- Create: `docs/concept-art/generated/chibi-player-assets/contact-sheets/`
- Create: `HiddenWeight/Assets/Art/Player/Abilities/`

**Interfaces:**
- Consumes: `docs/chibi.png`, 기존 플레이어 시트 6개
- Produces: 모든 후속 작업의 생성 원본·검토물 저장 구조

- [ ] **Step 1: 기준 이미지와 기존 시트 규격 검사**

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
expected = {
    'docs/chibi.png': (1254, 1254),
    'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png': (1536, 1024),
    'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png': (2048, 768),
    'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png': (1536, 1024),
    'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png': (2172, 724),
    'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png': (2172, 724),
    'HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png': (1536, 1023),
}
for name, size in expected.items():
    actual = Image.open(name).size
    assert actual == size, (name, actual, size)
print('PASS reference and existing sheets', len(expected))
PY
```

Expected: `PASS reference and existing sheets 7`.

- [ ] **Step 2: 출력 폴더 생성**

```bash
mkdir -p docs/concept-art/generated/chibi-player-assets/contact-sheets
mkdir -p HiddenWeight/Assets/Art/Player/Abilities
```

- [ ] **Step 3: 생성 기록 문서에 공통 규칙 기록**

`PROMPTS.md`에는 기준 이미지 역할, 기존 시트 역할, 크로마키 제거 명령, 각 시트의 최종 프롬프트·원본·적용본 경로를 기록한다.

- [ ] **Step 4: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets HiddenWeight/Assets/Art/Player
git commit -m "[chore] 치비 플레이어 아트 출력 구조 준비"
```

### Task 2: 핵심 포즈 8종 생성

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/Player_KeyPoses_Chibi_Source.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png`
- Modify: `docs/concept-art/generated/chibi-player-assets/PROMPTS.md`

**Interfaces:**
- Consumes: `docs/chibi.png`, 기존 `Player_KeyPoses_v1.png`
- Produces: 나머지 시트의 캐릭터 정체성 기준 `Player_KeyPoses_v1.png`

- [ ] **Step 1: built-in 이미지 생성 도구로 4×2 시트 생성**

```text
Use case: stylized-concept
Asset type: 2D side-view metroidvania player key-pose sprite atlas, exact 4 columns by 2 rows
Input images: chibi.png is the immutable identity and costume reference; current Player_KeyPoses_v1.png is pose-layout reference only
Primary request: redraw the eight gameplay poses using the chibi reference character
Rows and columns: Idle, Walk, Run, Jump / Fall, Land, Attack, Dash
Identity: short white bob hair with one upward ahoge, large cyan eyes, cream oversized turtleneck dress, charcoal leggings, white boots, tiny cyan gemstone and botanical ornaments
Game adaptation: stronger charcoal-lavender outline, cool lavender shadows, restrained cyan glow
Consistency: identical head size, body proportion, costume length, accessory placement, lighting and side-view camera in every cell; face right
Backdrop: perfectly flat uniform pure #00FF00 chroma green
Constraints: one full character per equal cell, generous padding, feet aligned, no cell overlap, no labels, no text, no scenery, no cast shadow, no watermark, no extra weapon; attack uses a pale crescent energy slash
```

- [ ] **Step 2: 원본을 1536×1024로 정규화하고 보존**

Pillow로 정확히 1536×1024 RGB 크로마키 원본을 저장한다.

- [ ] **Step 3: 크로마키 제거**

```bash
python "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py" \
  --input docs/concept-art/generated/chibi-player-assets/Player_KeyPoses_Chibi_Source.png \
  --out HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png \
  --key-color '#00ff00' --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill --force
```

- [ ] **Step 4: 4×2 셀별 외형 검수**

각 셀에 한 캐릭터만 있고 머리·눈·의상·레깅스·부츠·청록 장식이 유지되며 공격 이외 셀에 베기 VFX가 없는지 확인한다.

- [ ] **Step 5: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets \
  HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png
git commit -m "[art] 치비 플레이어 핵심 포즈 교체"
```

### Task 3: 지상 이동과 공중 동작 생성

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Locomotion_Chibi_Source.png`
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Aerial_Chibi_Source.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png`

**Interfaces:**
- Consumes: 승인된 치비 핵심 포즈, 기존 Locomotion·Aerial 시트
- Produces: 기존 클립 이름으로 재생 가능한 24프레임 시트 2개

- [ ] **Step 1: Locomotion 8×3 생성**

```text
Use case: stylized-concept
Asset type: transparent-ready 2D player locomotion sprite sheet, exact 8 columns by 3 rows
References: chibi.png fixes identity; approved chibi key poses fix game proportions; old locomotion sheet fixes motion timing only
Rows: 8-frame subtle Idle breathing, 8-frame Walk cycle, 8-frame Run cycle
Character: exact approved white-haired cyan-eyed cream-turtleneck chibi, right-facing side view
Consistency: fixed camera, fixed scale, bottom-aligned feet, identical outfit and accessories; seamless first-to-last loop
Backdrop: perfectly flat uniform pure #00FF00
Avoid: text, labels, background, shadow, duplicated body parts, changing hair length, changing costume, frame overlap, cropping, green inside character
```

- [ ] **Step 2: Aerial 6×4 생성**

```text
Use case: stylized-concept
Asset type: transparent-ready 2D player aerial sprite sheet, exact 6 columns by 4 rows
References: chibi.png fixes identity; approved key poses fix game proportions; old aerial sheet fixes motion timing only
Rows: Jump takeoff and rise / AirMove / Fall / Land and return to idle
Character: exact approved white-haired cyan-eyed cream-turtleneck chibi, right-facing side view
Consistency: fixed scale and camera, predictable body arc inside each cell, landing final frame matches idle
Backdrop: perfectly flat uniform pure #00FF00
Avoid: text, scenery, floor, cast shadow, extra character, changing clothing or accessories, cropping, overlap, green in subject
```

- [ ] **Step 3: 적용본 정규화·크로마 제거**

Locomotion은 2048×768, Aerial은 1536×1024로 정규화한 뒤 Task 2와 같은 키 제거 설정을 사용한다.

- [ ] **Step 4: 반복성과 발 기준선 검사**

Idle·Walk·Run의 첫/마지막 셀과 Land 마지막/Idle 첫 셀을 나란히 합성한 검사 시트를 만든다.

- [ ] **Step 5: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets \
  HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png
git commit -m "[art] 치비 플레이어 이동 애니메이션 교체"
```

### Task 4: 공격·대시와 벽 동작 생성

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Actions_Chibi_Source.png`
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Wall_Chibi_Source.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png`

**Interfaces:**
- Consumes: 치비 핵심 포즈, 기존 Actions·Wall 시트
- Produces: 6×2 액션 시트 두 장

- [ ] **Step 1: Actions 6×2 생성**

```text
Use case: stylized-concept
Asset type: 2D metroidvania player action sprite sheet, exact 6 columns by 2 rows
References: chibi.png identity; approved key poses proportions; old actions sheet timing
Rows: Attack anticipation-swing-follow-through-return / Dash crouch-launch-speed-recovery
Character: exact approved chibi, right-facing, no handheld weapon
Attack VFX: pale white-lavender crescent energy only in middle attack frames
Backdrop: perfectly flat pure #00FF00
Consistency: fixed body proportions, costume and accessories, bottom-center registration, no overlap or cropping
Avoid: scenery, labels, extra weapon, extra character, random particles in dash recovery, green in subject
```

- [ ] **Step 2: Wall 6×2 생성**

```text
Use case: stylized-concept
Asset type: 2D metroidvania wall-movement sprite sheet, exact 6 columns by 2 rows
References: chibi.png identity; approved key poses proportions; old wall sheet timing
Rows: WallCling subtle hold loop / WallJump compression-push-air-recovery
Character: exact approved chibi, right-facing base orientation
Constraints: show contact through hand and foot pose only; draw no wall; fixed scale and costume
Backdrop: perfectly flat pure #00FF00
Avoid: wall scenery, shadow, text, labels, overlap, cropping, green in subject
```

- [ ] **Step 3: 2172×724 정규화·크로마 제거**

두 원본을 정확히 2172×724로 저장하고 적용본에 알파를 만든다.

- [ ] **Step 4: 셀 크기 검사**

```bash
python3 - <<'PY'
from PIL import Image
for p in [
 'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png',
 'HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png']:
    im=Image.open(p)
    assert im.size == (2172,724)
    assert im.width//6 == 362 and im.height//2 == 362
print('PASS actions and wall 362px cells')
PY
```

- [ ] **Step 5: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets \
  HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png \
  HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png
git commit -m "[art] 치비 플레이어 액션 애니메이션 교체"
```

### Task 5: 플레이어 피격·사망·리스폰 VFX 교체

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/PlayerVFX_Chibi_Source.png`
- Modify: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png`

**Interfaces:**
- Consumes: 치비 핵심 포즈와 기존 PlayerVFX 흐름
- Produces: 캐릭터 외형이 일치하는 6×3 VFX 시트

- [ ] **Step 1: 6×3 VFX 생성**

```text
Use case: stylized-concept
Asset type: 2D player reaction VFX sprite sheet, exact 6 columns by 3 rows
References: chibi.png and approved key poses lock character identity; old PlayerVFX sheet locks effect timing
Rows: Hit flash and recoil / Death dissolve / Respawn reform
Character: exact approved chibi with cream dress, charcoal leggings, white boots and cyan ornaments
Effects: white-lavender impact, smoky gray-violet dissolve, restrained cyan reform particles
Backdrop: perfectly flat pure #00FF00
Constraints: one temporal frame per equal cell, same body scale and position, no gore, no text, no scenery, no overlap, no green subject pixels
```

- [ ] **Step 2: 1536×1023 정규화·크로마 제거**

6×3 셀은 256×341로 유지한다.

- [ ] **Step 3: 캐릭터 외형과 소멸 진행 검수**

Hit 첫 프레임이 플레이어 기본 포즈와 같은 크기인지, Death는 불투명→투명, Respawn은 투명→불투명 순서인지 확인한다.

- [ ] **Step 4: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets \
  HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png
git commit -m "[art] 치비 플레이어 반응 VFX 교체"
```

### Task 6: 숨죽이기와 자각 신규 시트

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Hush_Source.png`
- Create: `docs/concept-art/generated/chibi-player-assets/Player_Awareness_Source.png`
- Create: `HiddenWeight/Assets/Art/Player/Abilities/Player_Hush_v1.png`
- Create: `HiddenWeight/Assets/Art/Player/Abilities/Player_Awareness_v1.png`

**Interfaces:**
- Consumes: 치비 핵심 포즈
- Produces: 추후 Hush·Awareness 애니메이터가 소비할 6×3 시트 두 장

- [ ] **Step 1: Hush 6×3 생성**

```text
Use case: stylized-concept
Asset type: 2D player emotion ability sprite sheet, exact 6 columns by 3 rows
Rows: Hush Begin contracting inward / Hush Move small cautious steps / Hush End expanding to normal
Character: exact approved white-haired cyan-eyed cream-turtleneck chibi
Scale rule: Hush loop silhouette fits within 60 percent of normal standing height; shoulders and dress fold inward
Backdrop: perfectly flat pure #00FF00
Constraints: right-facing side view, fixed identity and accessories, one character per cell, no scenery, no text, no shadow, no green in subject
```

- [ ] **Step 2: Awareness 6×3 생성**

```text
Use case: stylized-concept
Asset type: 2D player awareness ability sprite sheet, exact 6 columns by 3 rows
Rows: Awareness Begin / Awareness Loop with delayed double contour / Awareness Unlock ending in calm frontal gaze
Character: exact approved chibi
Effects: restrained cyan eye and gemstone glow, pale delayed outline, cool lavender shadow
Backdrop: perfectly flat pure #00FF00
Constraints: no environment, no text, no extra character, no heavy bloom, no green in subject, consistent size and costume
```

- [ ] **Step 3: 1536×768 정규화·크로마 제거**

두 적용본을 셀 256×256의 6×3 RGBA 시트로 저장한다.

- [ ] **Step 4: Hush 크기와 Awareness 효과 검수**

Hush 중간 행의 알파 바운딩박스 높이가 Begin 첫 프레임의 70% 이하여야 하며, Awareness 이중 윤곽이 셀 경계를 넘지 않아야 한다.

- [ ] **Step 5: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets \
  HiddenWeight/Assets/Art/Player/Abilities
git commit -m "[art] 치비 플레이어 숨죽이기와 자각 시트 추가"
```

### Task 7: Unity 분할 설정 교정

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/Editor/ResidueArtSlicer.cs`
- Create: `HiddenWeight/Assets/Scripts/Editor/ChibiPlayerAbilityArtSlicer.cs`
- Create: `HiddenWeight/Assets/Tests/EditMode/ChibiPlayerArtImporterTests.cs`
- Create or modify: Unity `.meta` files for replaced and new sheets

**Interfaces:**
- Consumes: 최종 RGBA 시트 8개
- Produces: 기존 Sprite 이름을 유지하는 정확한 격자 분할

- [ ] **Step 1: 분할 규격을 검증하는 실패 테스트 작성**

`ChibiPlayerArtImporterTests`에 아래 검증을 작성한다.

- 기존 6개 시트와 신규 2개 시트가 `Sprite / Multiple / PPU 32 / Bilinear / Mipmap Off / Clamp / Uncompressed / Alpha Is Transparency`인지 검사한다.
- KeyPoses는 8개, Locomotion 24개, Aerial 24개, Actions 12개, Wall 12개, PlayerVFX 18개 Sprite인지 검사한다.
- Actions·Wall의 모든 rect가 362×362이고 캔버스 전체 2172×724를 정확히 덮는지 검사한다.
- Hush·Awareness는 각각 18개, 256×256 Sprite인지 검사한다.
- 모든 플레이어 Sprite의 pivot이 정규화 좌표 `(0.5, 0.0)`인지 검사한다.
- 기존 시트의 이름은 현재 이름을 그대로 유지하고 신규 이름은
  `HushBegin_00..05`, `HushMove_00..05`, `HushEnd_00..05`,
  `AwarenessBegin_00..05`, `AwarenessLoop_00..05`,
  `AwarenessUnlock_00..05`인지 검사한다.

- [ ] **Step 2: EditMode 테스트가 기존 설정 때문에 실패하는지 확인**

```bash
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath HiddenWeight -runTests \
  -testPlatform EditMode \
  -testFilter HiddenWeight.Tests.ChibiPlayerArtImporterTests \
  -testResults /tmp/chibi-player-import-red.xml \
  -logFile /tmp/chibi-player-import-red.log
```

Expected: Actions 또는 Wall rect가 341×341이거나 기존 플레이어 pivot이 Center이고,
신규 능력 시트가 아직 Multiple로 분할되지 않아 FAIL.

- [ ] **Step 3: 최소 슬라이서 교정**

`ResidueArtSlicer`의 플레이어 6개 시트는 기존 이름·행 구성을 유지하되 pivot을
Bottom Center로 통일하고 `wrapMode = Clamp`를 명시한다. 격자 크기는 하드코딩하지 않고
실제 이미지 크기를 열·행 수로 나누므로 Actions·Wall이 362×362로 교정된다.

`ChibiPlayerAbilityArtSlicer`는 `Assets/Art/Player/Abilities` 아래 두 시트를
6×3으로 분할하고 기존 슬라이서와 동일한 import 설정을 적용한다.

- [ ] **Step 4: 슬라이서 실행 후 테스트 통과 확인**

```bash
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.ResidueArtSlicer.SliceAll \
  -quit -logFile /tmp/chibi-player-slice.log

"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.ChibiPlayerAbilityArtSlicer.SliceAll \
  -quit -logFile /tmp/chibi-player-ability-slice.log
```

Step 2의 EditMode 테스트를 다시 실행해 PASS를 확인한다. Bottom Center 변경으로
보이는 위치가 달라질 수 있으므로 기존 플레이어 GameObject의 SpriteRenderer
로컬 위치는 변경하지 않고, Task 8 PlayMode 테스트와 프레임 감사 시트에서
발바닥 기준선이 유지되는지 확인한다.

- [ ] **Step 5: 커밋**

```bash
git add HiddenWeight/Assets/Art/Residue/Gameplay/Player \
  HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png* \
  HiddenWeight/Assets/Art/Player/Abilities \
  HiddenWeight/Assets/Scripts/Editor/ResidueArtSlicer.cs \
  HiddenWeight/Assets/Scripts/Editor/ChibiPlayerAbilityArtSlicer.cs \
  HiddenWeight/Assets/Tests/EditMode/ChibiPlayerArtImporterTests.cs
git commit -m "[fix] 치비 플레이어 스프라이트 분할 교정"
```

### Task 8: 전체 합성 검수와 회귀 테스트

**Files:**
- Create: `docs/concept-art/generated/chibi-player-assets/contact-sheets/chibi-player-all-sheets.jpg`
- Create: `docs/concept-art/generated/chibi-player-assets/contact-sheets/chibi-player-frame-audit.jpg`
- Modify: `docs/concept-art/generated/chibi-player-assets/PROMPTS.md`

**Interfaces:**
- Consumes: 최종 시트 8개
- Produces: 사용자 검토 연락판과 검증 기록

- [ ] **Step 1: PNG·알파·크로마 전수 검사**

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image
paths = [
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png'),
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png'),
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png'),
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png'),
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png'),
 Path('HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png'),
 Path('HiddenWeight/Assets/Art/Player/Abilities/Player_Hush_v1.png'),
 Path('HiddenWeight/Assets/Art/Player/Abilities/Player_Awareness_v1.png'),
]
for p in paths:
    im=Image.open(p).convert('RGBA')
    assert im.getchannel('A').getextrema()[0] == 0, p
    green=sum(1 for r,g,b,a in im.getdata() if a>32 and g>210 and r<80 and b<80)
    assert green == 0, (p, green)
print('PASS chibi player sheets', len(paths))
PY
```

- [ ] **Step 2: 전체 연락판과 프레임 감사 시트 생성**

체크무늬 배경 위에 8개 시트를 배치한 전체 연락판과 각 행을 확대해 발 기준선을 표시한 감사 시트를 만든다.

- [ ] **Step 3: Unity 전체 테스트**

```bash
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath HiddenWeight -runTests \
  -testPlatform EditMode -testResults /tmp/chibi-player-edit.xml \
  -logFile /tmp/chibi-player-edit.log

"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath HiddenWeight -runTests \
  -testPlatform PlayMode -testResults /tmp/chibi-player-play.xml \
  -logFile /tmp/chibi-player-play.log
```

Expected: EditMode와 PlayMode 모두 실패 0.

- [ ] **Step 4: 생성 기록과 최종 경로 갱신**

`PROMPTS.md`에 실제 사용한 최종 프롬프트, 생성 원본, Unity 적용본, 접촉 시트와 테스트 결과를 기록한다.

- [ ] **Step 5: 커밋**

```bash
git add docs/concept-art/generated/chibi-player-assets HiddenWeight/Assets/Art
git commit -m "[art] 치비 메인 플레이어 전체 교체 완료"
```
