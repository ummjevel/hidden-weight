# Residue Production Map Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 잔재 지역의 12개 메인 방과 3개 비밀방을 실제 Unity 2D 맵에서 사용할 수 있는 배경 레이어, 발판 스프라이트, 상호작용 오브젝트와 룸 구성으로 제작한다.

**Architecture:** 방마다 불투명 원경과 투명 중경·전경을 SpriteRenderer로 배치하고, 실제 이동 판정은 기존 Grid/Tilemap/Collider가 담당한다. 발판과 상호작용 오브젝트는 잔재 지역 공용 아틀라스로 관리하며, 생성 이미지의 복잡한 외곽선을 충돌 판정으로 사용하지 않는다.

**Tech Stack:** Unity 2022.3, URP 2D, SpriteRenderer, Grid/Tilemap, TilemapCollider2D, CompositeCollider2D, PNG RGBA, 32 Pixels Per Unit

## Global Constraints

- 카메라 Orthographic Size는 `6`을 유지한다.
- 모든 잔재 아트는 `32 PPU`를 사용한다.
- 회화형 이미지는 `Bilinear`, Mip Maps Off, Wrap Mode Clamp로 임포트한다.
- 배경에는 캐릭터를 포함하지 않는다.
- 원경과 장식에는 Collider를 추가하지 않는다.
- 실제 플레이 지형은 Tilemap 또는 단순 BoxCollider2D로 만든다.
- 메인 기준 이미지는 `docs/concept-art/generated/residue-full-region-map-v2.png`이다.
- 방별 기준 이미지는 `docs/concept-art/generated/residue-rooms-v2/*.png`이다.

---

