using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class GazeRoomArtImporter
    {
        const string Root = "Assets/Art/Gaze";

        [MenuItem("Hidden Weight/Art/Configure Gaze Room Art")]
        public static void ConfigureAll()
        {
            foreach (string guid in
                AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsRoomLayer(path))
                    continue;

                var importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency =
                    path.Contains("_BG_Mid") ||
                    path.Contains("_FG_Overlay");
                importer.SaveAndReimport();
            }
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
