using System.Collections.Generic;
using System.Linq;
using HiddenWeight.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class ChibiPlayerArtImporterTests
    {
        sealed class SheetExpectation
        {
            public string Path;
            public int Count;
            public int CellWidth;
            public int CellHeight;
        }

        static readonly SheetExpectation[] Sheets =
        {
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png",
                Count = 8, CellWidth = 384, CellHeight = 512,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png",
                Count = 24, CellWidth = 256, CellHeight = 256,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png",
                Count = 24, CellWidth = 256, CellHeight = 256,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png",
                Count = 12, CellWidth = 362, CellHeight = 362,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png",
                Count = 12, CellWidth = 362, CellHeight = 362,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png",
                Count = 18, CellWidth = 256, CellHeight = 341,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Player/Abilities/Player_Hush_v1.png",
                Count = 18, CellWidth = 256, CellHeight = 256,
            },
            new SheetExpectation
            {
                Path = "Assets/Art/Player/Abilities/Player_Awareness_v1.png",
                Count = 18, CellWidth = 256, CellHeight = 256,
            },
        };

        [Test]
        public void AllChibiPlayerSheetsUseRuntimeImportSettingsAndBottomCenterPivot()
        {
            foreach (var expected in Sheets)
            {
                var importer =
                    AssetImporter.GetAtPath(expected.Path) as TextureImporter;

                Assert.That(importer, Is.Not.Null, expected.Path);
                Assert.That(importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite), expected.Path);
                Assert.That(importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Multiple), expected.Path);
                Assert.That(importer.spritePixelsPerUnit,
                    Is.EqualTo(32f), expected.Path);
                Assert.That(importer.filterMode,
                    Is.EqualTo(FilterMode.Bilinear), expected.Path);
                Assert.That(importer.mipmapEnabled, Is.False, expected.Path);
                Assert.That(importer.wrapMode,
                    Is.EqualTo(TextureWrapMode.Clamp), expected.Path);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    expected.Path);
                Assert.That(importer.alphaIsTransparency, Is.True,
                    expected.Path);

                var sprites = importer.spritesheet;
                Assert.That(sprites, Has.Length.EqualTo(expected.Count),
                    expected.Path);

                foreach (var sprite in sprites)
                {
                    Assert.That(sprite.rect.width,
                        Is.EqualTo(expected.CellWidth), sprite.name);
                    Assert.That(sprite.rect.height,
                        Is.EqualTo(expected.CellHeight), sprite.name);
                    Assert.That(sprite.pivot.x,
                        Is.EqualTo(0.5f).Within(0.001f), sprite.name);
                    Assert.That(sprite.pivot.y,
                        Is.EqualTo(0f).Within(0.001f), sprite.name);
                }
            }
        }

        [Test]
        public void ActionsAndWallCellsCoverTheirEntireAtlases()
        {
            foreach (var path in new[]
                     {
                         Sheets[3].Path,
                         Sheets[4].Path,
                     })
            {
                var importer =
                    (TextureImporter)AssetImporter.GetAtPath(path);
                var sprites = importer.spritesheet;

                Assert.That(sprites.Min(sprite => sprite.rect.x),
                    Is.EqualTo(0f), path);
                Assert.That(sprites.Min(sprite => sprite.rect.y),
                    Is.EqualTo(0f), path);
                Assert.That(sprites.Max(sprite => sprite.rect.xMax),
                    Is.EqualTo(2172f), path);
                Assert.That(sprites.Max(sprite => sprite.rect.yMax),
                    Is.EqualTo(724f), path);
            }
        }

        [Test]
        public void AbilitySheetsUseStableAnimationFrameNames()
        {
            AssertNames(Sheets[6].Path, new[]
            {
                "HushBegin", "HushMove", "HushEnd",
            });
            AssertNames(Sheets[7].Path, new[]
            {
                "AwarenessBegin", "AwarenessLoop", "AwarenessUnlock",
            });
        }

        [Test]
        public void ResidueRoomImporterDoesNotOverwritePlayerSheetSettings()
        {
            foreach (var expected in Sheets.Take(6))
            {
                var importer =
                    (TextureImporter)AssetImporter.GetAtPath(expected.Path);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            ResidueArtImporter.ConfigureAll();

            foreach (var expected in Sheets.Take(6))
            {
                var importer =
                    (TextureImporter)AssetImporter.GetAtPath(expected.Path);

                Assert.That(importer.alphaIsTransparency, Is.True,
                    expected.Path);
                Assert.That(importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Multiple), expected.Path);
            }
        }

        static void AssertNames(
            string path,
            IReadOnlyList<string> rowNames)
        {
            var importer =
                (TextureImporter)AssetImporter.GetAtPath(path);
            var actual = importer.spritesheet
                .Select(sprite => sprite.name)
                .ToArray();
            var expected = rowNames
                .SelectMany(row => Enumerable.Range(0, 6)
                    .Select(frame => $"{row}_{frame:00}"))
                .ToArray();

            Assert.That(actual, Is.EqualTo(expected), path);
        }
    }
}