### Task 1: Room01 제작 기준 세트 확정

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Room01/Room01_BG_Far.png`
- Create: `HiddenWeight/Assets/Art/Residue/Room01/Room01_BG_Mid.png`
- Create: `HiddenWeight/Assets/Art/Residue/Room01/Room01_FG_Overlay.png`
- Create: `HiddenWeight/Assets/Art/Residue/Residue_TerrainAtlas.png`
- Create: `HiddenWeight/Assets/Art/Residue/Residue_InteractablesAtlas.png`
- Create: `HiddenWeight/Assets/Art/Residue/README.md`

**Interfaces:**
- Consumes: 잔재 마스터 이미지와 Room01 콘셉트
- Produces: 이후 모든 방이 따라야 하는 팔레트, 재질, 레이어 순서와 공용 아틀라스

- [x] **Step 1: 원경 배경 생성**

원경은 불투명 PNG로 만들고, 실제 발판처럼 보이는 전경 구조를 넣지 않는다.

- [x] **Step 2: 중경과 전경 생성**

중경과 전경은 단색 크로마 배경으로 생성한 뒤 알파 PNG로 변환한다.

- [x] **Step 3: 공용 아틀라스 생성**

Terrain은 4열×2행, Interactables는 3열×2행으로 구성한다.

- [x] **Step 4: 알파 채널 검증**

Run:

```bash
file HiddenWeight/Assets/Art/Residue/Room01/*.png \
  HiddenWeight/Assets/Art/Residue/Residue_*Atlas.png
```

Expected: 중경·전경·아틀라스는 `RGBA`, 원경은 `RGB` 또는 `RGBA`.

---

### Task 2: 나머지 14개 방의 배경 레이어 제작

**Files:**
- Create: `HiddenWeight/Assets/Art/Residue/Room02`부터 `Room12`
- Create: `HiddenWeight/Assets/Art/Residue/Secret01`부터 `Secret03`

**Interfaces:**
- Consumes: `residue-full-region-map-v2.png`, 각 방 콘셉트, Room01 레이어 규격
- Produces: 각 방의 `BG_Far`, `BG_Mid`, `FG_Overlay`

- [x] **Step 1: 각 방의 원경 생성**

각 콘셉트에서 플레이 가능한 발판과 캐릭터를 제거하고, 해당 위치의 원경·심도·거대 구조만 남긴다. 파일명은 `<Room>_BG_Far.png`로 통일한다.

- [x] **Step 2: 각 방의 중경 생성**

해당 방을 특정하는 손가락 뿌리, 곡선교 아치, 승강축, 손목 구조 등을 단색 크로마 배경에 분리해 생성하고 `<Room>_BG_Mid.png` RGBA로 변환한다.

- [x] **Step 3: 각 방의 전경 생성**

화면 가장자리의 사슬, 난간, 기둥과 가림 구조만 남기고 중앙 플레이 공간은 비운다. `<Room>_FG_Overlay.png` RGBA로 변환한다.

- [x] **Step 4: 파일 수와 규격 검증**

Run:

```bash
find HiddenWeight/Assets/Art/Residue \
  -type f -name '*.png' | sort
```

Expected: 방별 레이어 45개와 공용 아틀라스 2개가 존재한다.

---

### Task 3: Unity 아트 임포트 자동화

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Editor/ResidueArtImporter.cs`
- Create: `HiddenWeight/Assets/Tests/EditMode/ResidueArtImporterTests.cs`

**Interfaces:**
- Consumes: `Assets/Art/Residue` 아래 PNG
- Produces: `ResidueArtImporter.ConfigureAll()`와 Unity 임포트 설정

- [x] **Step 1: 실패하는 임포트 설정 테스트 작성**

```csharp
[Test]
public void BackgroundImportSettingsUseProjectScale()
{
    ResidueArtImporter.ConfigureAll();
    var importer = (TextureImporter)AssetImporter.GetAtPath(
        "Assets/Art/Residue/Room01/Room01_BG_Far.png");

    Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
    Assert.AreEqual(32f, importer.spritePixelsPerUnit);
    Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
    Assert.IsFalse(importer.mipmapEnabled);
}
```

- [x] **Step 2: 테스트 실패 확인**

Run: Unity EditMode test `ResidueArtImporterTests`

Expected: `ResidueArtImporter`가 없어 컴파일 실패.

- [x] **Step 3: 최소 임포터 구현**

```csharp
public static class ResidueArtImporter
{
    const string Root = "Assets/Art/Residue";

    [MenuItem("Hidden Weight/Art/Configure Residue Art")]
    public static void ConfigureAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = path.Contains("_Mid") ||
                path.Contains("_FG_") || path.Contains("Atlas");
            importer.SaveAndReimport();
        }
    }
}
```

- [x] **Step 4: EditMode 테스트 통과 확인**

Run: Unity EditMode test `ResidueArtImporterTests`

Expected: PASS.

---

### Task 4: 패럴랙스 룸 레이어 구성

**Files:**
- Create: `HiddenWeight/Assets/Scripts/World/ParallaxLayer.cs`
- Create: `HiddenWeight/Assets/Tests/EditMode/ParallaxLayerTests.cs`
- Modify: `HiddenWeight/Assets/Scenes/Zone_Residue.unity`

**Interfaces:**
- Consumes: 카메라 위치와 방별 Far/Mid/Foreground SpriteRenderer
- Produces: `ParallaxLayer.SetAnchor(Vector3 cameraPosition)`와 룸별 패럴랙스

- [x] **Step 1: 실패하는 패럴랙스 계산 테스트 작성**

```csharp
[Test]
public void MidLayerMovesHalfCameraDelta()
{
    var go = new GameObject();
    var layer = go.AddComponent<ParallaxLayer>();
    layer.SetMultiplierForTest(0.5f);
    layer.SetAnchor(Vector3.zero);
    layer.ApplyCameraPosition(new Vector3(4f, 2f, 0f));

    Assert.AreEqual(new Vector3(2f, 1f, 0f), go.transform.position);
}
```

- [x] **Step 2: 테스트 실패 확인**

Run: Unity EditMode test `ParallaxLayerTests`

Expected: 클래스가 없어 컴파일 실패.

- [x] **Step 3: 최소 패럴랙스 구현**

```csharp
public sealed class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float multiplier = 0.5f;
    Vector3 anchorLayer;
    Vector3 anchorCamera;

    public void SetAnchor(Vector3 cameraPosition)
    {
        anchorLayer = transform.position;
        anchorCamera = cameraPosition;
    }

    public void ApplyCameraPosition(Vector3 cameraPosition)
    {
        Vector3 delta = cameraPosition - anchorCamera;
        transform.position = anchorLayer + new Vector3(
            delta.x * multiplier, delta.y * multiplier, 0f);
    }

#if UNITY_EDITOR
    public void SetMultiplierForTest(float value) => multiplier = value;
#endif
}
```

- [x] **Step 4: 테스트 통과 확인**

Run: Unity EditMode test `ParallaxLayerTests`

Expected: PASS.

- [x] **Step 5: 씬 레이어 배치**

각 Room 아래 `Art/Far`, `Art/Mid`, `Art/Foreground`를 만들고 Sorting Order를 `-30`, `-20`, `20`으로 설정한다. Terrain은 기존 Tilemap과 Collider를 유지한다.

---

### Task 5: 잔재 Tilemap과 상호작용 오브젝트 교체

**Files:**
- Modify: `HiddenWeight/Assets/Scenes/Zone_Residue.unity`
- Modify: `HiddenWeight/Assets/Prefabs/Gate.prefab`
- Modify: `HiddenWeight/Assets/Prefabs/Checkpoint.prefab`
- Modify: `HiddenWeight/Assets/Prefabs/RewindableBlock.prefab`

**Interfaces:**
- Consumes: Terrain/Interactables 아틀라스와 기존 게임플레이 컴포넌트
- Produces: 아트가 교체된 플레이 가능 잔재 씬

- [x] **Step 1: Terrain 아틀라스 분할**

`Residue_TerrainAtlas.png`를 4열×2행 Multiple Sprite로 분할하고, 필요한 모듈을 Tile 또는 SpriteRenderer로 배치한다.

- [x] **Step 2: Interactables 아틀라스 분할**

`Residue_InteractablesAtlas.png`를 `512×512` 고정 그리드로 분할해 닫힌 문, 열린 문, 체크포인트, 되감기 전·후, 기억 파편 스프라이트를 지정한다.

- [ ] **Step 3: 충돌과 시각 분리**

TilemapCollider2D와 BoxCollider2D는 단순한 사각형·경사형으로 유지하고 복잡한 스프라이트 외곽선을 사용하지 않는다.

- [ ] **Step 4: 기존 플레이 테스트 실행**

Run: Unity PlayMode tests

Expected: 이동, 점프, 되감기, 체크포인트와 게이트 테스트가 모두 PASS.

---

### Task 6: 최종 시각·게임플레이 검증

**Files:**
- Verify: `HiddenWeight/Assets/Scenes/Zone_Residue.unity`
- Verify: `HiddenWeight/Assets/Art/Residue`

- [ ] **Step 1: 씬 렌더 확인**

16:9 Game View에서 각 방의 원경, 중경, 지형, 플레이어, 전경 순서가 올바른지 확인한다.

- [ ] **Step 2: 가독성 확인**

실제 발판과 배경 장식이 혼동되지 않고 플레이어 실루엣이 배경에서 분리되는지 확인한다.

- [ ] **Step 3: 룸 연결 확인**

각 방의 출입 높이, 카메라 경계, 지름길과 비밀방 진입 위치가 전체 지도 구조와 일치하는지 확인한다.

- [ ] **Step 4: 전체 테스트 실행**

Run: Unity EditMode and PlayMode test suites

Expected: 모든 테스트 PASS.
