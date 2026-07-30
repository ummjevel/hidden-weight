# Room Backgrounds 4K Regeneration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Regenerate all 45 Unity room backgrounds independently at exact 3840×2160 while preserving each current room's composition, palette, landmarks, and gameplay-readable space.

**Architecture:** Treat each existing room PNG as the sole high-fidelity edit reference for one replacement image. Save generated results in chapter-specific `Rooms4K` folders, normalize them to exact 16:9 4K without stretching, validate every file and its visual similarity, then switch the shared background resolver and rebuild the three full-zone scenes.

**Tech Stack:** Built-in ImageGen, ImageMagick, Unity 6000.5.4f1, C#, Unity Test Framework

## Global Constraints

- Generate exactly 45 independent landscape images; never generate a contact sheet, atlas, grid, split panel, or multiple rooms in one image.
- Final files must be PNG at exactly 3840×2160.
- Preserve the source camera angle, composition, architecture, landmark placement, chapter palette, and open gameplay space.
- Do not add text, UI, borders, panels, watermarks, new characters, or a different room layout.
- Do not overwrite the current `Rooms` source images.
- Keep `BG_Far`, `BG_Mid`, `FG_Overlay`, `MotionBack`, and `MotionFront` absent.
- Use the built-in ImageGen tool once per distinct room image.

---

### Task 1: Add the 4K asset contract

**Files:**
- Create: `HiddenWeight/Assets/Tests/EditMode/RoomBackground4KTests.cs`
- Create: `HiddenWeight/Assets/Art/Residue/Rooms4K/`
- Create: `HiddenWeight/Assets/Art/Gaze/Rooms4K/`
- Create: `HiddenWeight/Assets/Art/Fracture/Rooms4K/`

**Interfaces:**
- Produces: exact room-name-to-file contract at `Assets/Art/<Chapter>/Rooms4K/<RoomName>.png`
- Consumes: Unity `TextureImporter` and the 45 room names already used by the full-zone scenes

- [ ] **Step 1: Write the failing file-count and dimension test**

Create a parameterized EditMode test with these chapter/name rules:

```csharp
static IEnumerable<string> Paths()
{
    for (int i = 1; i <= 12; i++)
    {
        yield return $"Assets/Art/Residue/Rooms4K/Room{i:00}.png";
        yield return $"Assets/Art/Gaze/Rooms4K/GazeRoom{i:00}.png";
        yield return $"Assets/Art/Fracture/Rooms4K/FractureRoom{i:00}.png";
    }
    for (int i = 1; i <= 3; i++)
    {
        yield return $"Assets/Art/Residue/Rooms4K/Secret{i:00}.png";
        yield return $"Assets/Art/Gaze/Rooms4K/GazeSecret{i:00}.png";
        yield return $"Assets/Art/Fracture/Rooms4K/FractureSecret{i:00}.png";
    }
}

[TestCaseSource(nameof(Paths))]
public void EveryRoomHasAnExact4KBackground(string path)
{
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    Assert.That(texture, Is.Not.Null, path);
    Assert.That(texture.width, Is.EqualTo(3840), path);
    Assert.That(texture.height, Is.EqualTo(2160), path);
}
```

- [ ] **Step 2: Run the test and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform EditMode \
  -testFilter HiddenWeight.Tests.RoomBackground4KTests \
  -testResults /tmp/room-backgrounds-4k-red.xml \
  -logFile /tmp/room-backgrounds-4k-red.log
