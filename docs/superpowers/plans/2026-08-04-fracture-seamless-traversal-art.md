# Fracture Seamless Traversal Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the block-by-block Fracture floor and wall dressing in all 15 rooms with long, low-repetition traversal modules that preserve the existing white-stone and blue-glass style.

**Architecture:** A deterministic Pillow generator derives seven v3 traversal sprites from the existing Fracture terrain art without overwriting v2. `TraversalArtPalette` exposes the sprites as one complete-or-null module set, and `CameraLockedRoomBackground` uses that set to dress horizontal and vertical collision runs with caps plus long middle pieces. Existing `TerrainTileSet` rendering remains the fallback if the v3 set is incomplete.

**Tech Stack:** Python 3 + Pillow, Unity 6.0 C#, Unity 2D SpriteRenderer/Tilemap, NUnit EditMode and PlayMode tests.

## Global Constraints

- Apply the visual change to F01~F12, FS1~FS3, and `Zone_Fracture_Full`.
- Preserve Fracture's bright white stone and blue glass style.
- Do not change collision Tilemaps, BoxCollider2D geometry, room sizes, traversal routes, gameplay values, backgrounds, characters, enemies, doors, or interactables.
- Do not change Prologue, Residue, or Gaze rendering.
- Never overwrite `Fracture_TerrainTiles_v2.png`; all new project assets use `v3` filenames.
- Fall back to the existing complete `TerrainTileSet` whenever any v3 module is missing.

---

## File Structure

- Modify `docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py`: generate and validate the seven deterministic v3 module PNGs after generating v2.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalSurfaceLeft_v3.png`: horizontal run left cap.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalSurfaceMiddle_v3.png`: long, low-repetition horizontal middle.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalSurfaceRight_v3.png`: horizontal run right cap.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalWallTop_v3.png`: vertical run top cap.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalWallMiddle_v3.png`: long, low-detail vertical middle.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalWallBottom_v3.png`: vertical run bottom cap.
- Create `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_TraversalFill_v3.png`: low-contrast stone/glass fill texture.
- Modify `HiddenWeight/Assets/Scripts/World/TraversalArtPalette.cs`: define `ContinuousTerrainSet` and expose Fracture v3 availability.
- Modify `HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs`: import the seven sprites and wire the palette.
- Modify `HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs`: add horizontal and vertical continuous-run renderers and preserve fallback paths.
- Modify `HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs`: assert v3 wiring, low repetition, collision alignment, and no cross-zone regression.
- Create `HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs`: validate source files, dimensions, alpha, and palette completeness.
- Regenerate `HiddenWeight/Assets/Resources/TraversalArtPalette.asset` and the 16 Fracture scenes through their existing builders.

---

### Task 1: Deterministic v3 Traversal Assets

**Files:**
- Modify: `docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py`
- Create: seven `HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_Traversal*_v3.png` files listed above
- Test: `HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs`

**Interfaces:**
- Consumes: `build()` and its graded 256×256 v2 tile cells.
- Produces: `build_continuous_modules(tiles: list[Image.Image]) -> dict[str, Image.Image]`; seven RGBA PNGs with non-empty alpha coverage.

- [ ] **Step 1: Write the failing asset contract test**

Create an EditMode test with the exact expected asset names and assert that each loads as a `Texture2D`, has an alpha-capable importer, and meets its role dimensions:

```csharp
static readonly string[] Names = {
    "SurfaceLeft", "SurfaceMiddle", "SurfaceRight",
    "WallTop", "WallMiddle", "WallBottom", "Fill"
};

[TestCaseSource(nameof(Names))]
public void Fracture_v3_연속형_모듈이_존재한다(string role)
{
    string path = $"Assets/Art/Fracture/Environment/Terrain/Fracture_Traversal{role}_v3.png";
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    Assert.IsNotNull(texture, path);
    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
    Assert.IsNotNull(importer);
    Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
    Assert.IsTrue(importer.alphaIsTransparency || role == "Fill");
}
```

- [ ] **Step 2: Run the focused EditMode test and verify it fails**

Run:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -nographics -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform EditMode \
  -testFilter HiddenWeight.Tests.FractureTraversalAssetTests \
  -testResults "$PWD/.unity-logs/fracture-v3-assets-red.xml" \
  -logFile "$PWD/.unity-logs/fracture-v3-assets-red.log"
