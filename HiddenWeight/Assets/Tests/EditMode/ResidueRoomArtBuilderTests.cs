using HiddenWeight.EditorTools;
using HiddenWeight.World;
using NUnit.Framework;
using UnityEditor;
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
        public void BuildRoomArtCreatesOneCameraLockedBackgroundOnly()
        {
            _roomObject = new GameObject("Room01");
            _roomObject.AddComponent<BoxCollider2D>();
            var room = _roomObject.AddComponent<Room>();

            ResidueRoomArtBuilder.BuildRoomArt(room);

            Transform background = _roomObject.transform.Find("Art/RoomBackground");
            Assert.That(background, Is.Not.Null);

            var renderer = background.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(renderer.sprite),
                Does.Contain("/Rooms4K/"));
            Assert.That(renderer.sortingOrder, Is.EqualTo(-30));
            Assert.That(background.GetComponent("CameraLockedRoomBackground"), Is.Not.Null);

            foreach (string forbidden in new[]
                     {
                         "Far", "Mid", "Foreground",
                         "BG_Far", "BG_Mid", "FG_Overlay"
                     })
                Assert.That(_roomObject.transform.Find("Art/" + forbidden), Is.Null);
        }

        [Test]
        public void BuildRoomArtRemovesLegacyForegroundLayer()
        {
            _roomObject = new GameObject("Room01");
            _roomObject.AddComponent<BoxCollider2D>();
            var room = _roomObject.AddComponent<Room>();

            // 이전 버전이 만들어 둔 전경 레이어가 남아 있는 씬을 흉내 낸다.
            var art = new GameObject("Art");
            art.transform.SetParent(_roomObject.transform, false);
            var legacy = new GameObject("Foreground");
            legacy.transform.SetParent(art.transform, false);

            ResidueRoomArtBuilder.BuildRoomArt(room);

            Assert.That(_roomObject.transform.Find("Art/Foreground"), Is.Null);
            Assert.That(_roomObject.transform.Find("Art/RoomBackground"), Is.Not.Null);
        }
    }
}
