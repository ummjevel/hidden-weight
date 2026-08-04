using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class FractureTraversalAssetTests
    {
        const string TerrainRoot = "Assets/Art/Fracture/Environment/Terrain";

        static IEnumerable<TestCaseData> Modules()
        {
            yield return new TestCaseData("SurfaceLeft", 256, 256);
            yield return new TestCaseData("SurfaceMiddle", 1024, 256);
            yield return new TestCaseData("SurfaceRight", 256, 256);
            yield return new TestCaseData("WallTop", 256, 192);
            yield return new TestCaseData("WallMiddle", 256, 768);
            yield return new TestCaseData("WallBottom", 256, 192);
            yield return new TestCaseData("Fill", 512, 512);
        }

        [TestCaseSource(nameof(Modules))]
        public void Fracture_v3_연속형_모듈이_정확한_크기로_존재한다(
            string role, int expectedWidth, int expectedHeight)
        {
            string assetPath = $"{TerrainRoot}/Fracture_Traversal{role}_v3.png";
            string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            Assert.IsTrue(File.Exists(fullPath), assetPath);

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(texture, File.ReadAllBytes(fullPath)), assetPath);
            Assert.AreEqual(expectedWidth, texture.width, assetPath);
            Assert.AreEqual(expectedHeight, texture.height, assetPath);
            Assert.IsTrue(texture.GetPixels32().Any(pixel => pixel.a > 0), assetPath + " is empty");
            Object.DestroyImmediate(texture);
        }
    }
}
