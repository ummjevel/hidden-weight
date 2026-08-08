# Fracture Background and Terrain Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore full-screen camera-follow backgrounds throughout Fracture and polish its continuous floor/wall modules without changing gameplay geometry or camera behavior.

**Architecture:** `SingleRoomBackgroundBuilder` gets an explicit background sizing policy; Fracture builders request camera-follow while other zones keep room-fixed backgrounds. The deterministic Pillow generator remains the only source for the v3 traversal bitmaps, and existing runtime continuous-run rendering consumes the regenerated modules unchanged.

**Tech Stack:** Unity 6000.5.4f1, C#, NUnit/Unity Test Framework, Python 3 + Pillow, macOS standalone build.

## Global Constraints

- Apply to F01-F12 and FS1-FS3 plus `Zone_Fracture` and `Zone_Fracture_Full`.
- Do not change collision geometry, player movement, room bounds, camera zoom, or camera tracking.
- Do not change Prologue, Residue, or Gaze background sizing.
- Preserve continuous-run terrain and dedicated moving/special-platform artwork.
- Never expose black viewport margins or transparent module seams.

---

### Task 1: Explicit Fracture Background Sizing Policy

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs`
- Modify: `HiddenWeight/Assets/Scripts/Editor/FractureZoneBuilder.cs`
- Test: `HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs`

**Interfaces:**
- Produces: `SingleRoomBackgroundBuilder.BackgroundSizing` enum with `RoomFixed` and `CameraFollow` values.
- Produces: `Build(Room room, string artRoot, BackgroundSizing sizing = BackgroundSizing.RoomFixed)`.
- Consumes: `CameraLockedRoomBackground.ConfigureWorldSize(Vector2)` only when `sizing == RoomFixed`.

- [ ] **Step 1: Write the failing builder-policy test**

Add an EditMode test that opens `Room_Fracture_F01.unity`, finds its
`CameraLockedRoomBackground`, serializes it, and asserts `worldSize == Vector2.zero`.
Also open one Gaze room and assert both world-size components are positive.

```csharp
[Test]
public void Fracture_배경은_카메라추적이고_Gaze는_방고정이다()
{
    Scene fracture = EditorSceneManager.OpenScene("Assets/Scenes/Room_Fracture_F01.unity");
    var fractureBg = Object.FindFirstObjectByType<CameraLockedRoomBackground>();
    Vector2 fractureSize = new SerializedObject(fractureBg)
        .FindProperty("worldSize").vector2Value;
    Assert.AreEqual(Vector2.zero, fractureSize);

    Scene gaze = EditorSceneManager.OpenScene("Assets/Scenes/Room_Gaze_G01.unity");
    var gazeBg = Object.FindFirstObjectByType<CameraLockedRoomBackground>();
    Vector2 gazeSize = new SerializedObject(gazeBg)
        .FindProperty("worldSize").vector2Value;
    Assert.Greater(gazeSize.x, 0f);
    Assert.Greater(gazeSize.y, 0f);
}
```

- [ ] **Step 2: Run the test and verify RED**

Run the Unity EditMode filter for `FractureTraversalAssetTests`.
Expected: the Fracture assertion fails because its serialized `worldSize` is `(26, 14)`.

- [ ] **Step 3: Implement the explicit policy**

Add the enum and optional parameter in `SingleRoomBackgroundBuilder`. Wrap the
existing `ConfigureWorldSize` call:

```csharp
public enum BackgroundSizing { RoomFixed, CameraFollow }

public static void Build(Room room, string artRoot,
    BackgroundSizing sizing = BackgroundSizing.RoomFixed)
{
    // existing setup remains unchanged
    var locked = background.AddComponent<CameraLockedRoomBackground>();
    if (sizing == BackgroundSizing.RoomFixed)
    {
        var size = room.WorldBounds.size;
        locked.ConfigureWorldSize(new Vector2(size.x, size.y));
    }
}
```

Update both Fracture builder loops to pass
`SingleRoomBackgroundBuilder.BackgroundSizing.CameraFollow`.

- [ ] **Step 4: Rebuild Fracture scenes and verify GREEN**

Run `HiddenWeight.EditorTools.ZoneSceneBuilder.BuildFractureRooms` followed by
`HiddenWeight.EditorTools.ZoneSceneBuilder.RunFractureZone`, then rerun the targeted
EditMode test. Expected: PASS; Fracture is zero-sized and Gaze remains room-fixed.

- [ ] **Step 5: Commit**

```bash
git add HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs \
  HiddenWeight/Assets/Scripts/Editor/FractureZoneBuilder.cs \
  HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs \
  HiddenWeight/Assets/Scenes/Room_Fracture_*.unity \
  HiddenWeight/Assets/Scenes/Zone_Fracture*.unity
git commit -m "fix(fracture): restore camera-follow backgrounds"
```

### Task 2: Full-Viewport Background Runtime Contract

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs`

**Interfaces:**
- Consumes: `Refresh(Camera camera)`.
- Produces: camera-follow background bounds that contain all four orthographic viewport corners.

- [ ] **Step 1: Write the failing viewport-coverage test**

Load Fracture, move the player/camera to the F01 entrance, wait for `LateUpdate`,
then compare the background renderer bounds against the four camera corners.

```csharp
Bounds bounds = background.GetComponent<SpriteRenderer>().bounds;
float halfHeight = camera.orthographicSize;
float halfWidth = halfHeight * camera.aspect;
Assert.LessOrEqual(bounds.min.x, camera.transform.position.x - halfWidth + 0.01f);
Assert.GreaterOrEqual(bounds.max.x, camera.transform.position.x + halfWidth - 0.01f);
Assert.LessOrEqual(bounds.min.y, camera.transform.position.y - halfHeight + 0.01f);
Assert.GreaterOrEqual(bounds.max.y, camera.transform.position.y + halfHeight - 0.01f);
```

