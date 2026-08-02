# PRODUCTION_20260802_TUTORIAL_ART_PROMPTS.md — 몽환의 우주 아트 목록·생성 프롬프트

> 상위 문서: `LEVEL_10_DREAM_TUTORIAL.md`, `PRODUCTION_20260802_VIDEO_SCENARIOS.md`
>
> 목적: T01~T04 구현에 필요한 이미지, 크기, 분위기, 생성 프롬프트와 검수 기준을 확정한다.
>
> 적용 범위: `Zone_Prologue` 전용. 잔재·응시·균열 에셋은 수정하지 않는다.
>
> 문서 상태: 배경·보행 지형 1차 적용 완료, 게임 화면 검수 대기 · 2026-08-02

---

## 0. 현재 상태와 제작 전략

- 현재 프롤로그는 T01~T04 네 방으로 구현되어 이동·점프·벽점프·대시·공격을 차례로 익힌다.
- 승인된 우주 배경 4장을 각 방에 연결했다.
- 생성한 지형 아틀라스의 긴 발판을 튜토리얼 전용 보행 바닥으로 분리해 실제 충돌 블록 위에 표시한다.
- 잔재는 기존 4K 배경과 지형·보스 에셋이 충분하므로 이 문서에서 새로 생성하지 않는다.
- 생성 이미지는 배경·장식에 사용하고, 충돌 판정은 Unity Tilemap/Collider로 별도 유지한다.
- 배경에 실제 발판처럼 보이는 수평 모서리를 만들지 않는다.
- 키 문자와 안내 문구는 이미지에 굽지 않고 런타임 UI로 표시한다.

---

## 1. 공통 비주얼 방향

### 1.1 핵심 인상

```text
고요한 탄생
→ 공간이 플레이어의 움직임을 기억함
→ 아름다운 우주가 거대한 눈 또는 자궁 내부처럼 느껴짐
→ 멀리 있던 손이 점점 가까워짐
→ 잔재의 폐허와 맞닿는 경계
```

### 1.2 팔레트

| 역할 | 색상 방향 |
| --- | --- |
| 바탕 | 먹색, 거의 검은 남보라 |
| 깊이 | 짙은 인디고, 탁한 보라 |
| 기억·궤도 | 낮은 채도의 라벤더 |
| 플레이 가능 지형 | 차가운 은백색 외곽선 |
| 첫 반응 | 희미한 청백색 별빛 |
| 잔재 전이 | 유황빛이 아닌 탁한 갈색·골드 극소량 |

### 1.3 공통 생성 제약

- 2D 횡스크롤 게임 배경, 16:9
- 화면 아래 35%는 게임 지형이 놓일 수 있도록 시각적 복잡도를 낮춘다.
- 플레이 경로로 오인할 수 있는 밝고 긴 수평선, 계단, 다리 모양을 배경에서 피한다.
- 캐릭터, 적, 텍스트, UI, 로고, 워터마크를 넣지 않는다.
- 별과 성운을 과밀하게 채우지 않는다.
- 사진 같은 실제 우주보다 회화적인 심리적 공간으로 표현한다.
- 과도한 고어, 장기, 피, 명확한 인간 얼굴·눈알을 사용하지 않는다.
- 네 방은 같은 세계처럼 보여야 하며 손 실루엣의 거리와 어두운 파편 양만 단계적으로 바뀐다.

---

## 2. MVP 필수 이미지 목록

| ID | 파일명 제안 | 용도 | 수량·크기 | 출력 |
| --- | --- | --- | --- | --- |
| BG-T01 | `Prologue_T01_BG_v1.png` | 무중력의 씨앗 원경 | 3840×2160 | RGB PNG |
| BG-T02 | `Prologue_T02_BG_v1.png` | 별 사이의 틈 원경 | 3840×2160 | RGB PNG |
| BG-T03 | `Prologue_T03_BG_v1.png` | 기울어진 궤도 원경 | 3840×2160 | RGB PNG |
| BG-T04 | `Prologue_T04_BG_v1.png` | 잔재의 경계 원경 | 3840×2160 | RGB PNG |
| MOD-01 | `Prologue_OrbitHand_Modules_v1.png` | 궤도·별빛·손 실루엣 모듈 | 2048×2048 | RGBA PNG |
| TERR-01 | `Prologue_TerrainPlatforms_v1.png` | 은백색 바닥·벽·발판 | 2048×2048 | RGBA PNG |
| FG-01 | `Prologue_ForegroundFragments_v1.png` | 화면 가장자리 파편·실 | 2048×2048 | RGBA PNG |

### 2.1 선택 이미지

