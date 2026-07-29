using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class ChibiPlayerAbilityArtSlicer
    {
        const string Root = "Assets/Art/Player/Abilities";

        sealed class Sheet
        {
            public string FileName;
            public string[] RowClips;
        }

        static readonly Sheet[] Sheets =
        {
            new Sheet
            {
                FileName = "Player_Hush_v1.png",
                RowClips = new[]
                {
                    "HushBegin", "HushMove", "HushEnd",
                },
            },
            new Sheet
            {
                FileName = "Player_Awareness_v1.png",
                RowClips = new[]
                {
                    "AwarenessBegin", "AwarenessLoop", "AwarenessUnlock",
                },
            },
        };

        [MenuItem("Hidden Weight/Art/Slice Chibi Player Ability Sheets")]
        public static void SliceAll()
        {
            foreach (var sheet in Sheets)
            {
                string path = $"{Root}/{sheet.FileName}";
                var importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning(
                        $"[ChibiPlayerAbilityArtSlicer] 시트를 찾지 못했다: {path}");
                    continue;
                }

                importer.GetSourceTextureWidthAndHeight(
                    out int sourceWidth,
                    out int sourceHeight);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.maxTextureSize =
                    Mathf.Max(2048, Mathf.Max(sourceWidth, sourceHeight));
                importer.spritesheet =
                    BuildRects(sourceWidth, sourceHeight, sheet.RowClips);
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
        }

        static SpriteMetaData[] BuildRects(
            int width,
            int height,
            IReadOnlyList<string> rowClips)
        {
            const int columns = 6;
            const int rows = 3;
            int cellWidth = width / columns;
            int cellHeight = height / rows;
            var sprites = new List<SpriteMetaData>(columns * rows);

            for (int row = 0; row < rows; row++)
            {
                int y = height - (row + 1) * cellHeight;
                for (int column = 0; column < columns; column++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{rowClips[row]}_{column:00}",
                        rect = new Rect(
                            column * cellWidth,
                            y,
                            cellWidth,
                            cellHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f),
                    });
                }
            }

            return sprites.ToArray();
        }
    }
}
