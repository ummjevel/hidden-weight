# HiddenWeight SFX sources

## Generic gameplay foley replacement (2026-08-02)

The 59 runtime WAV files in the cue groups below were replaced with short,
ordinary foley built from the official Kenney CC0 packs. Existing Unity paths,
filenames, and `.meta` files were preserved so the current `SfxCue` loading and
serialized references continue to work without code changes.

- [Kenney Impact Sounds](https://www.kenney.nl/assets/impact-sounds)
- [Kenney RPG Audio](https://www.kenney.nl/assets/rpg-audio)
- [Kenney asset license guidance](https://kenney.nl/support)

Replaced cue groups: `Attack`, `AttackHit`, `Jump`, `Land`, `FootstepWalk`,
`FootstepRun`, `WallGrab`, `Hurt`, `Death`, `EnemyHit`, `EnemyDeath`,
`EnemyBlock`, `EnemyTelegraph`, `BossTelegraph`, `BossVictory`,
`PlatformCrack`, `PlatformCollapse`, `GateOpen`, `GateClose`, `LiftStart`, and
`LiftStop`. Player hurt and death are intentionally non-vocal cloth/body foley.
The retained `Player_Land_Heavy_*` filenames now contain ordinary landing
variations; their names remain only to preserve Unity asset identity.

Signature and progression cues were not replaced: `Dash`, `WallSlide`,
`WallJump`, `Respawn`, `Heal`, `Checkpoint`, `Fragment`, `ItemPickup`, `Reward`,
`ShortcutOpen`, `RewindStart`, `RewindComplete`, `BossPhase`, `SecretReveal`,
UI, ambience, and BGM remain as before.

Build provenance and recovery files are stored outside the Unity project:

- Original Kenney packs: `/Users/ksh/Desktop/sound/CC0_Kenney`
- Staged masters and per-file report: `/Users/ksh/Desktop/sound/Generic_Foley_Replacement`
- Pre-replacement backup and hash manifest: `/Users/ksh/Desktop/sound/Backups/Unity_SFX_PreGenericFoley_2026-08-02`

For the replaced cue groups, this section supersedes the older source tables
below. Those tables remain as historical provenance for previous iterations and
for signature cues that were intentionally retained.

## User-created / AI-generated source assets

Edited for gameplay use as 48 kHz, 16-bit mono WAV files:

- `Player_Walk_Stone_01.wav`
- `Player_Run_Stone_01.wav`
- `Player_Jump_01.wav`
- `Player_Land_Normal_01.wav`
- `Player_Dash_01.wav`
- `Player_Wall_Grab_01.wav`
- `Player_Wall_Slide_Loop_01.wav`
- `Player_Attack_Swing_01.wav`
- `Player_Attack_Swing_02.wav`
- `Player_Attack_Swing_03.wav`

The edited masters are stored outside the Unity project in
`/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX`.

The three basic attack swings were generated with ElevenLabs Sound Effects.
Their preserved candidates are stored in
`/Users/ksh/Desktop/sound/attack_swing_round`. The runtime variations use
candidate 4, candidate 2 with its leading delay removed, and candidate 1.
Candidate 3 was rejected because its dense double energy peak and substantially
higher loudness made it read as a heavier attack. The selected files were
trimmed, faded, DC-corrected, level-matched, and exported as gameplay masters.

## Refined signature sources

Generated with ElevenLabs Sound Effects at prompt influence 55% with prompt
auto-improvement off. The full library — 133 items, four raw MP3 variations
each — is preserved outside the Unity project in `/Users/ksh/Desktop/sound/out`,
alongside `MANIFEST.md` listing every item, its loop flag, and its length.

Runtime clips were derived from that library: leading silence trimmed with a
3 ms margin, tail capped per cue so a clip never outlives the action that
triggered it, 3 ms fade-in and 25 ms fade-out, peak-normalized to -3 dBFS, and
exported as 48 kHz, 16-bit mono WAV. Where a cue draws several variations, the
takes with the tightest attack were selected.

| SfxCue | Source item | Runtime variations |
|---|---|---:|
| `AttackHit` | `SFX_PLAYER_ATTACK_HIT` | 4 |
| `Hurt` | `SFX_PLAYER_HURT` | 4 |
| `Death` | `SFX_PLAYER_DEATH` | 3 |
| `Respawn` | `SFX_PLAYER_RESPAWN` | 3 |
| `Heal` | `SFX_PLAYER_HEAL` | 3 |
| `WallJump` | `SFX_PLAYER_WALL_JUMP` | 3 |
| `WallGrab` | `SFX_PLAYER_WALL_CLING` (alternates with `Player_Wall_Grab_01`) | 3 |
| `Land` | `SFX_PLAYER_LAND_HEAVY` (alternates with `Player_Land_Normal_01`) | 2 |
| `Checkpoint` | `SFX_CHECKPOINT_ACTIVATE` | 3 |
| `Fragment` | `SFX_MEMORY_FRAGMENT_PICKUP` | 3 |
| `ItemPickup` | `SFX_CURRENCY_PICKUP` | 3 |
| `Reward` | `SFX_HEALTH_SHARD_PICKUP` | 3 |
| `ShortcutOpen` | `SFX_GATE_UNLOCK` | 3 |
| `RewindStart` | `SFX_REWIND_CHANNEL_START` | 3 |
| `RewindComplete` | `SFX_REWIND_COMPLETE` | 3 |
| `EnemyHit` | `SFX_WALKER_HIT`, `SFX_FINGER_IMPACT`, `SFX_HARDENED_BLOCK` | 4 |
| `EnemyDeath` | `SFX_WALKER_DEATH`, `SFX_FINGER_DEATH`, `SFX_CARRIER_DEATH` | 4 |
| `BossTelegraph` | `SFX_WRIST_SWEEP_TELEGRAPH`, `SFX_WRIST_CHARGE_TELEGRAPH`, `SFX_WRIST_DROP_WARNING`, `SFX_INSTRUCTOR_CHAIN_WARNING` | 4 |
| `BossPhase` | `SFX_INSTRUCTOR_PHASE_WARNING`, `SFX_INSTRUCTOR_PHASE_RUPTURE` | 2 |
| `BossVictory` | `SFX_INSTRUCTOR_DEATH`, `SFX_WRIST_DEATH` | 2 |
| `EnemyTelegraph` | `SFX_WALKER_TELEGRAPH`, `SFX_HARDENED_TELEGRAPH`, `SFX_CARRIER_CHARGE_TELEGRAPH` | 4 |
| `EnemyBlock` | `SFX_HARDENED_BLOCK` | 3 |
| `PlatformCrack` | `SFX_FLOOR_CRACK` | 3 |
| `PlatformCollapse` | `SFX_FLOOR_COLLAPSE` | 3 |
| `GateOpen` | `SFX_GATE_OPEN` | 2 |
| `GateClose` | `SFX_GATE_CLOSE` | 2 |
| `LiftStart` | `SFX_LIFT_START` | 2 |
| `LiftStop` | `SFX_LIFT_STOP` | 2 |
| `SecretReveal` | `SFX_SECRET_EYE_REVEAL`, `SFX_SECRET_EYE_OPEN` | 3 |
| `UiCancel` | `SFX_UI_CANCEL` | 2 |
| `UiPause` | `SFX_UI_PAUSE` | 2 |
| `UiUnpause` | `SFX_UI_UNPAUSE` | 2 |
| `UiMapOpen` | `SFX_UI_MAP_OPEN` | 2 |
| `UiMapClose` | `SFX_UI_MAP_CLOSE` | 2 |

## Room ambience

`Assets/Resources/Audio/Ambience` holds seven seamless 30-second loops from the
same library, converted to 48 kHz, 16-bit mono and scaled to -6 dBFS peak. They
are deliberately left untrimmed and unfaded so the loop points stay clean.

`ResidueAmbientAudio` picks one per room and crossfades over 1.4 seconds when the
room changes, on its own `AudioSource` pair so the BGM mute policy still holds.
Rooms are mapped from the space descriptions in `LEVEL_21_RESIDUE_ROOMS.md`:

| Ambience | Rooms |
|---|---|
| `EntryBridge` | R01 입구 경계, R02 애도교, R03 손바닥 광장, R07 갈비 곡선교 |
| `LowerRuins` | R04 매몰된 하층 폐허, R11 후회의 회랑 |
| `SecretRoom` | R05 되감기 성소 |
| `InsideFingers` | R06 손가락 내부 |
| `LiftShaft` | R08 상층 승강축 |
| `UpperTower` | R09 끊어진 상층 고가교, R10 손목 감시탑 |
| `Gallows` | R12 기억의 교수대 |

Rooms outside that table keep the procedural low bed, so other zones reusing the
component still get something rather than silence.

## Freesound CC0 assets

Source pack: [Game Audio Starter Pack by Rob_Marion](https://freesound.org/people/Rob_Marion/packs/30567/)

License: [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/)

The following source sounds were converted to 48 kHz, 16-bit mono, trimmed,
faded, and peak-normalized for in-game use:

| Unity use | Freesound source |
|---|---|
| UI confirm | [542044](https://freesound.org/s/542044/) |

`UiConfirm` is the only cue still using this pack. The heal, respawn, melee hit,
player hurt, item pickup, and reward placeholders drawn from it were removed once
the generated signature sources above replaced them.

Attribution is not required by CC0, but the source information is retained for
asset provenance.

## Signature sounds still awaiting dedicated creation

These intentionally keep the procedural placeholder until a sound matching the
game's identity is supplied:

- `Ability` — emotion skill activation. The Residue-specific rewind cues exist,
  but a shared activation sound covering hush, foresight, and awareness does not.

Cues still served by earlier one-off masters rather than the generated library:
`Jump`, `Dash`, `FootstepWalk`, `FootstepRun`, `WallSlide`, `UiConfirm`. The
footstep cues stay single-material because there is no ground-material lookup
yet — importing the metal and fibrous variants would mix surfaces at random.
