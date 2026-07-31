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

## Freesound CC0 assets

Source pack: [Game Audio Starter Pack by Rob_Marion](https://freesound.org/people/Rob_Marion/packs/30567/)

License: [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/)

The following source sounds were converted to 48 kHz, 16-bit mono, trimmed,
faded, and peak-normalized for in-game use:

| Unity use | Freesound source |
|---|---|
| UI confirm | [542044](https://freesound.org/s/542044/) |
| Item pickup variations | [542030](https://freesound.org/s/542030/), [542029](https://freesound.org/s/542029/), [542028](https://freesound.org/s/542028/) |
| Heal and respawn placeholder | [541998](https://freesound.org/s/541998/) |
| Reward variations | [541982](https://freesound.org/s/541982/), [541981](https://freesound.org/s/541981/) |
| Basic melee hit and player hurt variations | [541986](https://freesound.org/s/541986/), [541990](https://freesound.org/s/541990/) |

Attribution is not required by CC0, but the source information is retained for
asset provenance.

## Signature sounds still awaiting dedicated creation

These intentionally keep the procedural placeholder until a sound matching the
game's identity is supplied:

- Player death
- Wall jump (temporarily reuses the normal jump sound)
- Checkpoint activation
- Story fragment
- Rewind and other emotion skills