| ID | 파일명 제안 | 크기 | 사용 조건 |
| --- | --- | ---: | --- |
| VFX-01 | `Prologue_AmbientVFX_v1.png` | 2048×2048 RGBA | 별빛 활성·대시 잔광 품질이 코드 효과만으로 부족할 때 |
| ENEMY-01 | `Prologue_NamelessEcho_v1.png` | 2048×1024 RGBA | 기존 잔영 재사용이 지역 톤과 크게 어긋날 때 |
| UI-01 | `Prologue_PromptFrames_v1.png` | 1024×1024 RGBA | 현재 월드 안내 프레임의 가독성이 부족할 때 |
| TRANS-01 | `Prologue_ResidueGate_Overlay_v1.png` | 3840×2160 RGBA | T04→R01 전환에 손·문 겹침 연출이 필요할 때 |

오늘은 필수 7종을 우선 검수한다. 선택 이미지는 플레이테스트에서 부족함이 확인된 항목만 생성한다.

---

## 3. 방별 배경 프롬프트

아래 프롬프트는 `imagegen` 기본 도구로 각각 별도 생성한다. 네 장을 한 번에 한 이미지로 만들지 않는다.

### 3.1 T01 — 무중력의 씨앗

```text
Use case: stylized-concept
Asset type: production game background for T01 of an original 2D side-scrolling psychological dream game
Primary request: a silent dreamlike outer space where a tiny self is about to awaken, expressing memory before it has taken a clear shape
Scene/backdrop: vast charcoal-black and deep-indigo space, sparse cold-white stars, translucent muted-lavender nebulae, one or two extremely thin incomplete circular orbits, a very distant ambiguous hand-shaped constellation barely readable near the horizon
Style/medium: painterly high-detail 2D game environment, psychological cosmic surrealism, soft atmospheric depth, not photorealistic
Composition/framing: 16:9 wide side-scrolling background; calm open negative space; lower 35 percent dark and visually quiet for gameplay collision art; no bright horizontal shapes that resemble platforms
Lighting/mood: still, weightless, gentle birth, beautiful before unsettling
Color palette: charcoal, deep indigo, muted lavender, cold white
Constraints: no character, no enemy, no text, no UI, no logo, no watermark, no explicit eye, no gore, no detailed foreground objects, no false floors, bridges, stairs or ledges
```

검수 포인트: 시작 화면이 비어 보이되 밋밋하지 않은가, 손은 첫눈에 괴물처럼 보이지 않는가,
플레이 지형을 놓을 아래 영역이 충분히 조용한가.

### 3.2 T02 — 별 사이의 틈

```text
Use case: stylized-concept
Asset type: production game background for T02 of an original 2D side-scrolling psychological dream game
Primary request: the same dream universe opening into a vertical gap between stars, suggesting that the world responds when the player learns to climb
Scene/backdrop: charcoal and deep-indigo cosmic interior, sparse star fields separated by a tall dark vertical void, long translucent threads of cold light hanging downward, incomplete circular orbit fragments curving around the void, the same distant hand-shaped constellation slightly larger than in T01
Style/medium: painterly high-detail 2D game environment, restrained cosmic surrealism, layered atmospheric depth
Composition/framing: 16:9 side-scrolling background with vertical visual flow near the middle-right; lower 35 percent and the wall-jump chimney area remain low contrast; no shapes that could be mistaken for climbable walls or platforms
Lighting/mood: curious, safe uncertainty, upward invitation rather than danger
Color palette: charcoal, deep indigo, muted lavender, cold white with a little more active starlight than T01
Constraints: no character, no enemy, no text, no UI, no logo, no watermark, no explicit eye, no gore, no bright fake ledges, no architectural stairs or bridges
```

검수 포인트: 수직 상승 방향은 느껴지되 실제 벽 위치와 경쟁하지 않는가, T01과 같은 세계로 보이는가.

### 3.3 T03 — 기울어진 궤도

```text
Use case: stylized-concept
Asset type: production game background for T03 of an original 2D side-scrolling psychological dream game
Primary request: the dream universe begins to resist movement, with a vast tilted orbit cutting through space and the first hint of an approaching emotional weight
Scene/backdrop: deep-indigo and charcoal space, one enormous incomplete circular orbit tilted diagonally across the upper half, sparse stars stretched slightly in one direction as if remembering motion, the same hand-shaped constellation now clearly enormous but still made only from negative space and stars, a small amount of dark debris entering from the frame edges
Style/medium: painterly high-detail 2D game environment, psychological cosmic surrealism, strong depth separation
Composition/framing: 16:9 wide background; diagonal movement in the upper two-thirds; lower gameplay band dark, simple and readable; no horizontal background edge matching the playable ground
Lighting/mood: first resistance, controlled tension, momentum and discovery
Color palette: charcoal, indigo, muted lavender, cold white, extremely restrained dusty brown in distant debris
Constraints: no character, no enemy, no text, no UI, no logo, no watermark, no explicit face or eyeball, no gore, no fake platforms or attack effects
```

