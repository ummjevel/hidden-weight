# Room Backgrounds 4K Regeneration Design

## Goal

Regenerate all 45 room backgrounds as independent 3840×2160 images while
preserving the established design of every current room.

## Scope

- Residue: 12 main rooms and 3 secret rooms
- Gaze: 12 main rooms and 3 secret rooms
- Fracture: 12 main rooms and 3 secret rooms
- Total: 45 landscape backgrounds

Foreground overlays, parallax layers, and room motion layers remain excluded.

## Source and Generation Method

Each current room PNG is used as the sole reference/edit target for its matching
4K replacement. Every room is generated independently; no contact sheet,
multi-panel image, atlas, or crop is used as a generation source.

The regeneration must preserve:

- camera angle and 16:9 composition;
- major architecture and silhouettes;
- platforms, bridges, doors, cages, shafts, and landmark placement;
- chapter-specific palette, lighting, atmosphere, and visual identity;
- open gameplay/readability space in the current composition.

The regeneration may improve:

- edge definition and local contrast;
- material texture and architectural micro-detail;
- depth separation;
- clarity in mist, bloom, and distant structures;
- fine detail that was lost at 1672×941.

It must not add text, UI, borders, panels, watermarks, new characters, or a
different room layout.

## Output

Final files are exact 3840×2160 PNGs:

- `HiddenWeight/Assets/Art/Residue/Rooms4K/<room>.png`
- `HiddenWeight/Assets/Art/Gaze/Rooms4K/<room>.png`
- `HiddenWeight/Assets/Art/Fracture/Rooms4K/<room>.png`

Existing `Rooms` images remain intact until all replacements pass review.

## Validation and Integration

For every output:

1. Verify exact 3840×2160 dimensions.
2. Compare against its source for composition and landmark preservation.
3. Reject multi-panel layouts, text, watermarks, severe design drift, or
   misplaced focal elements.
4. Render it in the corresponding Unity room at 1920×1080.
5. Confirm viewport coverage, aspect-ratio preservation, and player readability.

After all 45 images pass, update the shared room background builder to resolve
`Rooms4K` and rebuild the three full-zone scenes. Keep the existing no-foreground
and no-motion rules.

## Failure Handling

A failed room is regenerated independently with a tighter preservation prompt.
No failed or unreviewed image replaces a currently referenced room background.
