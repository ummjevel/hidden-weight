# Single-Room Background Restoration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace split parallax room art with one sharp, room-specific concept image per room, remove all foreground/background motion layers, hide placeholder Tilemap rendering, and preserve playable collision.

**Architecture:** A shared editor builder resolves one sprite path from a room name and creates a single `RoomBackground`. A small runtime component keeps that 16:9 image locked to the active camera and scales it to the viewport without stretching. Each full-zone builder calls the shared builder, omits legacy motion layers, and disables only the placeholder Tilemap renderer while leaving its collider active.

**Tech Stack:** Unity 6000.5.4f1, C#, Unity Test Framework, SpriteRenderer, TilemapRenderer

## Global Constraints

- Use exactly one room-specific background image per room.
- Do not create `BG_Far`, `BG_Mid`, `FG_Overlay`, `MotionBack`, or `MotionFront`.
- Do not change Tilemap collision behavior.
- Preserve source aspect ratio; never apply different X and Y scales.
- Validate all 45 rooms at 1920×1080.
- Do not modify unrelated dirty-worktree files.

---

### Task 1: Add failing single-background regression tests

**Files:**
- Modify: `HiddenWeight/Assets/Tests/EditMode/ResidueRoomArtBuilderTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/GazeArtWiringTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs`

**Interfaces:**
- Consumes: existing `ResidueRoomArtBuilder.BuildRoomArt(Room)`
- Produces: executable requirements for `Art/RoomBackground` and forbidden legacy layers

- [ ] **Step 1: Change the edit-mode expectation to one background**

Replace `BuildRoomArtCreatesBackgroundLayersOnly` with assertions equivalent to:

```csharp
ResidueRoomArtBuilder.BuildRoomArt(room);
var background = room.transform.Find("Art/RoomBackground");
Assert.That(background, Is.Not.Null);
Assert.That(background.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
Assert.That(background.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(-30));
Assert.That(background.GetComponent<CameraLockedRoomBackground>(), Is.Not.Null);
foreach (var forbidden in new[] { "Far", "Mid", "Foreground", "BG_Far", "BG_Mid", "FG_Overlay" })
    Assert.That(room.transform.Find("Art/" + forbidden), Is.Null);
```

- [ ] **Step 2: Change Gaze and Fracture play-mode art tests**

For every room, assert:

```csharp
Assert.IsNotNull(room.transform.Find("Art/RoomBackground"));
Assert.IsNull(room.transform.Find("MotionBack"));
Assert.IsNull(room.transform.Find("MotionFront"));
Assert.IsNull(room.transform.Find("Art/BG_Far"));
Assert.IsNull(room.transform.Find("Art/BG_Mid"));
Assert.IsNull(room.transform.Find("Art/FG_Overlay"));
```

Also assert the zone `TilemapRenderer.enabled` value is `false`.

- [ ] **Step 3: Run the edit-mode test and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform EditMode \
  -testFilter HiddenWeight.Tests.ResidueRoomArtBuilderTests \
  -testResults /tmp/room-background-red.xml \
  -logFile /tmp/room-background-red.log
```

Expected: test failure because `Art/RoomBackground` and `CameraLockedRoomBackground` do not exist.

---

### Task 2: Add the room-specific source assets

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Rooms/Room01.png` through `Room12.png`
- Create: `HiddenWeight/Assets/Art/Residue/Rooms/Secret01.png` through `Secret03.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Rooms/GazeRoom01.png` through `GazeRoom12.png`
- Create: `HiddenWeight/Assets/Art/Gaze/Rooms/GazeSecret01.png` through `GazeSecret03.png`
- Reuse: `HiddenWeight/Assets/Art/Fracture/Rooms/FractureRoom01.png` through `FractureSecret03.png`

**Interfaces:**
- Consumes: concept PNGs in `docs/concept-art/generated/residue-rooms-v2/` and `gaze-rooms-v1/`
- Produces: exact `<artRoot>/Rooms/<room.name>.png` lookup contract

- [ ] **Step 1: Copy and rename the 15 Residue originals**

Map `01-*.png`…`12-*.png` to `Room01.png`…`Room12.png`, and `S1-*`…`S3-*` to `Secret01.png`…`Secret03.png`.

- [ ] **Step 2: Copy and rename the 15 Gaze originals**

Map `01-*.png`…`12-*.png` to `GazeRoom01.png`…`GazeRoom12.png`, and `S1-*`…`S3-*` to `GazeSecret01.png`…`GazeSecret03.png`.

- [ ] **Step 3: Verify asset dimensions**

Run:

```bash
find HiddenWeight/Assets/Art/{Residue,Gaze,Fracture}/Rooms -name '*.png' -print0 |
  xargs -0 magick identify -format '%i %wx%h\n'
```

Expected: 45 room images and every image reports `1672x941`.

---

### Task 3: Implement the camera-locked single background

**Files:**
- Create: `HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs`
- Create: `HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs`
- Modify: `HiddenWeight/Assets/Scripts/Editor/ResidueRoomArtBuilder.cs`

**Interfaces:**
- Produces: `CameraLockedRoomBackground.Refresh(Camera camera)`
- Produces: `SingleRoomBackgroundBuilder.Build(Room room, string artRoot)`
- Consumes: `<artRoot>/Rooms/<room.name>.png`

- [ ] **Step 1: Implement viewport locking**

`CameraLockedRoomBackground` must:

