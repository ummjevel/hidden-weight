# 잔재 이미지 제작 100% 보완 세트

## 공통 규칙

- 모든 결과는 투명 RGBA PNG다.
- 생성 시 균일한 `#00ff00` 배경을 사용하고 soft matte·despill로 제거했다.
- 애니메이션 시트는 가로 8프레임이며 왼쪽에서 오른쪽으로 재생한다.
- 회갈색 석재, 먹색 철골, 탁한 앰버, 제한적인 남색·청백색 되감기 빛을 사용한다.
- 기존 잔재 자산을 덮어쓰지 않는다.

## 납품 규격

| 파일 | 격자 | 셀 크기 | 권장 FPS | 피벗 | 행 |
|---|---:|---:|---:|---|---|
| `ResidueEnemyProjectiles_v1.png` | 8×4 | 192×192 | 14 / 16 / 16 / 14 | Center | 석재 파편 / 손톱 궤적 / 돌진 잔상 / 지면 충격파 |
| `ResidueBossProjectiles_v1.png` | 8×5 | 192×192 | 14 / 14 / 16 / 14 / 12 | Center | 감시 파동 / 낙하 고리 / 기억침 / 되감기 구체 / 페이즈 파열 |
| `ResiduePlatformStates_v1.png` | 8×4 | 192×192 | 8 / 12 / 8 / 14 | Bottom Center | 균열 / 붕괴 / 파손 정착 / 되감기 복구 |
| `ResidueImpactVFX_v1.png` | 8×4 | 192×192 | 18 / 16 / 14 / 16 | Center | 근접 타격 / 벽 충돌 / 일반 착지 / 강한 충돌 |
| `ResidueForegroundMotion_v1.png` | 8×4 | 192×192 | 6 / 6 / 4 / 6 | Center | 사슬 / 철창과 천 / 거대 손가락 / 전경 먼지 |
| `ResidueBackgroundMotion_v1.png` | 8×4 | 192×192 | 5 / 5 / 3 / 4 | Center | 연기와 재 / 창문 / 거대 손 / 굳은 사람 군집 |
| `ResidueRoomTransitions_v1.png` | 8×4 | 192×192 | 10 / 10 / 12 / 10 | Bottom Center | 봉쇄 / 해제 / 지름길 / 비밀벽 |
| `ResidueUIIcons_v1.png` | 8×4 | 192×192 | 정적 | Center | 이동 / 상호작용 / 등장인물 / 진행 상태 |
| `ResidueStatusUI_v1.png` | 8×3 | 192×192 | 10 / 12 / 10 | Center | 되감기 / 위험 / 진행 반응 |

환경 모션은 Loop Time을 사용한다. 투사체, 충돌, 방 전환은 루프를 끈다. 발판 1행만 대기 루프로 사용할 수 있고 나머지는 상태 전환용이다.

## 최종 프롬프트

### ResidueEnemyProjectiles

```text
Production projectile sheet, exactly 8 columns by 4 rows.
Rows: walker stone splinter; hanging finger claw trail; mourning carrier amber charge trail;
hardened residue compressed ground shockwave.
Each row progresses through anticipation, active attack, impact and clean dissipation.
Match brown-gray stone, black rusted iron, dry tissue, muted amber memory light and faint navy shadow.
```

### ResidueBossProjectiles

```text
Production boss projectile sheet, exactly 8 columns by 5 rows.
Rows: Wrist Watcher horizontal surveillance wave; Wrist Watcher falling impact ring;
Memory Instructor memory needle; Memory Instructor rewind orb; boss phase-transition rupture.
Each row has readable warning, active, impact and dissipation frames.
```

### ResiduePlatformStates

```text
Production platform-state sheet, exactly 8 columns by 4 rows.
Use one consistent rusted black-iron and brown-gray stone platform with a fixed bottom-center anchor.
Rows: subtle shaking and cracking; progressive collapse; fully broken settling state;
pale-blue and muted-amber rewind reconstruction into the exact intact platform.
```

### ResidueImpactVFX

```text
Production centered impact VFX sheet, exactly 8 columns by 4 rows.
Rows: compact melee hit; stone wall collision; light landing dust; heavy landing or large-object impact.
Each effect starts compact, peaks near frames 3–5 and completely dissipates by frame 8.
```

### ResidueForegroundMotion

```text
Production seamless foreground sheet, exactly 8 columns by 4 rows.
Rows: close hanging chains swaying; iron cage and torn shroud swaying;
an enormous rigid finger silhouette passing along the extreme screen edge;
low charcoal dust, fragments and dim amber motes.
Keep dark and low contrast for gameplay readability.
```

### ResidueBackgroundMotion

```text
Production seamless distant-background sheet, exactly 8 columns by 4 rows.
Rows: distant smoke and ash; ruined tower windows blinking dim amber;
far colossal skeletal hand moving almost imperceptibly;
petrified human-like silhouettes making one synchronized micro-movement.
Keep soft edges, low contrast and distant scale.
```

### ResidueRoomTransitions

```text
Production room-transition mechanism sheet, exactly 8 columns by 4 rows.
Rows: chain-and-stone room seal closing; the same seal opening;
broken pulley and bridge rewinding into a shortcut;
secret wall made from petrified fingers separating into a passage.
Use a fixed bottom-center anchor and readable open/closed endpoints.
```

### ResidueUIIcons

```text
Atlas of exactly 32 distinct UI icons, exactly 8 columns by 4 rows.
Row 1: entrance, exit, upward route, downward route, door, locked door, shortcut, secret passage.
Row 2: checkpoint, healing, currency, memory fragment, health shard, rewind object, hazard, collapse platform.
Row 3: walker, hanging finger, mourning carrier, hardened residue, miniboss, region boss, NPC, lore record.
Row 4: undiscovered, discovered, completed, revisit, current position, objective, boss defeated, region complete.
No text, letters or numbers. Maintain identical padding and readable silhouettes at 32–64 pixels.
```

### ResidueStatusUI

```text
Production animated status UI sheet, exactly 8 columns by 3 rows.
Row 1: rewind energy charging, ready, activating and emptying.
Row 2: damage warning accumulating, reaching critical danger and clearing.
Row 3: memory acquired, boss warning and region-complete seal response.
Use compact circular emblems with muted amber and restrained pale-blue accents.
```

## Unity 임포트 권장값

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Filter Mode: `Bilinear`
- Compression: `None`
- Mip Maps: `Off`
- Wrap Mode: `Clamp`
- Slice: `Grid by Cell Size`, 192×192
- 공격체·VFX·UI: Center 피벗
- 발판·방 전환: Bottom Center 피벗
