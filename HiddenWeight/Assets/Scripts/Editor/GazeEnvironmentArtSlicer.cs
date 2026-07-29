using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class GazeEnvironmentArtSlicer
    {
        const string Root = "Assets/Art/Gaze/Environment";

        sealed class Sheet
        {
            public string Path;
            public int Columns;
            public int Rows;
            public Vector2 Pivot;
            public string Prefix;
        }

        static readonly Sheet[] Sheets =
        {
            new Sheet
            {
                Path = "Terrain/Gaze_TerrainTiles_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeTerrain"
            },
            new Sheet
            {
                Path = "Terrain/Gaze_Platforms_v1.png",
                Columns = 6, Rows = 3,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazePlatform"
            },
            new Sheet
            {
                Path = "Hazards/Gaze_EyeHazards_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0.5f), Prefix = "GazeHazard"
            },
            new Sheet
            {
                Path = "Interactables/Gaze_CoverObjects_v1.png",
                Columns = 6, Rows = 3,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeCover"
            },
            new Sheet
            {
                Path = "Interactables/Gaze_TransitStructures_v1.png",
                Columns = 6, Rows = 3,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeTransit"
            },
            new Sheet
            {
                Path = "Interactables/Gaze_DoorsShortcuts_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeDoor"
            },
            new Sheet
            {
                Path = "Props/Gaze_EnvironmentProps_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeProp"
            },
            new Sheet
            {
                Path = "Interactables/Gaze_AbilityObjects_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeAbility"
            },
            new Sheet
            {
                Path = "VFX/Gaze_AmbientVFX_v1.png",
                Columns = 8, Rows = 3,
                Pivot = new Vector2(0.5f, 0.5f), Prefix = "GazeAmbientVFX"
            },
        };

        [MenuItem("Hidden Weight/Art/Slice Gaze Environment Sheets")]
        public static void SliceAll()
        {
            int sliced = 0;
            foreach (var sheet in Sheets)
            {
                string path = $"{Root}/{sheet.Path}";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || texture == null)
                {
                    Debug.LogWarning(
                        $"[GazeEnvironmentArtSlicer] 시트를 찾지 못했다: {path}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.maxTextureSize =
                    Mathf.Max(2048, Mathf.Max(texture.width, texture.height));
                importer.spritesheet = BuildRects(
                    texture.width, texture.height, sheet);
                importer.SaveAndReimport();
                sliced++;
            }

            AssetDatabase.Refresh();
            Debug.Log(
                $"[GazeEnvironmentArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(
            int width,
            int height,
            Sheet sheet)
        {
            int cellWidth = width / sheet.Columns;
            int cellHeight = height / sheet.Rows;
            var sprites =
                new List<SpriteMetaData>(sheet.Columns * sheet.Rows);

            for (int row = 0; row < sheet.Rows; row++)
            {
                int y = height - (row + 1) * cellHeight;
                for (int column = 0; column < sheet.Columns; column++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name =
                            $"{sheet.Prefix}_r{row + 1}_c{column + 1}",
                        rect = new Rect(
                            column * cellWidth,
                            y,
                            cellWidth,
                            cellHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = sheet.Pivot,
                    });
                }
            }

            return sprites.ToArray();
        }
    }
}