```

Expected: 45 failures because the `Rooms4K` assets do not exist.

- [ ] **Step 3: Create the three output folders**

Create the three directories named in this task. Do not copy or upscale the old images as final results.

---

### Task 2: Regenerate Residue's 15 rooms

**Files:**
- Read: `docs/concept-art/generated/residue-rooms-v2/01-*.png` through `12-*.png`
- Read: `docs/concept-art/generated/residue-rooms-v2/S1-*.png` through `S3-*.png`
- Create: `HiddenWeight/Assets/Art/Residue/Rooms4K/Room01.png` through `Room12.png`
- Create: `HiddenWeight/Assets/Art/Residue/Rooms4K/Secret01.png` through `Secret03.png`

**Interfaces:**
- Consumes: one existing Residue source image per ImageGen edit call
- Produces: 15 independently regenerated Residue PNGs

- [ ] **Step 1: Generate one room per built-in ImageGen call**

For each source, use it as the edit target/reference and use this prompt, replacing `<room>` with the filename's room description:

```text
Use case: stylized-concept
Asset type: 4K 16:9 Unity room background
Primary request: Re-render this exact <room> environment as a sharper, high-detail production background.
Input image: the supplied image is the sole edit target and composition reference.
Style/medium: preserve the existing dark amber gothic ruin concept-art style.
Composition/framing: preserve the exact camera angle, silhouettes, architecture, platforms, bridges, doors, shafts, landmarks, negative space, and left/right traversal flow.
Lighting/mood: preserve the current lighting direction, darkness, amber highlights, depth, and atmosphere.
Constraints: one continuous room only; increase edge definition, local contrast, material texture, and distant detail; preserve every major structure and its placement; no layout redesign.
Avoid: multiple rooms, contact sheet, grid, panels, split screen, text, UI, border, watermark, characters, foreground overlay, blur, fog that erases geometry.
```

- [ ] **Step 2: Save non-destructively**

Copy each selected generated output from the built-in ImageGen result location to its exact `Rooms4K` filename. Never overwrite `Assets/Art/Residue/Rooms/*.png`.

- [ ] **Step 3: Normalize each selected image to exact 4K**

If a generated file is not already 3840×2160, preserve its full 16:9 composition and use high-quality Lanczos resizing:

```bash
magick <generated.png> \
  -filter Lanczos -resize 3840x2160! \
  -strip -define png:compression-level=6 \
  <Rooms4K/output.png>
```

Only use the forced resize when the generated result is already 16:9. Reject and regenerate non-16:9 results instead of stretching them.

- [ ] **Step 4: Validate the chapter**

Run:

```bash
magick identify -format '%f %wx%h\n' \
  HiddenWeight/Assets/Art/Residue/Rooms4K/*.png
```

Expected: 15 files, every line reports `3840x2160`.

---

### Task 3: Regenerate Gaze's 15 rooms

**Files:**
- Read: `docs/concept-art/generated/gaze-rooms-v1/01-*.png` through `12-*.png`
- Read: `docs/concept-art/generated/gaze-rooms-v1/S1-*.png` through `S3-*.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Rooms4K/GazeRoom01.png` through `GazeRoom12.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Rooms4K/GazeSecret01.png` through `GazeSecret03.png`

**Interfaces:**
- Consumes: one existing Gaze source image per ImageGen edit call
- Produces: 15 independently regenerated Gaze PNGs

- [ ] **Step 1: Generate one room per built-in ImageGen call**

Use the Task 2 procedure with this chapter-specific prompt:

```text
Use case: stylized-concept
Asset type: 4K 16:9 Unity room background
Primary request: Re-render this exact room as a sharper, high-detail production background.
Input image: the supplied image is the sole edit target and composition reference.
Style/medium: preserve the existing violet-black ocular gothic city concept-art style.
Composition/framing: preserve the exact camera angle, silhouettes, arches, cages, bridges, eye motifs, landmarks, negative space, and traversal flow.
Lighting/mood: preserve the current violet palette, cold blue accents, darkness, depth, and watched/uneasy atmosphere.
Constraints: one continuous room only; increase edge definition, local contrast, material texture, and distant detail; preserve every major structure and its placement; no layout redesign.
Avoid: multiple rooms, contact sheet, grid, panels, split screen, text, UI, border, watermark, characters, foreground overlay, blur, fog that erases geometry.
```

- [ ] **Step 2: Save non-destructively**

Copy each selected generated output from the built-in ImageGen result location
to its exact `Assets/Art/Gaze/Rooms4K/<GazeRoomName>.png` filename. Never
overwrite `Assets/Art/Gaze/Rooms/*.png`.

- [ ] **Step 3: Normalize each selected image to exact 4K**

Reject and regenerate any result that is not 16:9. For an accepted 16:9 result
that is not already 3840×2160, run:

```bash
magick <generated.png> \
  -filter Lanczos -resize 3840x2160! \
  -strip -define png:compression-level=6 \
  <Assets/Art/Gaze/Rooms4K/output.png>
```

- [ ] **Step 4: Validate the chapter**

Run:

```bash
magick identify -format '%f %wx%h\n' \
  HiddenWeight/Assets/Art/Gaze/Rooms4K/*.png
```

Expected: 15 files, every line reports `3840x2160`.

---

### Task 4: Regenerate Fracture's 15 rooms

**Files:**
- Read: `HiddenWeight/Assets/Art/Fracture/Rooms/FractureRoom01.png` through `FractureRoom12.png`
- Read: `HiddenWeight/Assets/Art/Fracture/Rooms/FractureSecret01.png` through `FractureSecret03.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Rooms4K/FractureRoom01.png` through `FractureRoom12.png`
- Create: `HiddenWeight/Assets/Art/Fracture/Rooms4K/FractureSecret01.png` through `FractureSecret03.png`

**Interfaces:**
- Consumes: one existing Fracture room image per ImageGen edit call
- Produces: 15 independently regenerated Fracture PNGs

- [ ] **Step 1: Generate one room per built-in ImageGen call**

Use this chapter-specific prompt:

```text
Use case: stylized-concept
Asset type: 4K 16:9 Unity room background
Primary request: Re-render this exact room as a crisp, high-detail production background while preserving its established design.
Input image: the supplied image is the sole edit target and composition reference.
Style/medium: preserve the existing luminous pastel future-ruin concept-art style with violet flowers, pale stone, cyan glass, and water.
Composition/framing: preserve the exact camera angle, silhouettes, arches, bridges, greenhouses, doors, landmarks, negative space, and traversal flow.
Lighting/mood: preserve the current daylight direction and dreamlike palette, but improve local contrast and edge separation so architecture remains readable.
Constraints: one continuous room only; reduce washed-out haze without changing the palette; increase material texture and distant detail; preserve every major structure and its placement; no layout redesign.
Avoid: multiple rooms, contact sheet, grid, panels, split screen, text, UI, border, watermark, characters, foreground overlay, overexposure, blur, white haze that erases geometry.
```

- [ ] **Step 2: Save non-destructively**

Copy each selected generated output from the built-in ImageGen result location
to its exact `Assets/Art/Fracture/Rooms4K/<FractureRoomName>.png` filename.
Never overwrite `Assets/Art/Fracture/Rooms/*.png`.

- [ ] **Step 3: Normalize each selected image to exact 4K**

Reject and regenerate any result that is not 16:9. For an accepted 16:9 result
that is not already 3840×2160, run:

```bash
magick <generated.png> \
  -filter Lanczos -resize 3840x2160! \
  -strip -define png:compression-level=6 \
  <Assets/Art/Fracture/Rooms4K/output.png>
```

- [ ] **Step 4: Validate the chapter**

Run:

```bash
magick identify -format '%f %wx%h\n' \
  HiddenWeight/Assets/Art/Fracture/Rooms4K/*.png
```

Expected: 15 files, every line reports `3840x2160`.

---

### Task 5: Visual similarity and artifact review

**Files:**
- Read: all 45 source images
- Read: all 45 `Rooms4K` outputs
- Create temporarily: `/tmp/hiddenweight-room4k-review/`

**Interfaces:**
- Consumes: source/output image pairs
- Produces: an approved set of 45 room images safe for Unity integration

- [ ] **Step 1: Create paired review sheets**

For each chapter, create source/output pairs at equal display size with ImageMagick labels. The sheet is review-only and must never be used as a Unity asset or ImageGen reference.

- [ ] **Step 2: Inspect every pair**

Reject a result if it contains any of:

- more than one room or a panel/grid layout;
- missing or moved major landmark;
- changed camera angle or traversal direction;
- new character, text, UI, border, or watermark;
- blur, block artifacts, stretched geometry, or severe palette drift.

- [ ] **Step 3: Regenerate failed rooms individually**

Repeat only the failed room's ImageGen edit with the same chapter prompt plus one targeted correction describing the observed drift.

- [ ] **Step 4: Re-run the exact dimension test**

Run `HiddenWeight.Tests.RoomBackground4KTests`.

Expected: 45/45 test cases pass.

---

### Task 6: Switch Unity to the approved 4K backgrounds

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs`
- Modify: `HiddenWeight/Assets/Tests/EditMode/ResidueRoomArtBuilderTests.cs`
- Modify: `HiddenWeight/Assets/Scenes/Zone_Residue_Full.unity`
- Modify: `HiddenWeight/Assets/Scenes/Zone_Gaze_Full.unity`
- Modify: `HiddenWeight/Assets/Scenes/Zone_Fracture_Full.unity`

**Interfaces:**
- Consumes: `Assets/Art/<Chapter>/Rooms4K/<room.name>.png`
- Produces: rebuilt full-zone scenes referencing exact 4K room sprites

- [ ] **Step 1: Change the failing path expectation**

Update the EditMode background builder test to assert:

```csharp
StringAssert.Contains(
    "/Rooms4K/",
    AssetDatabase.GetAssetPath(
        background.GetComponent<SpriteRenderer>().sprite));
```

- [ ] **Step 2: Run the builder test and verify RED**

Run `HiddenWeight.Tests.ResidueRoomArtBuilderTests`.

Expected: failure because the builder still resolves `/Rooms/`.

- [ ] **Step 3: Change the shared resolver**

In `SingleRoomBackgroundBuilder.Build`, change:

```csharp
string spritePath = $"{artRoot}/Rooms/{room.name}.png";
```

to:

```csharp
string spritePath = $"{artRoot}/Rooms4K/{room.name}.png";
```

- [ ] **Step 4: Rebuild all three full-zone scenes**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.RunResidueZone \
  -quit -logFile /tmp/build-residue-4k.log

/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.RunGazeZone \
  -quit -logFile /tmp/build-gaze-4k.log

/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.RunFractureZone \
  -quit -logFile /tmp/build-fracture-4k.log
```

- [ ] **Step 5: Run the complete verification suite**

Run all EditMode and PlayMode tests.

Expected:

- EditMode: zero failures.
- PlayMode: zero failures.
- 45 scenes' room objects reference `Rooms4K`.
- No foreground, split background, or motion layer names exist.

- [ ] **Step 6: Render the final 45-room audit**

Capture every room at 1920×1080 and inspect viewport coverage, aspect ratio,
sharpness, collision-element readability, and player visibility.
