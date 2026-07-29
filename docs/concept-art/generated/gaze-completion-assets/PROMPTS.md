# 응시 이미지 제작 100% 보완 세트

## 공통 규칙

- 결과는 투명 RGBA PNG이며 모든 셀은 192×192다.
- `#00ff00` 배경에서 생성 후 soft matte·despill로 제거했다.
- 저채도 보라·먹색·검은 철을 기본으로 사용한다.
- 기만·위험은 마젠타, 진실·해금은 청록으로 구분한다.
- 기존 응시 이미지를 덮어쓰지 않는다.

## 납품 규격

| 파일 | 격자 | 권장 FPS | 피벗 | 행 |
|---|---:|---:|---|---|
| `GazeEnemyProjectiles_v1.png` | 8×4 | 14 / 16 / 12 / 14 | Center | 소리 파문 / 비명탄 / 시선 그림자 / 판결 참격 |
| `GazeBossProjectiles_v1.png` | 8×5 | 16 / 14 / 14 / 16 / 14 | Center | 스캔 빔 / 눈꺼풀 파편 / 사슬 채찍 / 가짜 눈탄 / 진실 타격 |
| `GazePlatformStates_v1.png` | 8×4 | 8 / 12 / 6 / 14 | Bottom Center | 감시 반응 / 해체 / 빈 상태 / 진실 복구 |
| `GazeImpactVFX_v1.png` | 8×4 | 18 / 16 / 14 / 16 | Center | 기본 타격 / 빔 화상 / 착지 / 방어 파괴 |
| `GazeForegroundMotion_v1.png` | 8×4 | 6 / 6 / 4 / 6 | Center | 사슬·케이지 / 커튼 / 관객 가면 / 안개·눈 |
| `GazeBackgroundMotion_v1.png` | 8×4 | 4 / 5 / 5 / 4 | Center | 하늘 홍채 / 창문 눈 / 케이지 / 관객 군집 |
| `GazeRoomTransitions_v1.png` | 8×4 | 10 / 10 / 12 / 10 | Bottom Center | 봉쇄 / 해제 / 지름길 / 비밀 통로 |
| `GazeUIIcons_v1.png` | 8×4 | 정적 | Center | 이동 / 상호작용 / 등장인물 / 진행 |
| `GazeStatusUI_v1.png` | 8×3 | 10 / 12 / 10 | Center | 인지·진실 시야 / 발각·주시 / 진행 반응 |

전경·배경 모션은 Loop Time을 사용한다. 공격체·충돌·방 전환은 루프를 끈다.

## 최종 프롬프트

### GazeEnemyProjectiles

```text
Exactly 8 columns by 4 rows.
Rows: Blind Pilgrim sound ripple; Informing Mouth scream bolt;
Hanging Audience gaze-shadow marker; Faceless Judge verdict slash.
Each row shows warning, active state, impact and clean dissipation.
```

### GazeBossProjectiles

```text
Exactly 8 columns by 5 rows.
Rows: Iris Gatekeeper scan beam; eyelid shard rain; cage-chain lash;
Gaze of All false-eye projectile; true cyan revelation strike.
Use magenta false danger and cyan truth signals.
```

### GazePlatformStates

```text
Exactly 8 columns by 4 rows using one eye-carved violet-black platform.
Rows: stable platform reacting to observation; watched platform disassembling;
fully dissolved empty state; cyan true-sight reconstruction.
Keep a fixed bottom-center anchor.
```

### GazeImpactVFX

```text
Exactly 8 columns by 4 rows.
Rows: basic violet hit; gaze-beam scorch; light landing dust;
mask-and-eyelid guard break.
Each effect peaks near frames 3–5 and disappears by frame eight.
```

### GazeForegroundMotion

```text
Exactly 8 columns by 4 seamless rows.
Rows: close chains and empty cage; ragged theater curtain;
audience mask entering the screen edge; low violet mist and tiny blinking eyes.
Keep dark and below gameplay contrast.
```

### GazeBackgroundMotion

```text
Exactly 8 columns by 4 seamless rows.
Rows: colossal sky iris rotating; distant window-eyes blinking;
hanging cages swaying; audience silhouettes turning their heads together.
Keep soft distant edges and low contrast.
```

### GazeRoomTransitions

```text
Exactly 8 columns by 4 rows.
Rows: iris door sealing; iris door opening; cage-and-bridge shortcut opening;
mirror-and-curtain secret passage revealing.
Use fixed bottom-center anchors and readable endpoints.
```

### GazeUIIcons

```text
Exactly 8 columns by 4 rows of 32 distinct icons.
Row 1: entrance, exit, up, down, door, locked door, shortcut, secret passage.
Row 2: checkpoint, healing, currency, memory, awareness, true sight, gaze hazard, cover.
Row 3: four Gaze enemies, miniboss, region boss, NPC, lore record.
Row 4: undiscovered, discovered, completed, revisit, current position, objective,
boss defeated, region complete.
No text, letters or numbers.
```

### GazeStatusUI

```text
Exactly 8 columns by 3 rows.
Rows: awareness and true-sight charge/use; detection and gaze buildup/clear;
memory acquired, boss warning and region complete.
Use compact circular emblems with magenta danger and cyan truth signals.
```

## Unity 임포트 권장값

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Filter Mode: `Bilinear`
- Compression: `None`
- Mip Maps: `Off`
- Wrap Mode: `Clamp`
- Slice: `Grid by Cell Size`, 192×192
- 공격체·VFX·UI: Center
- 발판·방 전환: Bottom Center
