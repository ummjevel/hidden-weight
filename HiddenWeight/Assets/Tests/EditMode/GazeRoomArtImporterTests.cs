using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class GazeRoomArtImporterTests
    {
        static IEnumerable<string> LayerPaths()
        {
            for (int room = 1; room <= 12; room++)
            {
                string name = $"Room{room:00}";
                yield return $"Assets/Art/Gaze/{name}/{name}_BG_Far.png";
                yield return $"Assets/Art/Gaze/{name}/{name}_BG_Mid.png";
                yield return $"Assets/Art/Gaze/{name}/{name}_FG_Overlay.png";
            }

            for (int secret = 1; secret <= 3; secret++)
            {
                string name = $"Secret{secret:00}";
                yield return $"Assets/Art/Gaze/{name}/{name}_BG_Far.png";
                yield return $"Assets/Art/Gaze/{name}/{name}_BG_Mid.png";
                yield return $"Assets/Art/Gaze/{name}/{name}_FG_Overlay.png";
            }
        }

        [Test]
        public void AllRoomLayersUseRuntimeBackgroundImportSettings()
        {
            int checkedCount = 0;

            foreach (string path in LayerPaths())
            {
                var importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;

                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.spritePixelsPerUnit,
                    Is.EqualTo(32f), path);
                Assert.That(importer.filterMode,
                    Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.wrapMode,
                    Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    path);

                bool needsTransparency =
                    path.Contains("_BG_Mid") ||
                    path.Contains("_FG_Overlay");
                Assert.That(importer.alphaIsTransparency,
                    Is.EqualTo(needsTransparency), path);

                checkedCount++;
            }

            Assert.That(checkedCount, Is.EqualTo(45));
        }
    }
}
