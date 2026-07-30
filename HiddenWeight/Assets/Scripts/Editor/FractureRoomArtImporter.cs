using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 균열 15룸의 패럴랙스 3층(원경·중경·전경) 45장에 임포트 설정을 넣는다.
    //
    // 크기 상한은 2048이다 — 원본이 1672x941이라 축소 없이 들어간다. 처음에는 메모리를
    // 아끼려 1024로 눌렀는데, 1672→1024 축소가 화면에서 바로 보이는 블러가 됐다
    // (잔재·응시는 2048이라 균열만 뿌옇게 비교됐다). 대신 압축(CompressedHQ)은 유지해
    // 무압축 대비 메모리를 1/4로 막는다 — 45장 전체가 원본 해상도 + BC7로 약 70MB.
    public static class FractureRoomArtImporter
    {
        const string Root = "Assets/Art/Fracture";

        [MenuItem("Hidden Weight/Art/Configure Fracture Room Art")]
        public static void ConfigureAll()
        {
            int configured = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsRoomLayer(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                // 원경은 불투명 RGB고 중경·전경만 알파를 쓴다(설계 문서 1절).
                importer.alphaIsTransparency =
                    path.EndsWith("_BG_Mid.png") || path.EndsWith("_FG_Overlay.png");
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;

                var standalone = importer.GetPlatformTextureSettings("Standalone");
                standalone.overridden = true;
                standalone.maxTextureSize = 2048;
                standalone.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SetPlatformTextureSettings(standalone);

                importer.SaveAndReimport();
                configured++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[FractureRoomArtImporter] 룸 레이어 {configured}장 설정 완료");
        }

        static bool IsRoomLayer(string path)
        {
            bool isRoomDirectory =
                path.StartsWith($"{Root}/Room") ||
                path.StartsWith($"{Root}/Secret");
            bool isLayer =
                path.EndsWith("_BG_Far.png") ||
                path.EndsWith("_BG_Mid.png") ||
                path.EndsWith("_FG_Overlay.png");
            return isRoomDirectory && isLayer;
        }
    }
}
