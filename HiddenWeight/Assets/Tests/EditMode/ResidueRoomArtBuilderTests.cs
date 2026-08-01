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
        public void BuildRoomArtCreatesOneRoomFittedBackgroundOnly()
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
            Assert.That(background.GetComponent<RoomFittedBackground>(), Is.Not.Null);

            // 배경은 카메라가 아니라 방에 붙어 있어야 한다. 굽는 시점에 이미 방 경계를
            // 덮도록 맞춰져 있는지 본다 — 그래야 에디터에서도 실제 구도가 보인다.
            Bounds bounds = room.WorldBounds;
            Assert.That(background.position.x, Is.EqualTo(bounds.center.x).Within(0.001f));
            Assert.That(background.position.y, Is.EqualTo(bounds.center.y).Within(0.001f));

            Vector2 covered = renderer.sprite.bounds.size * background.localScale.x;
            Assert.That(background.localScale.x, Is.EqualTo(background.localScale.y).Within(0.0001f));
            Assert.That(covered.x, Is.GreaterThanOrEqualTo(bounds.size.x - 0.001f));
            Assert.That(covered.y, Is.GreaterThanOrEqualTo(bounds.size.y - 0.001f));

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
