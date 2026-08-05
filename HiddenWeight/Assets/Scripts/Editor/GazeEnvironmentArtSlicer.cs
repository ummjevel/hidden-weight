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

            // 이 칸들은 균열 지형 시트와 달리 셀 안에 여백을 두고 그려졌다(장식 소품으로
            // 그려져서 그렇다). 이어 붙이는 발판/바닥 타일로 재사용하는 칸만 골라 알파
            // 기준으로 실제 그림 경계에 딱 맞게 다시 자른다 — 안 그러면 셀 경계 기준
            // 균등 배치 시 조각 사이에 투명 여백만큼 뜬 구간이 생긴다(응시 다리·바닥에서
            // 실제로 그랬다). "r{row}_c{col}" 형식.
            public string[] TightenCells;
        }

        static readonly Sheet[] Sheets =
        {
            new Sheet
            {
                Path = "Terrain/Gaze_TerrainTiles_v1.png",
                Columns = 6, Rows = 4,
                Pivot = new Vector2(0.5f, 0f), Prefix = "GazeTerrain",
                TightenCells = new[] { "r2_c1", "r2_c4", "r2_c5", "r2_c6", "r3_c1", "r3_c2", "r3_c3" },
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
                var rects = BuildRects(texture.width, texture.height, sheet);

                if (sheet.TightenCells != null && sheet.TightenCells.Length > 0)
                {
                    // 알파 여백을 읽으려면 픽셀에 접근해야 하는데, 임포터를 isReadable로
                    // 바꿔 한 번 리임포트한 뒤 같은 파일에 spritesheet를 다시 대입해
                    // 리임포트하면 두 번째 대입이 조용히 무시됐다(이름이 같아 "이미 있음"
                    // 으로 보는 듯하다 — 저장 후에도 rect가 셀 크기 그대로 남았다).
                    // 그래서 임포터와 무관하게 원본 PNG 바이트를 직접 읽어 메모리에서만
                    // 픽셀을 본다 — 에셋 임포트는 이후 단 한 번만 일어난다.
                    string fullPath = System.IO.Path.Combine(
                        Application.dataPath, path.Substring("Assets/".Length));
                    var bytes = System.IO.File.ReadAllBytes(fullPath);
                    var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    raw.LoadImage(bytes);
                    TightenRects(raw, rects, sheet.TightenCells);
                    Object.DestroyImmediate(raw);
                }

                importer.spritesheet = rects;
                importer.SaveAndReimport();

                // SaveAndReimport만으로는 .meta에는 이름이 들어가는데 실제 서브에셋이
                // 갱신되지 않는 경우가 있다(엄폐물 시트가 실제로 그랬다 — meta에 이름 18개가
                // 있는데 씬 참조가 0이었다). 강제 재임포트로 확실히 반영시킨다.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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

        // rects 안의 지정된 칸들을 알파>임계값인 픽셀의 실제 경계로 다시 자른다. 피벗이
        // bottom-center라 셀 경계 기준으로 두면 그림마다 다른 여백만큼 바닥선이 어긋나고
        // (세로 여백 차이), 가로로 이어 붙이면 그림 사이에 투명 틈이 생긴다(가로 여백
        // 차이). 두 문제 다 셀이 아니라 그림 자체에 맞춘 rect면 사라진다.
        static void TightenRects(Texture2D texture, SpriteMetaData[] rects, string[] cellNames)
        {
            var wanted = new HashSet<string>(cellNames);
            for (int i = 0; i < rects.Length; i++)
            {
                // 이름은 "{Prefix}_r{row}_c{col}" — Prefix를 떼고 나머지만 비교한다.
                string suffix = rects[i].name.Substring(rects[i].name.IndexOf('_') + 1);
                if (!wanted.Contains(suffix)) continue;

                Rect cell = rects[i].rect;
                var pixels = texture.GetPixels(
                    (int)cell.x, (int)cell.y, (int)cell.width, (int)cell.height);

                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                int w = (int)cell.width, h = (int)cell.height;
                const float alphaThreshold = 10f / 255f;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a <= alphaThreshold) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < minX || maxY < minY) continue; // 완전히 빈 칸이면 건드리지 않는다

                rects[i].rect = new Rect(
                    cell.x + minX, cell.y + minY,
                    maxX - minX + 1, maxY - minY + 1);
            }
        }
    }
}
