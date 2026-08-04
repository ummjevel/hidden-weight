# Generic Foley Replacement Design

## Goal

Replace over-stylized, gong-like gameplay feedback with dry, ordinary foley
while preserving the game's signature music, ambience, rewind, healing, and
checkpoint identity.

## Root cause

The runtime folders currently mix semantically different generated cues. For
example, normal landing randomly includes heavy-landing clips, `EnemyHit`
includes a floor impact and an armored block, and boss folders mix different
boss identities. Many source prompts also request iron, glass, harmonics, and
resonance, which makes short feedback read as a struck instrument.

## Source and license

Use only the official Kenney `Impact Sounds` and `RPG Audio` packs. Both asset
pages identify the packs as Creative Commons CC0. Store the original selected
OGG sources and included license files under `/Users/ksh/Desktop/sound/CC0_Kenney`.

## Replacement scope

- Replace: attack swing, attack hit, jump, normal landing, walk/run footsteps,
  wall grab, player hurt/death, common enemy hit/death/block/telegraph, common
  boss telegraph/victory, platform crack/collapse, gate open/close, lift
  start/stop.
- Preserve: dash, wall-slide loop, respawn, heal, checkpoint, pickups, rewind,
  secret reveal, UI, room ambience, and BGM.
- Player hurt and death contain no voice.
- No selected source may contain `Bell`, `Glass`, `Plate`, `Pot`, or `Tin` in
  its filename. Metal impacts are allowed only for the explicit armored block
  and lift mechanism cues.

## Integration

Write the new audio into the existing runtime WAV paths so Unity `.meta` GUIDs,
`Resources.LoadAll`, and code call sites remain unchanged. Back up every
overwritten WAV under
`/Users/ksh/Desktop/sound/Backups/Unity_SFX_PreGenericFoley_2026-08-02`.

## Quality gates

- 48 kHz, 16-bit mono WAV.
- No sample reaches full scale.
- Player action clips are no longer than 1 second.
- Every overwritten file has a byte-for-byte backup.
- Generated replacement report records target, source layers, duration, peak,
  and license.

