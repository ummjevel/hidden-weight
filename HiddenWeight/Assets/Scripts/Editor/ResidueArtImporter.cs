using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class ResidueArtImporter
    {
        const string Root = "Assets/Art/Residue";

        [MenuItem("Hidden Weight/Art/Configure Residue Art")]
        public static void ConfigureAll()
        {
            foreach (string guid in
                AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer =
                    (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency =
                    path.Contains("_BG_Mid") ||
                    path.Contains("_FG_") ||
                    path.Contains("Atlas");

                if (path.EndsWith("Residue_TerrainAtlas.png"))
                    ConfigureGrid(importer, "Terrain", 4, 2, 1672, 941);
                else if (path.EndsWith(
                    "Residue_InteractablesAtlas.png"))
                    ConfigureGrid(
                        importer, "Interactable", 3, 2, 1536, 1024);
                else
                    importer.spriteImportMode = SpriteImportMode.Single;

                importer.SaveAndReimport();
            }
        }

        static void ConfigureGrid(
            TextureImporter importer,
            string namePrefix,
            int columns,
            int rows,
            int textureWidth,
            int textureHeight)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;

            int cellWidth = textureWidth / columns;
            int baseCellHeight = textureHeight / rows;
            var sprites = new SpriteMetaData[columns * rows];

            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                int y = row * baseCellHeight;
                int height = row == rows - 1
                    ? textureHeight - y
                    : baseCellHeight;

                for (int column = 0; column < columns; column++)
                {
                    sprites[index] = new SpriteMetaData
                    {
                        name = $"{namePrefix}_{row}_{column}",
                        rect = new Rect(
                            column * cellWidth,
                            y,
                            cellWidth,
                            height),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    index++;
                }
            }

#pragma warning disable CS0618
            importer.spritesheet = sprites;
#pragma warning restore CS0618
        }
    }
}
