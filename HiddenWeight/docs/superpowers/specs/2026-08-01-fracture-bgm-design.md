# Fracture BGM Asset Design

## Goal

Use the user-generated `Fracture_2026-08-01T093304.mp3` as the Fracture zone's
music without trimming, re-encoding, or changing its musical structure.

## Design

- Preserve the source MP3 bytes and import it as `Assets/Audio/Fracture_BGM.mp3`.
- Give the asset its own Unity GUID and the same streaming/import settings as
  `Residue_BGM.mp3`.
- Assign that GUID to `Zone_Fracture.asset`'s `bgm` field.
- Leave the procedural `AmbientAudioFactory` fallback unchanged; it will no
  longer be selected for Fracture because the zone now has an explicit clip.

## Verification

- Source and destination SHA-256 hashes must match.
- Unity audio metadata must identify a 44.1 kHz stereo MP3.
- `Zone_Fracture.asset` must reference the new audio asset GUID with audio
  file ID `8300000`.
- No other zone BGM assignment may change.
