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
            var palette = AssetDatabase.LoadAssetAtPath<TraversalArtPalette>(TraversalPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<TraversalArtPalette>();
                AssetDatabase.CreateAsset(palette, TraversalPalettePath);
            }

            palette.residueSurface = FindSprite(
                "Assets/Art/Residue/Environment/Terrain/Residue_TerrainTiles_v2.png",
                "Terrain_r1_c3");
            palette.gazeSurface = FindSprite(
                "Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png",
                "GazeTerrain_r1_c3");
            palette.fractureSurface = FindSprite(
                "Assets/Art/Fracture/Environment/Terrain/Fracture_TerrainTiles_v1.png",
                "FractureTerrain_r1_c3");

            if (palette.residueSurface == null || palette.gazeSurface == null
                || palette.fractureSurface == null)
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

        public static void Build(Room room, string artRoot)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            string spritePath = $"{artRoot}/Rooms4K/{room.name}.png";
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
            background.AddComponent<CameraLockedRoomBackground>();

            if (art.GetComponent<RoomVisualCuller>() == null)
                art.gameObject.AddComponent<RoomVisualCuller>();
        }

        static void ConfigureBackgroundImport(string path)
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
