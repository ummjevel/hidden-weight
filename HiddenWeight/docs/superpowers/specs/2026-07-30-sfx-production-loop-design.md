# HiddenWeight SFX Production Loop Design

## Goal

Produce and integrate one gameplay sound function at a time. Each round starts
with an ElevenLabs prompt, preserves every generated source, selects and edits
the strongest candidates, integrates the result into Unity, and verifies the
game event before moving to the next function.

## Working directories

- Generated source inbox: `/Users/ksh/Desktop/sound`
- Edited masters: `/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX`
- Unity runtime assets:
  `Assets/Resources/Audio/SFX/<SfxCue>`
- Source and license record: `Assets/Audio/SFX_SOURCES.md`

Generated source files are never overwritten. Only selected copies are edited.

## One-function production cycle

1. Codex provides one ElevenLabs prompt with duration, looping, prompt
   influence, rejection criteria, and target in-game length.
2. The user generates approximately four candidates and places them in the
   source inbox.
3. Codex detects only the new candidates and compares response delay, duration,
   unwanted content, noise, tonal fit, and gameplay readability.
4. Codex selects one to three useful variations. Ambiguous or unsuitable
   results are rejected rather than forced into the game.
5. Selected audio is trimmed, faded, peak-normalized, and converted to 48 kHz,
   16-bit mono WAV. Seamless loops remain loops and receive loop-boundary
   verification.
6. Edited masters receive stable descriptive names and are stored in the master
   directory.
7. Copies are placed in the matching Unity `Resources/Audio/SFX/<SfxCue>`
   directory. Multiple files in a cue directory become non-repeating random
   variations.
8. The corresponding gameplay event and AudioManager behavior are connected or
   updated only when necessary.
9. Focused Unity tests and relevant regression tests are run before the round is
   marked complete.
10. Codex reports the selected files and immediately provides the next
    function's prompt.

## Audio standards

- One-shots: minimal leading silence and no unrelated impact, voice, music, or
  ambience.
- Export: WAV, 48 kHz, 16-bit, mono.
- Default peak target: approximately -3 dBFS.
- Loop files: genuinely seamless and free from start/end transients.
- Variations: preserve a common sonic identity while changing only timing,
  texture, or intensity slightly.
- Signature sounds use generated custom audio. Generic utility sounds may use
  verified CC0 sources.

## Safety and provenance

- Freesound downloads are limited to free CC0 originals.
- Any checkout, paid bundle, donation requirement, subscription prompt, or
  possible charge stops the task immediately and is reported to the user.
- Every external source is recorded in `SFX_SOURCES.md`.
- Existing unrelated Unity and Git changes are preserved.

## Production order

The initial order is:

1. Basic attack swing, three variations
2. Wall jump, two variations
3. Player death, two variations
4. Checkpoint activation
5. Story fragment collection
6. Dedicated player respawn
7. Rewind skill set
8. Hush skill set
9. Foresight skill set
10. Awareness skill set

The order may change if a newly implemented gameplay event becomes more urgent.

## First round

The first completed round will be the basic attack swing. It must be a fast
single melee gesture without an embedded hit, footstep, voice, music, weapon
clang, heavy attack, combo, or laser-like effect. Three compatible variations
will be targeted so AudioManager can prevent immediate repetition.
