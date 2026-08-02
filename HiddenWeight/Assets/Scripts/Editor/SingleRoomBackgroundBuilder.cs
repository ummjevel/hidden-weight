using System;
using HiddenWeight.World;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class SingleRoomBackgroundBuilder
    {
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
            palette.gazeSurface = FindSprite(
                "Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png",
                "GazeTerrain_r1_c3");
            palette.fractureSurface = FindSprite(
                "Assets/Art/Fracture/Environment/Terrain/Fracture_TerrainTiles_v1.png",
                "FractureTerrain_r1_c3");

            if (palette.prologueSurface == null || palette.prologueWall == null || palette.prologueFill == null
                || palette.residueSurface == null
                || palette.gazeSurface == null
                || palette.fractureSurface == null || !palette.HasResidueModularV3)
                throw new InvalidOperationException("지역별 보행 바닥 스프라이트를 찾지 못했다.");

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            Debug.Log("[SingleRoomBackgroundBuilder] 보행 바닥 팔레트 생성 완료");
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

        public static void Build(Room room, string artRoot)
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
