# Fracture Room Layers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 균열 메인 룸 12개와 비밀방 3개를 `BG_Far`, `BG_Mid`, `FG_Overlay` 45장으로 제작한다.

**Architecture:** 각 룸 콘셉트는 합성 결과의 구도 기준이다. 원경은 불투명 전체 화면, 중경과 전경은 크로마키 생성 후 투명 RGBA로 변환하며 실제 발판과 기물은 이후 별도 아틀라스가 담당한다.

**Tech Stack:** built-in `image_gen`, `remove_chroma_key.py`, Pillow, ImageMagick, PNG

## Global Constraints

- 모든 최종 이미지는 1672×941이다.
- `BG_Far`는 RGB 또는 불투명 RGBA다.
- `BG_Mid`와 `FG_Overlay`는 RGBA이며 네 모서리가 투명하다.
- 캐릭터, 적, 아이템, UI, 텍스트, 실제 발판, 활성 장치를 포함하지 않는다.
- 중앙 65%는 플레이 가독성을 위해 전경 구조로 가리지 않는다.
- `docs/concept-art/generated/fracture-map-v1/rooms/*.png`를 룸 구도 기준으로 사용한다.
- 결과는 `HiddenWeight/Assets/Art/Fracture/Room01`~`Room12`, `Secret01`~`Secret03`에 저장한다.

---

### Task 1: 폴더와 프롬프트 준비

**Files:**
- Create: `docs/concept-art/generated/fracture-room-layers/PROMPTS.md`
- Create: `HiddenWeight/Assets/Art/Fracture/Room01/` through `Room12/`
- Create: `HiddenWeight/Assets/Art/Fracture/Secret01/` through `Secret03/`

**Interfaces:**
- Consumes: 룸 콘셉트 15장
- Produces: 45개 출력 경로와 공통 프롬프트

- [ ] 출력 폴더 15개와 연락판 폴더를 생성한다.
- [ ] `BG_Far`, `BG_Mid`, `FG_Overlay` 공통 프롬프트와 룸별 참조 경로를 기록한다.
- [ ] 룸 콘셉트가 정확히 15장이고 모두 1672×941인지 검사한다.

### Task 2: F01~F05 레이어 15장

**Files:**
- Create: `HiddenWeight/Assets/Art/Fracture/Room01/Room01_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room02/Room02_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room03/Room03_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room04/Room04_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room05/Room05_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: F01~F05 룸 콘셉트
- Produces: 입구·허브·하층정원·성소의 패럴랙스 세트

- [ ] 각 룸의 조용한 불투명 원경을 생성·정규화한다.
- [ ] 중거리 아치·온실·수면 구조를 녹색 배경으로 생성하고 알파를 제거한다.
- [ ] 가장자리 꽃·기둥·광학 굴절 전경을 녹색 배경으로 생성하고 알파를 제거한다.
- [ ] 15장의 크기, 모드, 알파 모서리와 합성 가독성을 검사하고 커밋한다.

### Task 3: F06~F10 레이어 15장

**Files:**
- Create: `HiddenWeight/Assets/Art/Fracture/Room06/Room06_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room07/Room07_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room08/Room08_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room09/Room09_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room10/Room10_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: F06~F10 룸 콘셉트
- Produces: 온실·부유 건축·승강축·거울 회랑·감시탑 패럴랙스 세트

- [ ] 불투명 원경 5장을 생성·정규화한다.
- [ ] 투명 중경 5장을 생성·크로마 제거한다.
- [ ] 투명 전경 5장을 생성·크로마 제거한다.
- [ ] 15장을 검사하고 커밋한다.

### Task 4: F11~F12·FS1~FS3 레이어 15장

**Files:**
- Create: `HiddenWeight/Assets/Art/Fracture/Room11/Room11_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Room12/Room12_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Secret01/Secret01_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Secret02/Secret02_{BG_Far,BG_Mid,FG_Overlay}.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Secret03/Secret03_{BG_Far,BG_Mid,FG_Overlay}.png`

**Interfaces:**
- Consumes: F11~F12와 FS1~FS3 룸 콘셉트
- Produces: 미래 폐허·종착점·비밀방 패럴랙스 세트

- [ ] 불투명 원경 5장을 생성·정규화한다.
- [ ] 투명 중경 5장을 생성·크로마 제거한다.
- [ ] 투명 전경 5장을 생성·크로마 제거한다.
- [ ] 15장을 검사하고 커밋한다.

### Task 5: 전체 검증

**Files:**
- Create: `docs/concept-art/generated/fracture-room-layers/contact-sheets/fracture-room-layers.jpg`

**Interfaces:**
- Consumes: 패럴랙스 45장
- Produces: 전체 검증 결과와 연락판

- [ ] 45개 파일 수와 1672×941 크기를 검사한다.
- [ ] 15개 `BG_Far`의 불투명도와 30개 투명 레이어의 RGBA·모서리 알파를 검사한다.
- [ ] 룸별 세 레이어 합성 미리보기와 전체 연락판을 만든다.
- [ ] 중앙 가독성과 팔레트 연속성을 육안 검사한다.
