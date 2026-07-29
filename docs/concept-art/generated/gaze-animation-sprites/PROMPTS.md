# 응시(Gaze) 애니메이션 스프라이트 생성 기록

## 공통 제작 규칙

- 범위: 2맵 `응시`, 플레이어 캐릭터 제외
- 형식: 투명 배경 RGBA PNG, 가로 8프레임
- 팔레트: 저채도 보라·먹색, 기만/위험은 마젠타, 진실/해금은 청록
- 모티브: 눈꺼풀, 홍채, 가면, 철창, 고딕 극장
- 생성 배경: 단색 `#00ff00`; 생성 후 soft matte와 despill로 제거
- 피벗: 적·지상 장치는 Bottom Center, 부유 오브젝트·VFX는 Center
- 공통 제외: 글자, 숫자, 셀 선, UI, 워터마크, 완성된 배경 장면, 플레이어

모든 프롬프트에는 다음 공통 꼬리를 적용했다.

```text
Preserve the referenced Gaze-region palette and painterly 2D metroidvania production-art style.
Keep identity, scale and anchor consistent. One complete isolated state per cell with generous padding,
no crop and no overlap. Flat solid chroma-key green #00ff00 background only.
No scenery, text, labels, grid lines, UI, watermark or player character.
```

## 납품 규격

| 파일 | 격자 | 셀(px) | 권장 FPS | 행 순서 |
|---|---:|---:|---:|---|
| `BlindPilgrim_v1.png` | 8×6 | 181×181 | 8 / 10 / 12 / 14 / 12 / 10 | Idle / Move / Telegraph / Attack / Hit / Death |
| `InformingMouth_v1.png` | 8×6 | 192×170 | 8 / 10 / 12 / 14 / 12 / 10 | Idle / Move / Telegraph / Attack / Hit / Death |
| `HangingAudience_v1.png` | 8×6 | 181×181 | 8 / 10 / 12 / 14 / 12 / 10 | Idle / Move / Telegraph / Attack / Hit / Death |
| `FacelessJudge_v1.png` | 8×6 | 207×207 | 8 / 10 / 10 / 12 / 12 / 9 | Idle / Move / Telegraph / Attack / Hit / Death |
| `IrisGatekeeper_Combat_v1.png` | 8×7 | 192×192 | 8 / 12 / 10 / 12 / 14 / 12 / 9 | Idle / Iris Sweep / Eyelid Close / Charge / Dual Gaze / Hurt / Death |
| `IrisGatekeeper_Transitions_v1.png` | 8×3 | 256×256 | 10 / 12 / 10 | Entrance / Overload / Shortcut Open |
| `GazeOfAll_Combat_v1.png` | 8×7 | 192×192 | 8 / 12 / 12 / 14 / 14 / 12 / 9 | Idle / Fixed Gaze / Rotating Gaze / Projectile / True Strike / Hurt / Death |
| `GazeOfAll_Deceptions_v1.png` | 8×4 | 192×192 | 12 / 12 / 12 / 10 | False Telegraph / True Telegraph / Delayed Imitation / Disappearance |
| `GazeOfAll_Reactions_v1.png` | 8×3 | 192×192 | 10 / 12 / 8 | Awareness Exposure / Final Confrontation / Audience Turn-Away |
| `EyeHazardTransitions_v1.png` | 8×5 | 192×192 | 12 / 12 / 14 / 16 / 12 | Open / Close / Beam Telegraph / Beam Discharge / Cluster Alarm |
| `CoverTransitions_v1.png` | 8×4 | 192×192 | 10 / 10 / 10 / 14 | Curtain Close / Curtain Open / Mask Shield / Breakable Cover |
| `TransitTransitions_v1.png` | 8×3 | 192×192 | 10 / 10 / 14 | Cage Lift / Iris Bridge / Chain Release |
| `AwarenessObjectTransitions_v1.png` | 8×4 | 192×192 | 10 / 10 / 10 / 12 | Shrine / Truth Lens / Memory Mirror / Observation Seal |
| `GazeArenaTransitions_v1.png` | 8×3 | 192×192 | 10 / 10 / 12 | Iris Door Close / Iris Door Open / Audience Barrier |
| `GazeCollectibleTransitions_v1.png` | 8×4 | 192×192 | 10 / 12 / 12 / 10 | Memory Shard / Watcher Token / Healing Mote / Map Fragment |
| `GazeCheckpointTransitions_v1.png` | 8×3 | 192×192 | 8 / 12 / 8 | Dormant / Activation / Restored Loop |
| `GazeAmbientMotion_v1.png` | 8×4 | 192×192 | 6 / 6 / 5 / 6 | Dust / Web and Chain / Distant Eyes / Floor Mist |
| `GazeSecondaryVFX_v1.png` | 8×4 | 192×192 | 16 / 14 / 12 / 14 | Hit / True-Sight Reveal / Gaze Buildup / Death Residue |

