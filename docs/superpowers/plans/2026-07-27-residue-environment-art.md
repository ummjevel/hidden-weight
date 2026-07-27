# Residue Environment Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 잔재 지역의 플레이 가능한 전경을 구성하는 지형·발판·되감기 구조물·위험물·장식·환경 VFX 투명 PNG 6종을 제작한다.

**Architecture:** 기존 잔재 배경 및 아틀라스를 스타일 참조로 사용해 균일한 녹색 배경의 모듈형 시트를 각각 생성한다. 생성 원본은 문서 폴더에 보존하고 크로마키 제거 및 정수 격자 정규화를 거친 RGBA PNG만 Unity Art 폴더에 배치한다.

**Tech Stack:** Built-in ImageGen, `remove_chroma_key.py`, ImageMagick, Unity 2D Sprite Multiple

## Global Constraints

- 기존 `Residue_TerrainAtlas.png`, `Residue_InteractablesAtlas.png`를 덮어쓰지 않는다.
- 최종 파일은 RGBA PNG이며 모든 시트가 지정한 행·열로 나머지 없이 분할되어야 한다.
- 팔레트는 검은 철골, 회갈색 석재, 낮은 앰버 발광, 제한적인 남색·남보라 반사광이다.
- 캐릭터, 적, UI, 배경 풍경, 텍스트, 워터마크, 투영 그림자를 포함하지 않는다.
- Collider는 이미지에서 생성하지 않고 Unity의 별도 Collider로 구성한다.

---

### Task 1: 저장 폴더와 기준 파일 준비

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Hazards/`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Props/`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/VFX/`
- Create: `docs/concept-art/generated/residue-environment-assets/`

**Interfaces:**
- Consumes: 기존 잔재 지형·상호작용·방 배경 PNG
- Produces: 생성 원본과 최종 RGBA PNG의 고정 저장 위치

- [ ] **Step 1: 대상 폴더 생성**

Run:

```bash
mkdir -p \
  HiddenWeight/Assets/Art/Residue/Environment/{Terrain,Interactables,Hazards,Props,VFX} \
  docs/concept-art/generated/residue-environment-assets
```

- [ ] **Step 2: 기존 파일 비덮어쓰기 확인**

Run:

```bash
find HiddenWeight/Assets/Art/Residue/Environment -type f -name '*.png'
```

Expected: 이번 계획의 여섯 파일명이 아직 존재하지 않는다.

---

### Task 2: 지형과 발판 시트 생성

**Files:**
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_TerrainTiles_v2_Source.png`
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_Platforms_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/Residue_TerrainTiles_v2.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/Residue_Platforms_v1.png`

**Interfaces:**
- Consumes: `Residue_TerrainAtlas.png`, `Residue_InteractablesAtlas.png`
- Produces: 6×4 지형 모듈과 6×3 발판 모듈

- [ ] **Step 1: ImageGen으로 두 원본을 별도 호출해 생성**

지형은 직선·모서리·벽·경사·파손 끝 24셀, 발판은 돌·철골·매달림·뼈 발판 18셀을
각각 균일한 `#00ff00` 배경에 생성한다.

- [ ] **Step 2: 크로마키 제거**

Run for each source:

```bash
python /Users/ksh/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py \
  --input SOURCE.png --out FINAL.png --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill --force
```

- [ ] **Step 3: 정수 격자로 정규화**

지형은 6×4, 발판은 6×3으로 정확히 나뉘는 가장 가까운 캔버스 크기로 오른쪽·아래만
투명 패딩한다. 물체를 비율 변경하지 않는다.

---

### Task 3: 상호작용 구조물과 위험물 시트 생성

**Files:**
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_RewindStructures_v1_Source.png`
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_Hazards_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Residue_RewindStructures_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Hazards/Residue_Hazards_v1.png`

**Interfaces:**
- Consumes: 기존 숏컷 아틀라스와 되감기 기획
- Produces: 동일 피벗의 파손·복원·중간 상태 24셀, 위험 예고·공격 18셀

- [ ] **Step 1: 되감기 시트 생성**

6열은 소형 발판, 중형 발판, 사슬다리, 철문, 승강기 바닥, 도르래다. 각 열의 행은
파손, 복원, 복원 초기, 복원 후기로 고정한다.

- [ ] **Step 2: 위험물 시트 생성**

바닥·천장·벽 가시, 심연 촉수, 압착 장치, 붕괴 바닥, 낙하물의 대기·예고·공격 상태를
6×3으로 생성한다.

- [ ] **Step 3: 두 시트를 RGBA로 변환하고 정수 격자로 패딩**

Task 2와 같은 크로마키 제거 및 패딩 절차를 사용한다.

---

### Task 4: 장식과 환경 VFX 시트 생성

**Files:**
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_EnvironmentProps_v1_Source.png`
- Create: `docs/concept-art/generated/residue-environment-assets/Residue_AmbientVFX_v1_Source.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/Props/Residue_EnvironmentProps_v1.png`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Residue_AmbientVFX_v1.png`

**Interfaces:**
- Consumes: 방 배경의 철골·손·사슬·교수대 모티프
- Produces: 24개 독립 장식과 3종×6프레임 환경 루프

- [ ] **Step 1: 환경 장식 6×4 생성**

기둥, 철골, 돌, 사슬, 우리, 교수대, 뼈, 손가락, 제단, 시체 잔재, 부조, 눈, 석상,
등, 균열·얼룩·재 데칼을 분리 생성한다.

- [ ] **Step 2: 환경 VFX 6×3 생성**

재·먼지, 낮은 남색 안개, 앰버 불씨·작은 낙하 파편을 각각 6프레임 루프로 생성한다.

- [ ] **Step 3: RGBA 변환과 정수 격자 패딩**

VFX의 반투명 가장자리를 보존하고 녹색 잔상이 있으면 `--edge-contract 1`로 한 번만
재처리한다.

---

### Task 5: 프롬프트·분할표 기록과 검증

**Files:**
- Create: `docs/concept-art/generated/residue-environment-assets/PROMPTS.md`
- Create: `HiddenWeight/Assets/Art/Residue/Environment/README.md`

**Interfaces:**
- Consumes: 최종 여섯 시트의 실제 크기
- Produces: 재생성 가능한 프롬프트 요약과 Unity Sprite Editor 분할표

- [ ] **Step 1: 파일별 실제 크기와 알파 검사**

Run:

```bash
magick identify HiddenWeight/Assets/Art/Residue/Environment/*/*.png
file HiddenWeight/Assets/Art/Residue/Environment/*/*.png
```

Expected: 여섯 파일 모두 RGBA PNG.

- [ ] **Step 2: 격자 나눗셈 검사**

각 이미지 폭을 열 수로, 높이를 행 수로 나눴을 때 나머지가 0이어야 한다.

- [ ] **Step 3: 시각 검사**

각 시트를 확인해 잘린 물체, 남은 녹색 배경, 셀 침범, 스타일 불일치가 없는지 검사한다.

- [ ] **Step 4: 문서 작성**

`PROMPTS.md`에는 공통 스타일과 셀별 구성을, `README.md`에는 파일 경로, 행·열, 셀 크기,
피벗, FPS, Collider 분리 규칙을 기록한다.

- [ ] **Step 5: 문서와 파일 검증**

Run:

```bash
git diff --check -- \
  docs/concept-art/generated/residue-environment-assets/PROMPTS.md \
  HiddenWeight/Assets/Art/Residue/Environment/README.md
```

Expected: 출력 없이 종료 코드 0.
