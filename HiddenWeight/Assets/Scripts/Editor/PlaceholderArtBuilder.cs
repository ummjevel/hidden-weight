using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // Assets/Art/Placeholder/에 단색 PNG 10장을 코드로 생성하고 스프라이트로 임포트 설정한다.
    // 실제 아트가 들어오기 전까지 게임을 플레이 가능한 상태로 만들기 위한 자리표시자다.
    public static class PlaceholderArtBuilder
    {
        const string Folder = "Assets/Art/Placeholder";

        // (파일명, 너비, 높이, 색 16진수) — 브리핑 표 그대로.
        static readonly (string name, int width, int height, string hex)[] Sprites =
        {
            ("Player", 32, 48, "#C8B8E8"),
            ("Enemy", 32, 32, "#FFFFFF"),
            ("Tile", 32, 32, "#808080"),
            ("Platform", 96, 16, "#A0A0A0"),
            ("Fragment", 16, 16, "#FFFFFF"),
            ("Gate", 32, 96, "#303048"),
            ("Eye", 48, 48, "#9060C0"),
            ("Candle", 16, 32, "#E8A050"),
            ("Bed", 192, 64, "#685878"),
            ("Wall", 256, 192, "#484058"),
        };

        public static void Run()
        {
            EnsureFolder();

            foreach (var s in Sprites)
            {
                WritePng(s.name, s.width, s.height, s.hex);
            }

            // PNG 파일들이 디스크에 먼저 보이도록 임포트를 강제한 뒤에야 TextureImporter를 잡을 수 있다.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var s in Sprites)
            {
                ConfigureImporter($"{Folder}/{s.name}.png");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderArtBuilder] 스프라이트 {Sprites.Length}개 생성 완료");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
            {
                AssetDatabase.CreateFolder("Assets", "Art");
            }
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Placeholder");
            }
        }

        static void WritePng(string name, int width, int height, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            Color32 c32 = color;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c32;
            tex.SetPixels32(pixels);
            tex.Apply();

            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            // Unity 에디터 스크립트에서 상대 경로는 프로젝트 루트(Assets의 부모) 기준으로 풀린다.
            File.WriteAllBytes($"{Folder}/{name}.png", bytes);
        }

        static void ConfigureImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32; // 설계 문서 2절 기준(Pixels Per Unit)과 일치
            importer.filterMode = FilterMode.Point; // 픽셀아트가 흐려지지 않도록
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