```

Expected: FAIL because the seven v3 files do not exist.

- [ ] **Step 3: Add deterministic module generation**

Extend the existing Pillow script so it keeps the graded v2 cells in memory and builds:

```python
MODULE_OUT = ART / "Environment/Terrain"
MODULE_NAMES = {
    "SurfaceLeft": "Fracture_TraversalSurfaceLeft_v3.png",
    "SurfaceMiddle": "Fracture_TraversalSurfaceMiddle_v3.png",
    "SurfaceRight": "Fracture_TraversalSurfaceRight_v3.png",
    "WallTop": "Fracture_TraversalWallTop_v3.png",
    "WallMiddle": "Fracture_TraversalWallMiddle_v3.png",
    "WallBottom": "Fracture_TraversalWallBottom_v3.png",
    "Fill": "Fracture_TraversalFill_v3.png",
}

def build_continuous_modules(graded_tiles):
    surface_middle = Image.new("RGBA", (1024, 256), (0, 0, 0, 0))
    for index, tile in enumerate(graded_tiles[1:5]):
        surface_middle.alpha_composite(tile, (index * 256, 0))
    wall_middle = make_calm_wall_strip(graded_tiles[6:10], (256, 768))
    return {
        "SurfaceLeft": graded_tiles[0],
        "SurfaceMiddle": feather_horizontal_seam(surface_middle, 8),
        "SurfaceRight": graded_tiles[5],
        "WallTop": make_wall_cap(graded_tiles[10], top=True),
        "WallMiddle": wall_middle,
        "WallBottom": make_wall_cap(graded_tiles[14], top=False),
        "Fill": make_low_contrast_fill(graded_tiles[8], (512, 512)),
    }
```

`make_calm_wall_strip` must use only colors sampled from the four existing wall cells, remove high-frequency ornament with a deterministic blur/downsample/upsample pass, and restore subtle stone joints at no less than 192 px spacing. `feather_horizontal_seam` must copy/alpha-blend eight pixels across the left and right boundaries so repeated middle modules have no transparent seam. `make_low_contrast_fill` must keep luminance contrast below 25% of the source wall cell. Validate every output with `mode == "RGBA"`, non-empty bounding box, and exact dimensions before saving.

- [ ] **Step 4: Generate the assets and verify their pixel contracts**

Run:

```bash
python3 docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py
python3 - <<'PY'
from pathlib import Path
from PIL import Image
root = Path('HiddenWeight/Assets/Art/Fracture/Environment/Terrain')
for path in sorted(root.glob('Fracture_Traversal*_v3.png')):
    im = Image.open(path)
    assert im.mode == 'RGBA', (path, im.mode)
    assert im.getbbox(), path
    print(path.name, im.size)
PY
```

Expected: seven RGBA files; surface middle is 1024×256, wall middle is 256×768, fill is 512×512, and all caps are non-empty.

- [ ] **Step 5: Commit the generated asset unit**

```bash
git add docs/concept-art/generated/fracture-terrain-v2/build_fracture_terrain_v2.py \
  HiddenWeight/Assets/Art/Fracture/Environment/Terrain/Fracture_Traversal*_v3.png \
  HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs
git commit -m "feat(fracture): derive continuous traversal modules"
```

---

### Task 2: Palette Wiring with Safe Fallback

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/World/TraversalArtPalette.cs`
- Modify: `HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs`
- Modify: `HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs`
- Regenerate: `HiddenWeight/Assets/Resources/TraversalArtPalette.asset`

**Interfaces:**
- Consumes: seven single-sprite PNGs from Task 1.
- Produces: `ContinuousTerrainSet fractureContinuous`; `ContinuousTerrainSet ContinuousSetFor(string sceneName)`; `bool ContinuousTerrainSet.IsComplete`.

- [ ] **Step 1: Add the failing palette completeness test**

```csharp
[Test]
public void Fracture_팔레트가_v3_모듈을_전부_참조한다()
{
    var palette = AssetDatabase.LoadAssetAtPath<TraversalArtPalette>(
        "Assets/Resources/TraversalArtPalette.asset");
    Assert.IsNotNull(palette);
    Assert.IsNotNull(palette.fractureContinuous);
    Assert.IsTrue(palette.fractureContinuous.IsComplete);
    Assert.AreSame(palette.fractureContinuous,
        palette.ContinuousSetFor("Room_Fracture_F01"));
    Assert.IsNull(palette.ContinuousSetFor("Room_Gaze_G01"));
}
```

- [ ] **Step 2: Run the focused test and verify the missing API failure**

Run the Task 1 Unity command with `-testFilter HiddenWeight.Tests.FractureTraversalAssetTests`.

Expected: test assembly compilation fails because `fractureContinuous` and `ContinuousSetFor` are undefined.

- [ ] **Step 3: Implement the complete-or-null palette API**

Add:

