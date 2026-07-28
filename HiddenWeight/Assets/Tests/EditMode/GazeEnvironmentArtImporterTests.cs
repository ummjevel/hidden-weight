using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class GazeEnvironmentArtImporterTests
    {
        readonly (string path, int sprites)[] _atlases =
        {
            ("Assets/Art/Gaze/Environment/Terrain/Gaze_TerrainTiles_v1.png", 24),
            ("Assets/Art/Gaze/Environment/Terrain/Gaze_Platforms_v1.png", 18),
            ("Assets/Art/Gaze/Environment/Hazards/Gaze_EyeHazards_v1.png", 24),
            ("Assets/Art/Gaze/Environment/Interactables/Gaze_CoverObjects_v1.png", 18),
            ("Assets/Art/Gaze/Environment/Interactables/Gaze_TransitStructures_v1.png", 18),
            ("Assets/Art/Gaze/Environment/Interactables/Gaze_DoorsShortcuts_v1.png", 24),
            ("Assets/Art/Gaze/Environment/Props/Gaze_EnvironmentProps_v1.png", 24),
            ("Assets/Art/Gaze/Environment/Interactables/Gaze_AbilityObjects_v1.png", 24),
            ("Assets/Art/Gaze/Environment/VFX/Gaze_AmbientVFX_v1.png", 24),
        };

        [Test]
        public void GazeEnvironmentAtlasesAreReadyForGridSpriteUse()
        {
            foreach (var (path, sprites) in _atlases)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Multiple), path);
                Assert.That(importer.spritesheet,
                    Has.Length.EqualTo(sprites), path);
                Assert.That(importer.spritePixelsPerUnit,
                    Is.EqualTo(32f), path);
                Assert.That(importer.filterMode,
                    Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            }
        }
    }
}