검수 포인트: 대시 방향성이 느껴지는가, 손이 명확해졌지만 배경 랜드마크로 남는가, 전투 가독성을
해칠 밝은 요소가 아래쪽에 없는가.

### 3.4 T04 — 잔재의 경계

```text
Use case: stylized-concept
Asset type: production game background for T04 of an original 2D side-scrolling psychological dream game
Primary request: a seamless border where the quiet cosmic dream is being transformed into the first ruined region, while preserving a clear route for the player's final tutorial test
Scene/backdrop: the same charcoal-indigo universe, the enormous hand silhouette now closest and partially outside the frame, circular orbits breaking into hanging cables and ruined structural fragments, muted-lavender starlight fading into dusty brown and dim old-gold traces toward the exit, dark fragments and thread-like shapes gathering along the outer frame
Style/medium: painterly high-detail 2D game environment, psychological cosmic surrealism transitioning into restrained architectural ruin
Composition/framing: 16:9 side-scrolling background; visual transition progresses across the frame without putting a fake gate or fake floor in the gameplay band; lower 35 percent remains low contrast; center and exit retain clean silhouette space
Lighting/mood: culmination, unease, voluntary crossing into an unknown memory
Color palette: charcoal, deep indigo, muted lavender, cold white, restrained dusty brown and old gold near the boundary
Constraints: no character, no enemy, no text, no UI, no logo, no watermark, no explicit human face or eye, no gore, no bright fake ledges, no fully rendered gameplay gate
```

검수 포인트: T03에서 자연스럽게 이어지는가, 잔재의 갈색 톤은 출구 쪽에만 제한되는가, 실제 문과
발판을 올릴 빈 공간이 있는가.

---

## 4. 투명 모듈·지형 프롬프트

투명 에셋은 기본 `imagegen`에서 평평한 크로마키 배경으로 생성한 뒤 로컬 도구로 제거한다.
피사체에 녹색이 거의 없으므로 기본 키 색은 `#00ff00`을 사용한다.

### 4.1 궤도·별빛·손 모듈

```text
Use case: stylized-concept
Asset type: modular 2D game environment sprite atlas for a cosmic dream tutorial
Primary request: a clean atlas of separate reusable cosmic ornaments: four incomplete circular orbit arcs, six sparse star clusters, four thin hanging light threads, two distant ambiguous hand-shaped constellations, and four small dormant-to-awake starlight motifs
Style/medium: painterly 2D game sprites with crisp readable silhouettes and soft internal glow
Composition/framing: orthographic sprite-sheet presentation; every object isolated, non-overlapping, fully visible, generous uniform padding, consistent scale families
Color palette: cold white, muted lavender, deep indigo details
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Constraints: one uniform background color with no shadows, gradients, texture, reflections or floor plane; no #00ff00 in any sprite; no text, character, UI, logo, watermark; no complete scene; no cast shadows; no cropped objects
```

### 4.2 은백색 지형·발판

```text
Use case: stylized-concept
Asset type: modular 2D side-scrolling game terrain sprite atlas
Primary request: a coherent set of cosmic dream terrain pieces: straight floor segments, inner and outer corners, vertical wall segments, short floating platforms, rounded step pieces, and wall-jump surface strips; dark translucent indigo bodies with a thin cold silver-white upper rim for gameplay readability
Style/medium: painterly but production-readable 2D game terrain, restrained texture, clean silhouettes, consistent edge thickness
Composition/framing: orthographic atlas grid; each terrain piece isolated and non-overlapping with generous padding; matching edges designed to connect visually; no perspective
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Constraints: one uniform background color with no shadows, gradients, texture, reflections or floor plane; no #00ff00 in the terrain; no scenery, text, character, UI, logo or watermark; no lighting baked from a specific direction; no cropped pieces
```

생성 결과가 정확한 타일 연결 규칙을 만족하지 못하면 반복 생성으로 억지 보정하지 않는다. 직선 바닥과
벽을 우선 사용하고 모서리는 Unity에서 겹쳐 조립하거나 결정적인 코드형 외곽선으로 보완한다.

### 4.3 전경 파편·실

