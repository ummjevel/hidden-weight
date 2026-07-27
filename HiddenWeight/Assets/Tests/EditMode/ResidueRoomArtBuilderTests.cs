using HiddenWeight.EditorTools;
using HiddenWeight.World;
using NUnit.Framework;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class ResidueRoomArtBuilderTests
    {
        GameObject _roomObject;

        [TearDown]
        public void TearDown()
        {
            if (_roomObject != null)
                Object.DestroyImmediate(_roomObject);
        }

        [Test]
        public void BuildRoomArtCreatesThreeOrderedLayers()
        {
            _roomObject = new GameObject("Room1");
            _roomObject.AddComponent<BoxCollider2D>();
            var room = _roomObject.AddComponent<Room>();

            ResidueRoomArtBuilder.BuildRoomArt(room);

            AssertLayer("Art/Far", -30, true);
            AssertLayer("Art/Mid", -20, true);
            AssertLayer("Art/Foreground", 20, false);
        }

        void AssertLayer(
            string path,
            int sortingOrder,
            bool hasParallax)
        {
            Transform layer = _roomObject.transform.Find(path);
            Assert.That(layer, Is.Not.Null, path);

            var renderer = layer.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null, path);
            Assert.That(renderer.sprite, Is.Not.Null, path);
            Assert.That(renderer.sortingOrder, Is.EqualTo(sortingOrder));
            Assert.That(
                layer.GetComponent<ParallaxLayer>() != null,
                Is.EqualTo(hasParallax));
        }
    }
}