```csharp
[System.Serializable]
public sealed class ContinuousTerrainSet
{
    public Sprite surfaceLeft, surfaceMiddle, surfaceRight;
    public Sprite wallTop, wallMiddle, wallBottom;
    public Sprite fill;
    public bool IsComplete => surfaceLeft != null && surfaceMiddle != null
        && surfaceRight != null && wallTop != null && wallMiddle != null
        && wallBottom != null && fill != null;
}
```

Add `public ContinuousTerrainSet fractureContinuous;` and return it only for Fracture scene names when complete. Keep `TileSetFor` unchanged as the fallback API.

- [ ] **Step 4: Import and wire the single-sprite modules**

In `BuildTraversalArtPalette`, call the existing traversal import helper for the six transparent edge/surface sprites and the fill import helper for `Fracture_TraversalFill_v3.png`. Load them into one `ContinuousTerrainSet`. Change the validation exception to require both the legacy fallback and complete v3 set.

Run:

```bash
"$UNITY" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.SingleRoomBackgroundBuilder.BuildTraversalArtPalette \
  -logFile "$PWD/.unity-logs/fracture-v3-palette.log"
```

- [ ] **Step 5: Run the asset and palette tests**

Run the focused EditMode command again.

Expected: PASS, with Gaze returning null from `ContinuousSetFor`.

- [ ] **Step 6: Commit palette wiring**

```bash
git add HiddenWeight/Assets/Scripts/World/TraversalArtPalette.cs \
  HiddenWeight/Assets/Scripts/Editor/SingleRoomBackgroundBuilder.cs \
  HiddenWeight/Assets/Resources/TraversalArtPalette.asset \
  HiddenWeight/Assets/Tests/EditMode/FractureTraversalAssetTests.cs \
  HiddenWeight/Assets/Art/Fracture/Environment/Terrain/*.meta
git commit -m "feat(fracture): wire continuous traversal palette"
```

---

### Task 3: Continuous Horizontal and Vertical Run Rendering

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs`

**Interfaces:**
- Consumes: `ContinuousTerrainSet ContinuousSetFor(string sceneName)`.
- Produces: `AddContinuousHorizontalRun(...)`, `AddContinuousVerticalRun(...)`, and runtime children named `FractureSurfaceCapLeft`, `FractureSurfaceMiddle`, `FractureSurfaceCapRight`, `FractureWallCapTop`, `FractureWallMiddle`, `FractureWallCapBottom`.

- [ ] **Step 1: Write failing PlayMode behavior tests**

Add tests that load `Zone_Fracture_Full` and assert:

```csharp
Assert.Greater(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
    .Count(t => t.name == "FractureSurfaceMiddle"), 0);
Assert.Zero(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
    .Count(r => r.name == "TraversalSurface" &&
                r.sprite != null && r.sprite.name.StartsWith("FractureTerrain_r1")));
```

For every horizontal run, group the v3 children by parent and assert caps occur at most once per side, middle modules are no more than `ceil(worldWidth / 5f) + 1`, and renderer bounds touch the Tilemap top boundary within 0.06 world units. For every vertical run, assert wall renderer bounds touch the exposed collider/tile boundary within 0.06 world units. Also assert no v3-named renderer exists after loading one Prologue, Residue, and Gaze scene.

- [ ] **Step 2: Run the Fracture PlayMode fixture and verify it fails**

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform PlayMode \
  -testFilter HiddenWeight.Tests.FractureArtWiringTests \
  -testResults "$PWD/.unity-logs/fracture-v3-play-red.xml" \
  -logFile "$PWD/.unity-logs/fracture-v3-play-red.log"
```

Expected: FAIL because no v3 runtime renderers exist.

- [ ] **Step 3: Implement horizontal run composition**

In `AddTraversalSurface`, query `ContinuousSetFor` before `TileSetFor`. Add one run root and place left/right caps only when the span can hold them. Fill the remaining span with `surfaceMiddle` at a natural width of `SurfaceHeight * spriteAspect`, using `Mathf.CeilToInt` and redistributing only the final module. Apply `0.02f` world-unit overlap on internal joins, keep the upper sprite bound on the collision surface, and use the existing tint/sorting helpers.

Route `BuildTiledPlatformSurface` through the same helper using the platform's collider bounds. Do not route moving, crumbling, omen, or other objects that already have a non-placeholder Fracture platform sprite.

- [ ] **Step 4: Implement vertical run composition**

In `AddTilemapWallFace` and `BuildTiledWallClimbSurface`, query the continuous set first. Compose top cap, one or more long middle pieces, and bottom cap across the full exposed height. Keep the face width at 1.1 world units for Tilemap sides and at the collider's actual width for wall blocks. Use a 0.02f overlap, clamp Tilemap side depth to `MaxWallFaceDrop`, and place no per-course ornament.

