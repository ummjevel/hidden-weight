# 잔재 애니메이션 스프라이트 생성 기록

## 공통 기준

- Built-in ImageGen으로 기존 잔재 시트를 정체성·재질 참조로 사용했다.
- 생성 원본은 균일한 `#00ff00` 배경으로 만들고 로컬 크로마키 제거 후 RGBA PNG로 저장했다.
- 검은 철골, 회갈색 석재, 앰버 발광, 제한적인 남보라 기억 에너지를 공통 팔레트로 사용했다.
- 텍스트, 번호, 셀 선, 워터마크, 그림자, 배경 장면은 제외했다.
- 기존 파일은 보존하고 신규 파일만 추가했다.

## 파일별 규격

| 파일 | 격자 | 셀 크기 | 행 | 권장 FPS |
| --- | ---: | ---: | --- | --- |
| `ResidueWalker_v2.png` | 8×6 | 181×181 | Idle / Move / Telegraph / Attack / Hit / Death | 8 / 12 / 12 / 14 / 16 / 12 |
| `HangingFinger_v2.png` | 8×6 | 181×181 | Hang / Crawl / Telegraph / Drop / Hit / Death | 6 / 10 / 10 / 14 / 16 / 12 |
| `MourningCarrier_v2.png` | 8×6 | 202×161 | Idle / Move / Telegraph / Charge / Hit / Death | 8 / 12 / 12 / 16 / 16 / 12 |
| `HardenedResidue_v2.png` | 8×6 | 181×181 | Idle / Move / Telegraph / Heavy Attack / Block Break / Death | 6 / 10 / 10 / 12 / 14 / 10 |
| `WristWatcher_Combat_v2.png` | 8×7 | 192×192 | Idle / Sweep / Charge / Impact / Drop / Hurt / Death | 8 / 14 / 16 / 12 / 14 / 16 / 10 |
| `WristWatcher_Transitions_v1.png` | 8×3 | 192×341 | Entrance / Phase / Device Interaction | 10 / 12 / 12 |
| `MemoryInstructor_Attacks_v1.png` | 8×4 | 222×222 | Blade Sweep / Hook Pull / Chain Slam / Recover | 14 / 14 / 12 / 10 |
| `MemoryInstructor_Reactions_v1.png` | 8×3 | 222×296 | Hurt / Phase Rupture / Death | 16 / 12 / 10 |
| `MemoryInstructor_CoreHalo_v1.png` | 8×3 | 248×264 | Halo / Core Pulse / Overload | 8 / 10 / 14 |
| `HazardTransitions_v1.png` | 8×5 | 198×198 | Spike / Tendril / Crusher / Collapse / Debris | 12 / 12 / 10 / 10 / 12 |
| `RewindObjectTransitions_v1.png` | 8×4 | 222×222 | Platform / Chain Bridge / Lift / Pulley | 12 / 12 / 10 / 12 |
| `BossArenaTransitions_v1.png` | 8×3 | 215×305 | Arena Lock / Safe Platform / Seal Rupture | 12 / 12 / 14 |
| `CollectibleTransitions_v1.png` | 8×5 | 202×194 | Currency / Healing / Health / Memory / Rewind Core | 16 / 14 / 14 / 12 / 14 |
| `CheckpointTransitions_v1.png` | 8×3 | 192×341 | Activate / Heal / Respawn | 12 / 14 / 12 |
| `AmbientMotion_v1.png` | 8×4 | 202×242 | Chain / Cloth / Ash / Fog | 6 / 8 / 8 / 6 |
| `SecondaryGameplayVFX_v1.png` | 8×4 | 209×235 | Heavy Hit / Guard Break / Enemy Dissolve / Boss Burst | 18 / 16 / 14 / 14 |

## 생성 프롬프트

### 일반 적 공통

```text
Use case: stylized-concept.
Asset type: production-ready 2D side-scrolling game animation sprite sheet.
Preserve the exact referenced enemy anatomy, proportions, black-iron and ash-bone material, restrained amber glow and side view.
Create an 8-column by 6-row sheet: idle, locomotion, telegraph, attack and recovery, hit, irreversible death.
Lock the bottom-center contact point and keep equal invisible cells.
Perfectly flat solid #00ff00 chroma-key background.
No text, numbers, grid lines, borders, watermark, scenery, extra creatures, anatomy drift or camera motion.
```

적별로 보행, 천장 매달림·낙하, 돌진, 방어·강공 동작을 대응 행에 지정했다.

### 손목 감시자

```text
Preserve the exact referenced Wrist Watcher with its tall skeletal tower body, blade arms, caged head and amber core.
Combat rows: idle, sweep, charge, impact-stun, drop, hurt, death.
Transition rows: entrance unfolding, phase overload, arena-device interaction.
Every attack must show anticipation, active impact and recovery.
Flat #00ff00 background; no text, grid, scenery or identity drift.
```

### 기억의 교수자

```text
Preserve the exact modular Memory Instructor: anchored gallows torso, caged head, blade arm, hook arm, chains, halo and amber/indigo core.
Keep the torso anchored and rotate limbs around consistent sockets.
Create blade sweep, hook pull, chain slam, recovery, hurt, phase rupture, death, halo rotation, core pulse and overload frames.
Flat #00ff00 background; no text, grid, scenery, extra subjects or anatomy drift.
```

### 위험물·장치

```text
Match the referenced Residue black iron, ash stone, chains, amber mechanisms and indigo rewind traces.
Hazards show idle, warning, activation, active hold and recovery.
Rewind objects show broken debris reversing into an intact object.
Boss arena objects show lock, safe-platform restoration and seal rupture.
Keep the physical anchor fixed. Flat #00ff00 background; no text, grid or scenery.
```

### 아이템·체크포인트

```text
Preserve the exact referenced collectibles and checkpoint shrine.
Collectibles lift, compress into light, stream toward the player and disappear by the final frame.
Checkpoint rows show first activation, healing pulse and respawn release.
Keep the shrine base fixed. Flat #00ff00 background; no player, text, grid or scenery.
```

### 환경·보조 VFX

```text
Create seamless loops for hanging chains, torn cloth, falling ash and low indigo fog.
Create one-shot effects for heavy hit, guard break, enemy dissolve and boss phase burst.
Effects start compact or empty, peak near the middle and dissipate to empty.
Keep motion subtle enough not to obscure gameplay.
Flat #00ff00 background; no text, grid, scenery or camera motion.
```

## 후처리

크로마키 제거:

```bash
python /Users/ksh/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py \
  --input source.png --out final.png \
  --auto-key border --soft-matte \
  --transparent-threshold 12 --opaque-threshold 220 --despill
```

생성기가 요청보다 많은 열을 출력한 `HangingFinger_v2`,
`WristWatcher_Combat_v2`, `MemoryInstructor_Attacks_v1`은 각 행에서
동작 진행을 대표하는 8프레임을 선택해 동일 셀 규격으로 재배열했다.