FPS 값은 행별 권장 시작점이다. Unity에서는 Idle·환경 행을 Loop Time으로 설정하고, 공격·피격·사망·획득 행은 루프를 끈다.

## 최종 프롬프트

### 1. Blind Pilgrim

```text
Create an exactly 8-column by 6-row side-view enemy sprite sheet for Blind Pilgrim:
a small blind robed pilgrim with a probing staff, sealed eyelids and violet-black cloth.
Rows: quiet idle, cautious staff-led walk, sound reaction telegraph, forward staff thrust and recovery,
compact hit recoil, irreversible collapse into cloth and dust.
```

### 2. Informing Mouth

```text
Create an exactly 8-column by 6-row side-view enemy sprite sheet for Informing Mouth:
a floating support creature combining a caged mouth, thin black frame and restrained magenta eye-light.
Rows: hover idle, lateral flight, inhaling scream telegraph, scream that activates gaze devices,
hit recoil, irreversible cage-and-mouth dissolution.
```

### 3. Hanging Audience

```text
Create an exactly 8-column by 6-row side-view enemy sprite sheet for Hanging Audience:
a ceiling cage containing pale audience masks and ragged violet cloth.
Rows: hanging idle, ceiling crawl and sway, projected shadow telegraph, sudden drop and recovery,
hit shudder, irreversible fall and break.
```

### 4. Faceless Judge

```text
Create an exactly 8-column by 6-row side-view enemy sprite sheet for Faceless Judge:
a heavy faceless gothic adjudicator with broad eyelid armor, long robe and stone gavel-limb.
Rows: imposing idle, slow grounded walk, raised-gavel telegraph, deliberate heavy strike and recovery,
guard-breaking hit recoil, irreversible kneel and collapse.
```

### 5. Iris Gatekeeper Combat

```text
Create an exactly 8-column by 7-row side-view boss sheet for Iris Gatekeeper:
a tall gothic iris-door guardian with eyelid armor, rotating violet iris core,
hooked stone limbs and teal gaze seams.
Rows: idle, iris sweep, eyelid close, charge judgment, dual gaze, hurt, death.
Every attack must show warning, active state and recovery.
```

### 6. Iris Gatekeeper Transitions

```text
Create an exactly 8-column by 3-row transition sheet for the same Iris Gatekeeper.
Rows: doorway entrance unfolding into the boss, half-health iris overload,
defeated shortcut gate opening. Keep the fixed bottom-center anchor.
```

### 7. Gaze of All Combat

```text
Create an exactly 8-column by 7-row region-boss sprite sheet for Gaze of All:
a floating gothic theater idol made from pale audience masks, a central hanging cage,
violet-black drapery, magenta false eyes and one hidden cyan-teal true eye.
Rows: idle, fixed gaze, rotating gaze, eye projectile, true strike, hurt, irreversible death.
```

### 8. Gaze of All Deceptions