```text
Use case: stylized-concept
Asset type: modular foreground overlay sprite atlas for a 2D cosmic dream game
Primary request: separate dark edge-framing elements that increase from quiet dream to ruined memory: eight charcoal fragments, six thin hanging threads or cables, four soft indigo mist clusters, and two partial broken-orbit silhouettes
Style/medium: painterly atmospheric 2D foreground sprites, soft edges where appropriate, no detailed focal objects
Composition/framing: every object isolated, non-overlapping and fully visible with generous padding; shapes designed for screen edges and corners without covering the center gameplay area
Color palette: charcoal, deep indigo, muted lavender accents; no bright highlights
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Constraints: one uniform background color with no shadows, gradients, texture, reflections or floor plane; no #00ff00 in any sprite; no text, character, UI, logo, watermark; no complete scene; no fake platforms; no cropped objects
```

---

## 5. 이미지 생성·검수 절차

### 5.1 생성 순서

1. T01과 T04 배경을 먼저 생성해 지역의 시작·끝 톤을 검수한다.
2. 승인된 두 장을 기준 이미지로 T02와 T03을 생성한다.
3. 네 배경 승인 후 궤도·손 모듈을 생성한다.
4. 지형 아틀라스를 생성하고 실제 충돌 블록 위에 시험 배치한다.
5. 전경 아틀라스는 마지막에 생성해 가독성을 해치지 않는 범위에서만 사용한다.

### 5.2 사용자 검수 질문

각 이미지마다 다음 네 항목만 먼저 확인한다.

- 같은 게임의 같은 지역처럼 보이는가?
- 실제로 걸을 수 있는 길이 배경보다 분명하게 보이는가?
- 아름다움과 불편함의 비율이 적절한가?
- 거대한 손이 너무 직접적인 괴물처럼 보이지 않는가?

### 5.3 기술 검수

- 배경 원본: 3840×2160, 16:9, 알파 불필요
- 1920×1080 게임 뷰에서 캐릭터와 지형 외곽선 대비 확인
- 투명 PNG: 알파 채널, 모서리 투명, 녹색 프린지 없음
- 스프라이트 아틀라스: 오브젝트 간 패딩과 잘림 여부 확인
- Unity Import: 배경은 압축으로 띠가 생기지 않게 확인, 지형은 Pixels Per Unit을 실제 충돌 크기에 맞춰 별도 확정
- 전경은 플레이어·적·구덩이·출구를 0.5초 이상 가리지 않는다.

---

## 6. 프로젝트 저장 위치 제안

```text
HiddenWeight/Assets/Art/Prologue/
├── Backgrounds/
│   ├── Prologue_T01_BG_v1.png
│   ├── Prologue_T02_BG_v1.png
│   ├── Prologue_T03_BG_v1.png
│   └── Prologue_T04_BG_v1.png
├── Environment/
│   ├── Prologue_OrbitHand_Modules_v1.png
│   ├── Prologue_TerrainPlatforms_v1.png
│   └── Prologue_ForegroundFragments_v1.png
└── Optional/
    ├── Prologue_AmbientVFX_v1.png
    ├── Prologue_NamelessEcho_v1.png
    ├── Prologue_PromptFrames_v1.png
    └── Prologue_ResidueGate_Overlay_v1.png
```

기존 `Residue`, `Gaze`, `Fracture`, 공용 `Placeholder` 폴더의 이미지는 덮어쓰지 않는다.

---

## 7. 이미지 적용 전 승인선

- [x] T01·T04 프롬프트와 톤 승인
- [x] 방별 배경 시안 4장 사용 결정
- [x] T02를 후속 에셋의 색·가독성 기준으로 선택
- [x] 투명 모듈 3종 생성 및 크로마키 제거 방식 확인
- [ ] 실제 1920×1080 게임 화면 가독성 승인
- [x] 방별 배경을 `Assets/Art/Prologue/Rooms4K`에 저장하고 T01~T04에 연결
- [x] 지형 아틀라스의 긴 발판을 튜토리얼 보행 바닥에 1차 적용
- [x] 궤도·손·성운·파편을 T01~T04에 한 개씩 1차 배치
- [ ] 1920×1080 게임 화면에서 장식 투명도·위치 승인

### 7.1 2026-08-02 생성 현황

- 배경 4장은 생성 도구의 원본 크기인 1672×941로 적용했다. 1920×1080 게임 뷰에서 확대 품질을
  확인하고 부족할 때만 4K 원본을 다시 만든다.
- 궤도·손, 지형·발판, 전경 파편 아틀라스도 1672×941로 생성했다.
- 아틀라스의 녹색 배경은 원본을 보존한 채 프로젝트 복사본에서 투명 알파로 변환했다.
- 지형 아틀라스에서는 긴 발판 하나만 `Prologue_TraversalSurface.png`로 분리해 적용했다.
- 궤도·손과 전경 파편에서 4개만 분리해 충돌 없는 배경 장식으로 1차 배치했다.
- 보행 경로와 안내 문구를 덮지 않도록 방당 한 개만 사용하고, 최종 투명도는 실제 게임 화면에서 확정한다.
