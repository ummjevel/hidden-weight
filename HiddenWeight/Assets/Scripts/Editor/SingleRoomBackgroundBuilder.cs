using System;
using HiddenWeight.World;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class SingleRoomBackgroundBuilder
    {
        public enum BackgroundSizing
        {
            RoomFixed,
            CameraFollow,
        }

        const string TraversalPalettePath = "Assets/Resources/TraversalArtPalette.asset";

        [MenuItem("Hidden Weight/Art/Build Traversal Art Palette")]
        public static void BuildTraversalArtPalette()
        {
            const string prologueSurfacePath =
                "Assets/Art/Prologue/Environment/Prologue_TraversalSurface_v2.png";
            ConfigureTraversalSurfaceImport(prologueSurfacePath);
            const string prologueWallPath =
                "Assets/Art/Prologue/Environment/Prologue_TraversalWall_v2.png";
            ConfigureTraversalSurfaceImport(prologueWallPath);
            const string prologueFillPath =
                "Assets/Art/Prologue/Environment/Prologue_TraversalFill_v1.png";
            ConfigureTraversalFillImport(prologueFillPath);

            var palette = AssetDatabase.LoadAssetAtPath<TraversalArtPalette>(TraversalPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<TraversalArtPalette>();
                AssetDatabase.CreateAsset(palette, TraversalPalettePath);
            }

            palette.prologueSurface = AssetDatabase.LoadAssetAtPath<Sprite>(prologueSurfacePath);
            palette.prologueWall = AssetDatabase.LoadAssetAtPath<Sprite>(prologueWallPath);
            palette.prologueFill = AssetDatabase.LoadAssetAtPath<Sprite>(prologueFillPath);
            palette.residueSurface = FindSprite(
                "Assets/Art/Residue/Environment/Terrain/Residue_TerrainTiles_v2.png",
                "Terrain_r1_c3");
            const string residueTerrainV3 =
                "Assets/Art/Residue/Environment/Terrain/ModularV3/Residue_ModularTerrain_v3.png";
            const string residueWallsV3 =
                "Assets/Art/Residue/Environment/Terrain/ModularV3/Residue_ModularWallsStairs_v3.png";
            palette.residueGroundLeft = FindSprite(residueTerrainV3, "ResidueGroundLeft");
            palette.residueGroundMiddle = FindSprite(residueTerrainV3, "ResidueGroundMiddle");
            palette.residueGroundRight = FindSprite(residueTerrainV3, "ResidueGroundRight");
            palette.residueGroundFill = FindSprite(residueTerrainV3, "ResidueGroundFill");
            palette.residuePlatformShort = FindSprite(residueTerrainV3, "ResiduePlatformShort");
            palette.residuePlatformMedium = FindSprite(residueTerrainV3, "ResiduePlatformMedium");
            palette.residuePlatformLong = FindSprite(residueTerrainV3, "ResiduePlatformLong");
            palette.residueWallMiddle = FindSprite(residueWallsV3, "ResidueWallMiddle");
            palette.residueClimbPillar = FindSprite(residueWallsV3, "ResidueClimbPillar");
            const string gazeSheet =
                "Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png";
            palette.gazeSurface = FindSprite(gazeSheet, "GazeTerrain_r1_c3");
            const string fractureSheet =
                "Assets/Art/Fracture/Environment/Terrain/Fracture_TerrainTiles_v2.png";
            palette.fractureSurface = FindSprite(fractureSheet, "FractureTerrain_r1_c3");
            const string fractureTerrainRoot = "Assets/Art/Fracture/Environment/Terrain";
            string FractureModulePath(string role) =>
                $"{fractureTerrainRoot}/Fracture_Traversal{role}_v3.png";
            foreach (string role in new[]
                     {
                         "SurfaceLeft", "SurfaceMiddle", "SurfaceRight",
                         "WallTop", "WallMiddle", "WallBottom",
                     })
                ConfigureTraversalSurfaceImport(FractureModulePath(role));
            ConfigureTraversalFillImport(FractureModulePath("Fill"));

            palette.fractureContinuous = new ContinuousTerrainSet
            {
                surfaceLeft = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("SurfaceLeft")),
                surfaceMiddle = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("SurfaceMiddle")),
                surfaceRight = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("SurfaceRight")),
                wallTop = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("WallTop")),
                wallMiddle = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("WallMiddle")),
                wallBottom = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("WallBottom")),
                fill = AssetDatabase.LoadAssetAtPath<Sprite>(FractureModulePath("Fill")),
            };

            if (palette.prologueSurface == null || palette.prologueWall == null || palette.prologueFill == null
                || palette.residueSurface == null
                || palette.gazeSurface == null
                || palette.fractureSurface == null || !palette.HasResidueModularV3
                || palette.fractureContinuous == null || !palette.fractureContinuous.IsComplete)
                throw new InvalidOperationException("지역별 보행 바닥 스프라이트를 찾지 못했다.");

            palette.fractureTiles = BuildFractureTileSet(fractureSheet);
            palette.gazeTiles = BuildGazeTileSet(gazeSheet);

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            Debug.Log("[SingleRoomBackgroundBuilder] 보행 바닥 팔레트 생성 완료");
        }

        // 시트의 행·열 배정은 `build_fracture_terrain_v2.py`가 정한다.
        //   r1 바닥 윗면   좌 끝단 / 중간 4종 / 우 끝단
        //   r2 세로 벽면   켜 4종 + 모서리 2종
        //   r3 천장·아랫면 좌 / 중 / 우 …
        static TerrainTileSet BuildFractureTileSet(string sheet)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(sheet);
            var missing = new System.Collections.Generic.List<string>();
            Sprite At(int row, int column)
            {
                string wanted = $"FractureTerrain_r{row}_c{column}";
                foreach (var asset in all)
                    if (asset is Sprite sprite && sprite.name == wanted)
                        return sprite;
                missing.Add(wanted);
                return null;
            }

            var set = new TerrainTileSet
            {
                topLeft = At(1, 1),
                topMid = new[] { At(1, 2), At(1, 3), At(1, 4), At(1, 5) },
                topRight = At(1, 6),
                wallCourse = new[] { At(2, 1), At(2, 2), At(2, 3), At(2, 4) },
                ceilingLeft = At(3, 1),
                ceilingMid = At(3, 2),
                ceilingRight = At(3, 3),
            };

            if (!set.IsComplete)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var asset in all)
                    if (asset is Sprite sprite) names.Add(sprite.name);
                throw new InvalidOperationException(
                    $"균열 지형 타일셋이 불완전하다: {sheet}\n"
                    + $"  못 찾은 칸: {string.Join(", ", missing)}\n"
                    + $"  시트가 실제로 가진 스프라이트 {names.Count}개: "
                    + string.Join(", ", names));
            }

            return set;
        }

        // 응시 시트(6x4)는 균열과 달리 역할별로 깔끔하게 나뉘어 있지 않다 — 모서리·기둥·
        // 계단·아치가 뒤섞여 있다. 바닥 윗면(꺾이지 않는 순수 평판)과 코너(옆이 무너져
        // 내려가는 모서리)만 골라 topLeft/topMid/topRight로 쓰고, 세로 창살판을 벽 켜로,
        // 물방울이 늘어지는 조각을 천장 밑면으로 재사용한다. r1_c3/r1_c4는 원래 2칸짜리
        // 장식 하나가 6칸 그리드에 반으로 잘려 들어간 조각이라 반복용에서 제외했다.
        //
        // topMid은 2행(r2c*) 안에서만 고른다 — 셀 크기는 전부 8x8유닛으로 같지만 그림이
        // 셀 안에서 시작하는 위치(위쪽 여백)는 조각마다 다르고, Sprite.bounds는 알파를
        // 무시한 셀 전체 크기라 이 여백 차이가 그대로 반영된다. 1행 조각(예: r1_c1은
        // 위쪽 여백이 2행보다 약 2유닛 더 크다)을 섞으면 코너와 중간 조각의 바닥면이
        // 어긋나 보인다. 2행은 전부 여백이 53px 안팎으로 같아서 이 안에서만 고르면
        // 이음매가 맞는다 — 대신 "꺾이지 않는" 평판은 r2_c5/r2_c6 두 개뿐이라 반복
        // 무늬 종류가 균열(4종)보다 적다.
        static TerrainTileSet BuildGazeTileSet(string sheet)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(sheet);
            var missing = new System.Collections.Generic.List<string>();
            Sprite At(int row, int column)
            {
                string wanted = $"GazeTerrain_r{row}_c{column}";
                foreach (var asset in all)
                    if (asset is Sprite sprite && sprite.name == wanted)
                        return sprite;
                missing.Add(wanted);
                return null;
            }

            var set = new TerrainTileSet
            {
                topLeft = At(2, 1),
                topMid = new[] { At(2, 5), At(2, 6) },
                topRight = At(2, 4),
                wallCourse = new[] { At(3, 1), At(3, 2), At(3, 3) },
                ceilingMid = At(2, 6),
            };

            if (!set.IsComplete)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var asset in all)
                    if (asset is Sprite sprite) names.Add(sprite.name);
                throw new InvalidOperationException(
                    $"응시 지형 타일셋이 불완전하다: {sheet}\n"
                    + $"  못 찾은 칸: {string.Join(", ", missing)}\n"
                    + $"  시트가 실제로 가진 스프라이트 {names.Count}개: "
                    + string.Join(", ", names));
            }

            return set;
        }

        static Sprite FindSprite(string path, string spriteName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            return null;
        }

        static void ConfigureTraversalSurfaceImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void ConfigureTraversalFillImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        public static void Build(Room room, string artRoot,
            BackgroundSizing sizing = BackgroundSizing.RoomFixed)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            string spritePath = ResidueBackgroundPath(room, artRoot)
                ?? $"{artRoot}/Rooms4K/{room.name}.png";
            ConfigureBackgroundImport(spritePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                throw new InvalidOperationException(
                    $"Room background sprite is missing: {spritePath}");

            Transform art = room.transform.Find("Art");
            if (art == null)
            {
                var artObject = new GameObject("Art");
                artObject.transform.SetParent(room.transform, false);
                art = artObject.transform;
            }

            for (int i = art.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(art.GetChild(i).gameObject);

            var background = new GameObject("RoomBackground");
            background.transform.SetParent(art, false);
            background.transform.position = room.WorldBounds.center;

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -30;
            var locked = background.AddComponent<CameraLockedRoomBackground>();
            if (sizing == BackgroundSizing.RoomFixed)
            {
                // 카메라를 따라 매 프레임 다시 스케일하는 대신 방 크기에 한 번만 맞춘다 —
                // 그래야 그림이 방 안에서 고정되어 실제 오브젝트와의 크기 관계가 일정해진다.
                var size = room.WorldBounds.size;
                locked.ConfigureWorldSize(new Vector2(size.x, size.y));
            }
            if (artRoot.EndsWith("/Prologue", StringComparison.Ordinal))
            {
                var serialized = new SerializedObject(locked);
                serialized.FindProperty("backgroundTint").colorValue =
                    new Color(0.9f, 0.92f, 1f, 0.9f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (art.GetComponent<RoomVisualCuller>() == null)
                art.gameObject.AddComponent<RoomVisualCuller>();
        }

        public static string ResidueBackgroundPath(Room room, string artRoot)
        {
            if (room == null || !artRoot.EndsWith("/Residue", StringComparison.Ordinal))
                return null;

            string roomName = room.name;
            if (roomName.Contains("R08") || roomName.Contains("R09")
                || roomName.Contains("Room08") || roomName.Contains("Room09"))
                return "Assets/Art/Residue/Backgrounds/V3/Residue_Background_Shaft_v3.png";
            if (roomName.Contains("R10") || roomName.Contains("R11")
                || roomName.Contains("R12") || roomName.Contains("Room10")
                || roomName.Contains("Room11") || roomName.Contains("Room12")
                || roomName.Contains("S3") || roomName.Contains("Secret03"))
                return "Assets/Art/Residue/Backgrounds/V3/Residue_Background_BellTower_v3.png";
            return "Assets/Art/Residue/Backgrounds/V3/Residue_Background_Bridge_v3.png";
        }

        public static void ConfigureBackgroundImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            bool changed =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.spritePixelsPerUnit != 100f ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                importer.maxTextureSize != 4096 ||
                importer.textureCompression !=
                    TextureImporterCompression.Uncompressed;
            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
