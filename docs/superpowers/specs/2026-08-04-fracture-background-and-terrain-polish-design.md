# Fracture Background and Terrain Polish Design

## Goal

Restore the Fracture zone's pre-rebuild composition so the concept-art background
always fills the camera view, while polishing the continuous floor and wall art to
feel native to the pale ruined-garden atmosphere.

## Root Cause

The latest Fracture scene rebuild serialized `worldSize` into every room's
`CameraLockedRoomBackground`. Older Fracture scenes had the field unset, so their
background followed and filled the camera. With `worldSize` set, the background is
fixed to the room. Fracture's player-locked camera can look beyond the room bounds
near entrances and low platforms, exposing the camera's black clear color.

## Scope

- Apply to all Fracture rooms F01-F12 and FS1-FS3, including the full-zone scene.
- Do not change collision geometry, player movement, room bounds, camera zoom, or
  camera tracking behavior.
- Do not change Prologue, Residue, or Gaze background behavior.
- Preserve the continuous-run terrain system and dedicated moving/special-platform
  artwork.

## Background Design

Fracture backgrounds will use camera-follow mode again. At runtime, the background
is centered on the active camera and uniformly scaled to cover the complete
orthographic viewport at every supported aspect ratio. This removes black margins
without cropping the playable view or changing camera motion.

The existing per-room Fracture tint curve remains. It keeps foreground traversal
art readable while allowing the background palette to progress from bright
lavender-blue ruins to the darker final rooms.

The builder will explicitly choose camera-follow mode for Fracture rather than
depending on a missing serialized value. Other zones continue to use their current
room-fixed configuration.

## Terrain Art Design

The long surface middle remains visually continuous, but gains restrained detail:

- subtle lavender marble value variation instead of a flat solid strip;
- a soft cyan inner glow in the glass band, matched to the background water and
  windows;
- sparse, low-contrast seam and crystal accents at irregular intervals;
- stronger ornamental end caps only at actual platform ends;
- wall middles with vertical stone grain and faint edge lighting so tall walls read
  as architecture rather than blank rectangles.

Detail must stay quieter than the character and hazards. Repetition must not create
a short block-tile rhythm, and module joins must not show black or transparent
seams.

## Implementation Boundaries

`SingleRoomBackgroundBuilder` will expose or use an explicit sizing policy and the
Fracture builders will request camera-follow sizing. `CameraLockedRoomBackground`
will retain both supported modes: camera-follow for Fracture and world-size for
zones that deliberately use room-fixed composition.

The deterministic Fracture terrain generator remains the source of the v3 bitmap
modules. Generated PNGs and their Unity imports remain reproducible.

## Verification

- Add an EditMode contract test proving every generated Fracture room background
  is configured for camera-follow mode.
- Add or extend runtime tests proving a Fracture background covers the viewport at
  the left, right, top, and bottom edges at the game's window aspect ratio.
- Rebuild all 15 Fracture rooms and both Fracture zone scenes.
- Capture representative early, middle, late, and secret rooms from entrance and
  interior positions; confirm there are no black margins, clipped wall fragments,
  or obvious short-tile repetition.
- Run the targeted Fracture test suites and rebuild the macOS app to the Desktop.

