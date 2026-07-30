# Basic Attack Swing SFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate, select, edit, and integrate three compatible basic attack swing variations that replace the current procedural `SfxCue.Attack` placeholder.

**Architecture:** ElevenLabs produces four source candidates in the desktop sound inbox. The best three are preserved as edited masters, copied into the existing Resources cue directory, and loaded automatically by `AudioManager.ResolveSfx(SfxCue.Attack)` without changing the attack code.

**Tech Stack:** ElevenLabs Sound Effects, macOS Core Audio tools, WAV PCM, Unity 6000.5.4f1, Unity Test Framework

## Global Constraints

- Generated sources are stored in `/Users/ksh/Desktop/sound` and never overwritten.
- Edited masters are stored in `/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX`.
- Runtime clips are WAV, 48 kHz, 16-bit, mono.
- Default peak target is approximately -3 dBFS.
- The attack swing contains no embedded hit, footstep, voice, music, ambience, weapon clang, heavy attack, combo, or laser-like effect.
- Any paid plan, credit purchase, checkout, subscription, or possible charge stops the task immediately and is reported to the user.
- Existing unrelated Unity and Git changes are preserved.

---

### Task 1: Generate four attack swing candidates

**Files:**
- Create externally: `/Users/ksh/Desktop/sound/Isolated_fast_single_*.mp3`
- Preserve: every generated source file

**Interfaces:**
- Consumes: the approved ElevenLabs prompt and generation settings below
- Produces: four new MP3 candidates in `/Users/ksh/Desktop/sound`

- [ ] **Step 1: Open ElevenLabs Sound Effects and verify login**

Open the Sound Effects generation page. If login is required, pause while the
user signs in. Do not select any upgrade, paid plan, credit purchase, or
checkout action.

- [ ] **Step 2: Configure generation**

Use:

```text
Duration: 0.4 seconds
Looping: OFF
Prompt Influence: 55%
```

- [ ] **Step 3: Generate four candidates**

Prompt:

```text
Isolated fast single basic melee swing of a small cloaked character, starting instantly with a tight cloth snap and a short clean air cut, followed by a faint midnight-violet granular shadow trail that collapses immediately. Agile dark gothic 2D action game sound, light and responsive, one swing only, no impact, no footsteps, no landing, no voice, no music, no ambience, no metal clang, no sword ring, no heavy attack, no combo, no magic explosion, no laser.
```

- [ ] **Step 4: Download the generated candidates**

Download the four original candidates into `/Users/ksh/Desktop/sound`. Stop and
report immediately if downloading requires payment, purchasing credits, or
changing to a paid plan.

---

### Task 2: Select and edit three compatible variations

**Files:**
- Read: `/Users/ksh/Desktop/sound/Isolated_fast_single_*.mp3`
- Create:
  - `/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX/Player_Attack_Swing_01.wav`
  - `/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX/Player_Attack_Swing_02.wav`
  - `/Users/ksh/Desktop/sound/HiddenWeight_Unity_SFX/Player_Attack_Swing_03.wav`

**Interfaces:**
- Consumes: the four new ElevenLabs MP3 candidates
- Produces: three edited 48 kHz, 16-bit mono WAV masters

- [ ] **Step 1: Inventory only newly generated candidates**

Compare the current sound inbox against the pre-generation inventory and select
files whose creation time and names belong to this generation round.

- [ ] **Step 2: Reject unsuitable candidates**

Reject a candidate when it has any of the following:

```text
audible leading delay
more than one swing
embedded impact
metallic sword ring
heavy or oversized weapon character
laser or teleport character
music, ambience, voice, footstep, or landing
tail long enough to mask a 0.35-second attack cooldown
```

- [ ] **Step 3: Rank the remaining candidates**

Rank by immediate response, clear single gesture, compatibility with the current
dark cloth-and-shadow movement sounds, and ability to remain distinct from the
separate `AttackHit` cue.

- [ ] **Step 4: Convert and edit the best three**

For each selected candidate:

```text
remove leading silence while retaining a 2-5 ms safety margin
retain one complete swing
cap practical length at 0.25-0.40 seconds
apply a 3-5 ms fade-in if the cut clicks
apply a 20-35 ms fade-out
convert to 48 kHz, 16-bit, mono PCM WAV
peak-normalize to approximately -3 dBFS
```

- [ ] **Step 5: Verify the edited masters**

Confirm each master is mono, 48 kHz, 16-bit, between 0.25 and 0.40 seconds, and
has no clipped samples or unintended silence.

---

### Task 3: Integrate the attack variations and verify gameplay loading

**Files:**
- Create:
  - `Assets/Resources/Audio/SFX/Attack/Player_Attack_Swing_01.wav`
  - `Assets/Resources/Audio/SFX/Attack/Player_Attack_Swing_02.wav`
  - `Assets/Resources/Audio/SFX/Attack/Player_Attack_Swing_03.wav`
- Modify: `Assets/Audio/SFX_SOURCES.md`
- Modify: `Assets/Tests/EditMode/AudioManagerSfxTests.cs`
- Verify: `Assets/Scripts/Player/PlayerAttack.cs`
- Verify: `Assets/Scripts/Core/AudioManager.cs`

**Interfaces:**
- Consumes: three edited master WAV files
- Produces: three non-repeating runtime variations returned by `AudioManager.ResolveSfx(SfxCue.Attack)`

- [ ] **Step 1: Write the failing asset-resolution test**

Add:

```csharp
[Test]
public void ResolveSfx_UsesThreeImportedAttackSwingVariations()
{
    var resolved = new HashSet<AudioClip>();
    for (int i = 0; i < 6; i++)
        resolved.Add(_audio.ResolveSfx(SfxCue.Attack));

    Assert.That(resolved.Count, Is.EqualTo(3));
    foreach (var clip in resolved)
        Assert.That(clip.name, Does.StartWith("Player_Attack_Swing_"));
}
```

Add `using System.Collections.Generic;` to the test file.

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "/Users/ksh/Desktop/NHN HACKERton/HiddenWeight" \
  -runTests -testPlatform EditMode \
  -testFilter "HiddenWeight.Tests.AudioManagerSfxTests.ResolveSfx_UsesThreeImportedAttackSwingVariations" \
  -testResults /tmp/hiddenweight-attack-sfx-red.xml \
  -logFile /tmp/hiddenweight-attack-sfx-red.log
```

Expected: FAIL because the `Attack` Resources directory does not yet contain
the three imported clips.

- [ ] **Step 3: Copy the three edited masters into Unity**

Create `Assets/Resources/Audio/SFX/Attack` and copy the three WAV masters with
their stable names. Allow Unity to generate and retain the corresponding
`.meta` files.

- [ ] **Step 4: Record the generated asset provenance**

Add the three attack swing filenames to `Assets/Audio/SFX_SOURCES.md` under the
user-created or AI-generated source section. Record that ElevenLabs generated
the preserved source candidates and that the runtime files are edited masters.

- [ ] **Step 5: Run the focused test and verify it passes**

Run the command from Step 2 with result and log paths ending in `-green`.

Expected: PASS, resolving exactly three imported attack swing clips.

- [ ] **Step 6: Run relevant regression tests**

Run:

```text
All EditMode tests
HiddenWeight.Tests.AttackSanityTests
```

Expected: every selected test passes.

- [ ] **Step 7: Report the completed round**

Report which generated candidates were selected, the final durations, the Unity
destination, the test results, and any rejected candidate with a concise
reason. Then provide the wall-jump generation prompt as the next round.