```text
Create an exactly 8-column by 4-row sheet for the same Gaze of All.
Rows: many magenta masks create a false telegraph; false eyes dim and the cyan true eye appears;
a translucent delayed imitation repeats the prior attack; masks and curtains fold into an empty cage
and begin to return.
```

### 9. Gaze of All Reactions

```text
Create an exactly 8-column by 3-row reaction sheet for the same Gaze of All.
Rows: cyan awareness light exposes hidden seams and the true eye; masks spread into a final crown
while the cage opens; audience masks rotate away and close their eyes around an abandoned cage.
```

### 10. Eye Hazard Transitions

```text
Create an exactly 8-column by 5-row Gaze eye-hazard sheet.
Rows: sealed wall eye opening; alert eye closing into masonry; iris tightening and beam telegraph;
thin magenta beam discharge and particle breakup; embedded eye cluster waking in a wave and dimming.
```

### 11. Cover Transitions

```text
Create an exactly 8-column by 4-row Gaze cover-device sheet.
Rows: ragged theater curtain closing; the same curtain opening; audience-mask stone shield rotating
edge-on to face-on; cracked eye-carved cover taking an impact and collapsing into readable chunks.
```

### 12. Transit Transitions

```text
Create an exactly 8-column by 3-row Gaze transit-mechanism sheet.
Rows: empty suspended cage lift opening and rising; segmented eye-carved bridge unfurling from a coil;
ornate shortcut chain latch trembling, breaking free and leaving an unlocked end.
```

### 13. Awareness Object Transitions

```text
Create an exactly 8-column by 4-row Gaze awareness-object sheet.
Rows: eye-shaped shrine waking with a cyan pulse; suspended truth lens rotating and focusing;
black memory mirror rippling and showing an impossible eye; many-eyed observation seal opening in sequence.
```

### 14. Gaze Arena Transitions

```text
Create an exactly 8-column by 3-row Gaze arena-transition sheet.
Rows: broad iris door contracting shut; the same door dilating open;
low black railing and pale-mask audience barrier rising and igniting magenta.
```

### 15. Gaze Collectible Transitions

```text
Create an exactly 8-column by 4-row centered Gaze collectible sheet.
Rows: cyan memory lens shard rotating and collapsing into light; sealed watcher token blinking and dissolving;
pale healing mote rising and vanishing; black-violet map fragment unfolding and compressing into a rune.
```

### 16. Gaze Checkpoint Transitions

```text
Create an exactly 8-column by 3-row checkpoint sheet:
an empty low gothic observation chair fused with an eye lantern and folded theater curtains.
Rows: dormant breathing idle; cyan activation and outward rune illumination;
restored safe loop with motes, soft pulse and one calm blink.
```

### 17. Gaze Ambient Motion

```text
Create an exactly 8-column by 4-row seamless ambient Gaze sheet.
Rows: slowly drifting violet dust; hanging webs and chains swaying;
distant clusters of tiny audience eyes blinking asynchronously;
low cyan-violet floor mist curling horizontally. Keep contrast below gameplay silhouettes.
```

### 18. Gaze Secondary VFX

```text
Create an exactly 8-column by 4-row centered Gaze gameplay VFX sheet.
Rows: compact violet-white light-hit impact; cyan true-sight eye ripple revealing cracks;
magenta eye motes forming a pupil-shaped status warning; dark mask fragments and violet death dust
collapsing inward and evaporating without gore.
```

## Unity 임포트 메모

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Filter Mode: `Bilinear`
- Compression: `None` 또는 고품질
- Mesh Type: `Full Rect`
- Extrude Edges: `1`
- Pixels Per Unit: 실제 플레이어 높이와 맞춘 뒤 지역 전체에서 고정
- Slice: 위 표의 셀 크기로 `Grid by Cell Size`
- 공격 판정은 이미지 밝기나 알파에서 자동 추출하지 않고 애니메이션 이벤트로 연결
