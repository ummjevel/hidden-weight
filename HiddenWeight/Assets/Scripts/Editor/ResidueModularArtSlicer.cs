using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 생성 시트의 셀 안 여백을 그대로 Sprite rect에 넣으면 반복 배치할 때 빈 틈이 생긴다.
    // 각 3x3 셀의 알파 경계를 찾아 실제 그림만 잘라, 늘리지 않고 이어 붙일 수 있게 한다.
    public static class ResidueModularArtSlicer
    {
        const string TerrainPath =
            "Assets/Art/Residue/Environment/Terrain/ModularV3/Residue_ModularTerrain_v3.png";
        const string WallsPath =
            "Assets/Art/Residue/Environment/Terrain/ModularV3/Residue_ModularWallsStairs_v3.png";

        static readonly string[] TerrainNames =
        {
            "ResidueGroundLeft", "ResidueGroundMiddle", "ResidueGroundRight",
            "ResiduePlatformShort", "ResiduePlatformMedium", "ResiduePlatformLong",
            "ResidueCliffLeft", "ResidueGroundFill", "ResidueCliffRight",
        };

        static readonly string[] WallNames =
        {
            "ResidueWallBottom", "ResidueWallMiddle", "ResidueWallTop",
            "ResidueStairUpRight", "ResidueStairUpLeft", "ResidueStairShort",
            "ResidueCornerRight", "ResidueCornerLeft", "ResidueClimbPillar",
        };

        [MenuItem("Hidden Weight/Art/Slice Residue Modular V3")]
        public static void SliceAll()
        {
            Slice(TerrainPath, TerrainNames, new Vector2(0.5f, 0f));
            Slice(WallsPath, WallNames, new Vector2(0.5f, 0f));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[ResidueModularArtSlicer] 모듈형 잔재 환경 시트 분할 완료");
        }

        static void Slice(string path, string[] names, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"잔재 모듈 시트를 찾지 못했다: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                throw new System.InvalidOperationException($"잔재 모듈 텍스처를 읽지 못했다: {path}");

            SpriteRect[] rects = BuildAlphaTrimmedRects(texture, names, pivot);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static SpriteRect[] BuildAlphaTrimmedRects(
            Texture2D texture, IReadOnlyList<string> names, Vector2 pivot)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels32();
            var sprites = new List<SpriteRect>(9);

            for (int topRow = 0; topRow < 3; topRow++)
            {
                int y0 = height - (topRow + 1) * height / 3;
                int y1 = height - topRow * height / 3;
                for (int col = 0; col < 3; col++)
                {
                    int x0 = col * width / 3;
                    int x1 = (col + 1) * width / 3;
                    int minX = x1;
                    int minY = y1;
                    int maxX = x0;
                    int maxY = y0;

                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        if (pixels[y * width + x].a <= 10) continue;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }

                    if (maxX < minX || maxY < minY)
                        throw new System.InvalidOperationException(
                            $"{texture.name} {topRow + 1}행 {col + 1}열이 비어 있다.");

                    const int padding = 2;
                    minX = Mathf.Max(x0, minX - padding);
                    minY = Mathf.Max(y0, minY - padding);
                    maxX = Mathf.Min(x1 - 1, maxX + padding);
                    maxY = Mathf.Min(y1 - 1, maxY + padding);

                    int index = topRow * 3 + col;
                    sprites.Add(new SpriteRect
                    {
                        name = names[index],
                        rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        spriteID = GUID.Generate(),
                    });
                }
            }

            return sprites.ToArray();
        }
    }
}
