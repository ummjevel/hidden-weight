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
            const string fractureSheet =
                "Assets/Art/Fracture/Environment/Terrain/Fracture_TerrainTiles_v2.png";
            palette.fractureSurface = FindSprite(fractureSheet, "FractureTerrain_r1_c3");

            if (palette.residueSurface == null || palette.gazeSurface == null
                || palette.fractureSurface == null)
                throw new InvalidOperationException("지역별 보행 바닥 스프라이트를 찾지 못했다.");

            palette.fractureTiles = BuildFractureTileSet(fractureSheet);

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