```csharp
void LateUpdate()
{
    var camera = Camera.main;
    if (camera != null) Refresh(camera);
}

public void Refresh(Camera camera)
{
    transform.position = new Vector3(
        camera.transform.position.x,
        camera.transform.position.y,
        transform.position.z);

    var renderer = GetComponent<SpriteRenderer>();
    if (renderer == null || renderer.sprite == null) return;
    var size = renderer.sprite.bounds.size;
    float requiredWidth = camera.orthographicSize * 2f * camera.aspect;
    float requiredHeight = camera.orthographicSize * 2f;
    float scale = Mathf.Max(requiredWidth / size.x, requiredHeight / size.y);
    transform.localScale = new Vector3(scale, scale, 1f);
}
```

- [ ] **Step 2: Implement the shared editor builder**

`SingleRoomBackgroundBuilder.Build` must:

- Create or reuse `Art`.
- Delete every existing child of `Art`.
- Create `RoomBackground`.
- Load `$"{artRoot}/Rooms/{room.name}.png"`.
- Throw `InvalidOperationException` when the sprite is missing.
- Add `SpriteRenderer` at sorting order `-30`.
- Add `CameraLockedRoomBackground`.
- Add one `RoomVisualCuller` to `Art`.

- [ ] **Step 3: Delegate Residue construction**

Replace `ResidueRoomArtBuilder.BuildRoomArt` internals with:

```csharp
SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Residue");
```

- [ ] **Step 4: Run the edit-mode test and verify GREEN**

Run the Task 1 EditMode command again.

Expected: all `ResidueRoomArtBuilderTests` pass.

---

### Task 4: Integrate all three full-zone builders and remove foreground

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/Editor/ResidueZoneBuilder.cs`
- Modify: `HiddenWeight/Assets/Scripts/Editor/GazeZoneBuilder.cs`
- Modify: `HiddenWeight/Assets/Scripts/Editor/FractureZoneBuilder.cs`

**Interfaces:**
- Consumes: `SingleRoomBackgroundBuilder.Build(Room, string)`
- Produces: rebuilt `Zone_Residue_Full`, `Zone_Gaze_Full`, and `Zone_Fracture_Full`

- [ ] **Step 1: Remove room motion construction**

Delete the `BuildRoomMotion` call from all three builders. No full-zone scene may create `MotionBack` or `MotionFront`.

- [ ] **Step 2: Replace split background calls**

Use:

```csharp
SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Residue");
SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Gaze");
SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Fracture");
```

Remove `BuildGazeRoomLayers`, `BuildGazeRoomLayer`, `BuildFractureRoomLayers`, and `BuildFractureRoomLayer`.

- [ ] **Step 3: Hide placeholder Tilemap rendering**

After each full-zone Tilemap is created:

```csharp
tilemap.GetComponent<TilemapRenderer>().enabled = false;
```

Do not disable `Tilemap`, `TilemapCollider2D`, or its static `Rigidbody2D`.

- [ ] **Step 4: Add Room08 camera headroom**

Change the three Room08 room heights from `28f` to `30f`, leaving their minimum coordinate and gameplay geometry unchanged.

- [ ] **Step 5: Rebuild the three scenes**

Run each execute method separately:

```bash
Unity -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.ResidueZoneBuilder.RunResidueZone -quit
Unity -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.GazeZoneBuilder.RunGazeZone -quit
Unity -batchmode -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.FractureZoneBuilder.RunFractureZone -quit
```

Expected: all three scenes save with 15 `RoomBackground` objects and no legacy layer names.

---

### Task 5: Verify behavior and visual alignment

**Files:**
- Modify if required by verified failures only: the three full-zone builder files
- Test: all existing EditMode and selected PlayMode map tests

**Interfaces:**
- Consumes: rebuilt scenes
- Produces: verified 45-room implementation

- [ ] **Step 1: Run background wiring tests**

Run the affected EditMode and PlayMode tests.

Expected:

- 45/45 rooms have one `RoomBackground`.
- Zero `MotionFront`, `MotionBack`, `BG_Far`, `BG_Mid`, or `FG_Overlay`.
- Three Tilemap renderers are disabled.

- [ ] **Step 2: Run map traversal tests**

Run:

```text
ResidueZoneTests
ResiduePlacementTests
GazeFracturePlaythroughTests
GazeFractureZoneTests
ZonePlayableTests
```

Expected: no traversal, collision, lift, or placement regressions.

- [ ] **Step 3: Render 45 whole-room and player views**

Capture each room at 1920×1080 with the runtime camera size `6`. Review:

- Background covers the viewport.
- Source aspect ratio is preserved.
- No foreground covers the player.
- No large gray, purple, or green Tilemap area is visible.
- Room08 player head remains inside the camera frame.

- [ ] **Step 4: Correct only confirmed placement mismatches**

When a gameplay platform edge differs from the concept-art surface by more than `0.5` world units, change the corresponding `BuildRxx`, `BuildGxx`, or `BuildFxx` placement coordinate and rerun the room traversal test. Do not move collision geometry for decorative structures or perspective-only background architecture.

- [ ] **Step 5: Run final verification**

Run the complete affected test set and confirm XML reports zero failures. Run `git diff --check`.

- [ ] **Step 6: Commit implementation**

Stage only the background assets, related scripts, tests, and rebuilt scenes. Use:

```bash
git commit -m "fix: restore sharp room-specific backgrounds"
```
