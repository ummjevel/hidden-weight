using System;
using HiddenWeight.World;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    public static class SingleRoomBackgroundBuilder
    {
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
