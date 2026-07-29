using NUnit.Framework;
using UnityEditor;

namespace HiddenWeight.Tests
{
    public class FractureConceptArtImporterTests
    {
        [Test]
        public void 균열_배경_열다섯_장은_독립_스프라이트이며_1024_예산을_쓴다()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D",
                new[] { "Assets/Art/Fracture/Rooms" });
            Assert.AreEqual(15, guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.IsTrue(standalone.overridden, path + " Standalone 설정이 고정되지 않았다.");
                Assert.LessOrEqual(standalone.maxTextureSize, 1024, path + " 텍스처 예산 초과");
            }
        }
    }
}