If any sprite has a zero-size bound, log one `Debug.LogError` containing the scene and role, then call the existing `TerrainTileSet` path for that run.

- [ ] **Step 5: Run focused PlayMode and existing Fracture structure tests**

Run the Step 2 command and then:

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform PlayMode \
  -testFilter "HiddenWeight.Tests.FractureArtWiringTests;HiddenWeight.Tests.ZonePlayableTests" \
  -testResults "$PWD/.unity-logs/fracture-v3-play-green.xml" \
  -logFile "$PWD/.unity-logs/fracture-v3-play-green.log"
```

Expected: PASS; collision sizes and routes remain unchanged.

- [ ] **Step 6: Commit continuous rendering**

```bash
git add HiddenWeight/Assets/Scripts/World/CameraLockedRoomBackground.cs \
  HiddenWeight/Assets/Tests/PlayMode/FractureArtWiringTests.cs
git commit -m "fix(fracture): render terrain as continuous runs"
```

---

### Task 4: Rebuild All Fracture Scenes and Verify Visually

**Files:**
- Regenerate: `HiddenWeight/Assets/Scenes/Room_Fracture_F01.unity` through `Room_Fracture_FS3.unity`
- Regenerate: `HiddenWeight/Assets/Scenes/Zone_Fracture.unity`
- Regenerate: `HiddenWeight/Assets/Scenes/Zone_Fracture_Full.unity`
- Inspect: `.unity-logs/screenshots` outputs from `ZoneScreenshotTool`

**Interfaces:**
- Consumes: generator, palette, and continuous run renderer from Tasks 1–3.
- Produces: rebuilt Fracture scenes and visual evidence for the completion criteria.

- [ ] **Step 1: Rebuild the 15 room scenes and full scene**

```bash
"$UNITY" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.FractureZoneBuilder.BuildFractureRooms \
  -logFile "$PWD/.unity-logs/fracture-v3-rooms.log"
"$UNITY" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -executeMethod HiddenWeight.EditorTools.FractureZoneBuilder.RunFractureZone \
  -logFile "$PWD/.unity-logs/fracture-v3-full.log"
```

Expected: logs contain completion messages for 15 rooms and the full zone, with no compile/import exception.

- [ ] **Step 2: Run all EditMode tests and focused PlayMode regression tests**

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/.unity-logs/fracture-v3-edit-all.xml" \
  -logFile "$PWD/.unity-logs/fracture-v3-edit-all.log"
"$UNITY" -batchmode -nographics -projectPath "$PWD/HiddenWeight" \
  -runTests -testPlatform PlayMode \
  -testFilter "HiddenWeight.Tests.FractureArtWiringTests;HiddenWeight.Tests.ZonePlayableTests;HiddenWeight.Tests.PrologueLayoutTests;HiddenWeight.Tests.GazeArtWiringTests;HiddenWeight.Tests.ResiduePlacementTests" \
  -testResults "$PWD/.unity-logs/fracture-v3-regression.xml" \
  -logFile "$PWD/.unity-logs/fracture-v3-regression.log"
```

Expected: zero failed tests in both XML files.

- [ ] **Step 3: Capture representative long floors and walls**

Run `ZoneScreenshotTool` for at least F01, F04, F09, F12, FS1, FS2, and FS3. Inspect the PNGs at original detail and confirm: no black seams, caps only at real ends, no ornament every cell, wall faces read as one structure, collision surfaces remain legible, and the player remains visually dominant.

- [ ] **Step 4: Make one targeted art iteration if visual QA fails**

If a seam is visible, change only `feather_horizontal_seam` overlap. If ornament is still too dense, change only the surface middle source cell selection. If walls look flat, change only the calm wall strip joint opacity. Regenerate assets, rebuild scenes, and repeat Steps 2–3; do not change collider geometry or room layouts.

- [ ] **Step 5: Commit regenerated scenes and final verification changes**

```bash
git add HiddenWeight/Assets/Scenes/Room_Fracture_*.unity \
  HiddenWeight/Assets/Scenes/Zone_Fracture.unity \
  HiddenWeight/Assets/Scenes/Zone_Fracture_Full.unity \
  HiddenWeight/Assets/Art/Fracture/Environment/Terrain \
  HiddenWeight/Assets/Resources/TraversalArtPalette.asset \
  HiddenWeight/Assets/Scripts HiddenWeight/Assets/Tests
git commit -m "chore(fracture): rebuild rooms with continuous terrain"
```

Record the exact passed/failed counts and representative screenshot paths in the implementation handoff.
