using HiddenWeight.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class ResidueArtImporterTests
    {
        const string FarBackgroundPath =
            "Assets/Art/Residue/Room01/Room01_BG_Far.png";
        const string TerrainAtlasPath =
            "Assets/Art/Residue/Residue_TerrainAtlas.png";
        const string InteractablesAtlasPath =
            "Assets/Art/Residue/Residue_InteractablesAtlas.png";

        [Test]
        public void ConfigureAllUsesProjectSpriteScale()
        {
            ResidueArtImporter.ConfigureAll();

            var importer =
                (TextureImporter)AssetImporter.GetAtPath(FarBackgroundPath);

            Assert.That(importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        }

        [Test]
        public void ConfigureAllSlicesTerrainAtlasIntoEightSprites()
        {
            ResidueArtImporter.ConfigureAll();

            var importer =
                (TextureImporter)AssetImporter.GetAtPath(TerrainAtlasPath);

            Assert.That(importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritesheet, Has.Length.EqualTo(8));
        }

        [Test]
        public void ConfigureAllSlicesInteractablesAtlasIntoSixSprites()
        {
            ResidueArtImporter.ConfigureAll();

            var importer =
                (TextureImporter)AssetImporter.GetAtPath(
                    InteractablesAtlasPath);

            Assert.That(importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritesheet, Has.Length.EqualTo(6));
        }

    }
}
