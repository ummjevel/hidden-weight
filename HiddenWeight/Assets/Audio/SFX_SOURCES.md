# HiddenWeight SFX sources

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

Selected and refined masters live outside the Unity project in
`/Users/ksh/Desktop/sound/Selected_Refined`. Each source folder holds one
`*_Selected.wav`, exported as 48 kHz, 16-bit mono.

| SfxCue | Refined source |
|---|---|
| `AttackHit` | `Player/SFX_PLAYER_ATTACK_HIT` |
| `WallJump` | `Player/SFX_PLAYER_WALL_JUMP` |
| `WallGrab` | `Player/SFX_PLAYER_WALL_CLING` (alternates with `Player_Wall_Grab_01`) |
| `Hurt` | `Player/SFX_PLAYER_HURT` |
| `Death` | `Player/SFX_PLAYER_DEATH` |
| `Respawn` | `Player/SFX_PLAYER_RESPAWN` |
| `Heal` | `Player/SFX_PLAYER_HEAL` |
| `Checkpoint` | `World/SFX_CHECKPOINT_ACTIVATE` |
| `Fragment` | `World/SFX_MEMORY_FRAGMENT_PICKUP` |
| `ShortcutOpen` | `World/SFX_GATE_UNLOCK` |
| `EnemyHit` | `Enemies/SFX_WALKER_HIT` |
| `EnemyDeath` | `Enemies/SFX_WALKER_DEATH` |
| `BossTelegraph` | `Bosses/SFX_WRIST_CHARGE_TELEGRAPH`, `SFX_WRIST_SWEEP_TELEGRAPH`, `SFX_WRIST_DROP_WARNING`, `SFX_INSTRUCTOR_CHAIN_WARNING` |
| `BossPhase` | `Bosses/SFX_INSTRUCTOR_PHASE_RUPTURE` |
| `BossVictory` | `Bosses/SFX_INSTRUCTOR_DEATH` |

`BossTelegraph` intentionally holds four takes so `ResolveSfx` alternates
between them; every other cue holds a single take.

## Freesound CC0 assets

Source pack: [Game Audio Starter Pack by Rob_Marion](https://freesound.org/people/Rob_Marion/packs/30567/)

License: [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/)

The following source sounds were converted to 48 kHz, 16-bit mono, trimmed,
faded, and peak-normalized for in-game use:

| Unity use | Freesound source |
|---|---|
| UI confirm | [542044](https://freesound.org/s/542044/) |
| Item pickup variations | [542030](https://freesound.org/s/542030/), [542029](https://freesound.org/s/542029/), [542028](https://freesound.org/s/542028/) |
| Reward variations | [541982](https://freesound.org/s/541982/), [541981](https://freesound.org/s/541981/) |

The heal, respawn, melee hit, and player hurt placeholders drawn from this pack
were removed once the refined signature sources above replaced them.

Attribution is not required by CC0, but the source information is retained for
asset provenance.

## Signature sounds still awaiting dedicated creation

These intentionally keep the procedural placeholder until a sound matching the
game's identity is supplied:

- `Ability` — emotion skill activation
- `RewindStart` and `RewindComplete`
