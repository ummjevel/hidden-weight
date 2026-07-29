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