- [ ] **Step 2: Run the test and verify its evidence**

Expected after Task 1: PASS. If it fails, record the exact deficient edge and change
only `Refresh` scale/position math needed for that edge; do not change the camera.

- [ ] **Step 3: Keep `Refresh` mathematically minimal**

Retain uniform cover scaling:

```csharp
float requiredHeight = camera.orthographicSize * 2f;
float requiredWidth = requiredHeight * camera.aspect;
float scale = Mathf.Max(requiredWidth / spriteSize.x, requiredHeight / spriteSize.y);
```

Only add a small `1.002f` cover margin if the test demonstrates floating-point edge
exposure. Do not add per-room magic offsets.

- [ ] **Step 4: Run the complete `FractureArtWiringTests` fixture and commit**

Expected: all tests pass, including viewport coverage and continuous terrain wiring.

```bash
git add HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs \
  HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs
git commit -m "test(fracture): guard full-screen background coverage"
```

### Task 3: Polished Continuous Terrain Modules

**Files:**
- Modify: `docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py`
- Regenerate: `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalSurfaceMiddle_v3.png`
- Regenerate: `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalWallMiddle_v3.png`
- Regenerate if needed: the matching left/right and top/bottom v3 caps.
- Test: `HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs`

**Interfaces:**
- Consumes: deterministic `make_calm_surface_strip` and `make_calm_wall_strip` inputs.
- Produces: unchanged module dimensions and filenames consumed by `TraversalArtPalette`.

- [ ] **Step 1: Add image-contract assertions before regeneration**

Extend the asset test to assert both horizontal edges of `SurfaceMiddle` and both
vertical edges of `WallMiddle` have no fully transparent pixels. Add a low-frequency
variation check so the middle strip cannot regress to a flat fill.

```csharp
Color32[] pixels = texture.GetPixels32();
Assert.IsTrue(pixels.All(pixel => pixel.a == 255), assetPath + " contains transparency");
int distinctLuma = pixels.Select(p => (p.r + p.g + p.b) / 3).Distinct().Count();
Assert.Greater(distinctLuma, 24, assetPath + " is visually flat");
```

- [ ] **Step 2: Run the test and record RED or baseline evidence**

Run `FractureTraversalAssetTests`. Expected: current strip fails the opaque/variation
contract or establishes the exact baseline that the regenerated strip must exceed.

- [ ] **Step 3: Implement the selected art direction deterministically**

In `make_calm_surface_strip`, preserve the existing long slab but add seeded,
low-opacity lavender veins, a cyan glass glow, and sparse non-periodic crystal
clusters. In `make_calm_wall_strip`, add vertical stone grain and faint edge light.
Keep all RNG seeded from the existing deterministic generator and feather only color,
never alpha, across repeat edges.

- [ ] **Step 4: Regenerate and verify the bitmap contract**

Run the generator, restore the unrelated v2 sheet if its bytes changed, and rerun
`FractureTraversalAssetTests`. Expected: correct dimensions, full opacity, sufficient
variation, complete palette references.

- [ ] **Step 5: Commit**

```bash
git add docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py \
  HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_Traversal*_v3.png \
  HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs
git commit -m "feat(fracture): polish continuous traversal art"
```

### Task 4: Visual QA, Regression, and Desktop Build

**Files:**
- Verify: all modified Fracture scenes and assets.
- Output: `.unity-logs/screenshots/room_F01.png`, `room_F05.png`, `room_F09.png`, `room_F12.png` and secret-room captures.
- Build: `HiddenWeight/Builds/macOS/HiddenWeight.app` and `/Users/ksh/Desktop/HiddenWeight.app`.

**Interfaces:**
- Consumes: rebuilt scenes and v3 assets from Tasks 1-3.
- Produces: verified Desktop application.

- [ ] **Step 1: Capture entrance and interior views**

Run `HiddenWeight.Tests.ZoneScreenshotTool.균열_모든_방을_찍는다` in a graphical
Unity batch invocation. Add entrance-position captures if the existing tool centers
the camera away from room boundaries.

- [ ] **Step 2: Inspect representative images at original resolution**

Check F01, F05, F09, F12, FS1, and FS3 for: no black margins; background covering
every edge; no clipped wall fragments; no short-block rhythm; player/hazard contrast.

- [ ] **Step 3: Run targeted regressions**

Run EditMode `FractureTraversalAssetTests`, then PlayMode
`FractureArtWiringTests;FractureFallSafetyTests;GazeFractureZoneTests`.
Expected: all targeted tests pass.

- [ ] **Step 4: Build and verify the Desktop app**

Run `HiddenWeight.EditorTools.BuildScript.BuildMac`. Verify exit code 0, log lines
`[BuildScript] 빌드 성공` and `[BuildScript] 바탕화면에 복사`, executable bit on
`/Users/ksh/Desktop/HiddenWeight.app/Contents/MacOS/Hidden Weight`, and successful
`codesign --verify --deep --strict`.

- [ ] **Step 5: Commit any scene-only rebuild output and report**

```bash
git status --short
git add HiddenWeight/Assets/Scenes/Room_Fracture_*.unity \
  HiddenWeight/Assets/Scenes/Zone_Fracture*.unity
git commit -m "chore(fracture): bake polished room visuals"
```

