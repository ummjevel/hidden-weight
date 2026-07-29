# Chibi Player Asset Generation Record

## Identity lock

- Immutable character reference: `docs/chibi.png`
- Pose and timing references: the matching legacy runtime sheets under
  `HiddenWeight/Assets/Art/Residue/Gameplay`
- Preserve short white bob hair and ahoge, cyan eyes, cream oversized
  turtleneck dress, charcoal leggings, white boots, cyan gemstone and
  botanical ornaments.
- Adapt for gameplay with charcoal-lavender outlines, cool lavender shadows,
  restrained cyan glow, fixed right-facing side view, fixed scale and
  bottom-aligned registration.
- Do not add weapons. Attack frames use a pale white-lavender energy crescent.

## Production pipeline

1. Generate one chroma-key source per atlas with the built-in image generator.
2. Preserve each generated source in this directory.
3. Normalize to the exact legacy atlas dimensions without changing the grid.
4. Remove the uniform `#00ff00` background:

```bash
python "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py" \
  --input SOURCE.png --out RUNTIME.png --key-color '#00ff00' \
  --soft-matte --transparent-threshold 12 --opaque-threshold 220 \
  --despill --force
```

5. Import runtime PNGs as Sprite Multiple, PPU 32, Bilinear, Mipmap Off,
   Clamp, Uncompressed, Alpha Is Transparency, Bottom Center pivot.

## Generated sheets

Final prompts, source paths, runtime paths, visual audit notes and test results
are appended here as each atlas is completed.

### Player key poses

- Source: `Player_KeyPoses_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png`
- Grid: 4×2 at 1536×1024
- Prompt summary: redraw the immutable `chibi.png` identity over the legacy
  Idle, Walk, Run, Jump / Fall, Land, Attack, Dash layout; exact cream
  turtleneck costume, charcoal leggings, white boots and cyan ornaments;
  right-facing, bottom-aligned, one character per cell, attack crescent only
  in the attack cell, uniform `#00ff00` backdrop.
- Audit: eight isolated full-body poses, no crop or overlap, transparent
  runtime corners, identity and costume preserved.

### Player locomotion

- Source: `Player_Locomotion_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png`
- Grid: 8×3 at 2048×768; rows are Idle, Walk and Run.
- Prompt summary: preserve the approved chibi identity across 24 right-facing,
  bottom-aligned frames with a subtle idle loop, alternating walk contacts and
  a stronger forward-leaning run; no effects or scenery.

### Player aerial

- Source: `Player_Aerial_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png`
- Grid: 6×4 at 1536×1024; rows are Jump, AirMove, Fall and Land.
- Prompt summary: preserve scale and registration across takeoff, aerial
  movement, falling hair follow-through and landing recovery; final land pose
  returns to approved idle proportions.
- Audit: `contact-sheets/locomotion-loop-audit.jpg` compares loop boundaries
  and the Land-to-Idle transition against a common foot baseline.

### Player actions

- Source: `Player_Actions_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png`
- Grid: 6×2 at 2172×724; rows are Attack and Dash.
- Prompt summary: compact six-frame weaponless energy attack and six-frame
  low dash, with every character and effect held inside a square cell.
- Iteration note: the first draft used a 2:1 canvas and crossed cell
  boundaries. The accepted 3:1 regeneration adds 12% safe padding and keeps
  the crescent, particles, speed lines and dust inside their source cells.

### Player wall movement

- Source: `Player_Wall_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png`
- Grid: 6×2 at 2172×724; rows are WallCling and WallJump.
- Prompt summary: invisible-wall hand and foot contact in the upper loop,
  followed by compression, push, airborne extension and recovery in the lower
  row; no wall scenery is baked into the sprite sheet.

### Player reaction VFX

- Source: `PlayerVFX_Chibi_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png`
- Grid: 6×3 at 1536×1023; rows are Hit, Death and Respawn.
- Prompt summary: a compact white-lavender impact burst, gray-violet smoky
  dissolution and restrained cyan-white reconstruction. Each effect progresses
  left to right, preserves bottom-center registration and stays inside its
  256×341 runtime cell.

### Player Hush ability

- Source: `Player_Hush_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Player/Abilities/Player_Hush_v1.png`
- Grid: 6×3 at 1536×768; rows are HushBegin, HushMove and HushEnd.
- Prompt summary: contract from idle into an inward guarded crouch, move with
  small cautious steps, then expand back to idle.
- Audit correction: the generated HushMove row measured 78% of standing
  height. It was uniformly reduced and bottom-aligned; final maximum is 58.5%.

### Player Awareness ability

- Source: `Player_Awareness_Source.png`
- Runtime: `HiddenWeight/Assets/Art/Player/Abilities/Player_Awareness_v1.png`
- Grid: 6×3 at 1536×768; rows are AwarenessBegin, AwarenessLoop and
  AwarenessUnlock.
- Prompt summary: restrained eye and gemstone ignition, a pale delayed
  double-contour perception loop, then contour convergence and a calm frontal
  gaze in the final frame.
- Audit: every character, delayed contour and mote remains inside its 256×256
  cell.
