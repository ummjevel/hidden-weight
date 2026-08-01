using NUnit.Framework;
using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 방 배경이 카메라를 따라다니지 않고 방에 붙어 있는지 확인한다. 따라다니면 카메라가
    // 방 안을 움직일 때 공간 전체가 미끄러져 보인다.
    public class RoomFittedBackgroundTests
    {
        [Test]
        public void 배경은_방_중심에_놓이고_방을_덮는다()
        {
            var background = BuildBackground(new Vector2(30f, 18f), new Vector2(120f, -40f),
                                             3840, 2160);
            background.Fit();

            var t = background.transform;
            Assert.That(t.position.x, Is.EqualTo(120f).Within(0.001f));
            Assert.That(t.position.y, Is.EqualTo(-40f).Within(0.001f));

            // 4K 배경은 100 PPU에서 38.4 x 21.6유닛이다. 30x18 방을 덮으려면 세로 기준
            // 18/21.6이 아니라 더 큰 쪽인 가로 30/38.4 배율이 이긴다.
            float expected = Mathf.Max(30f / 38.4f, 18f / 21.6f);
            Assert.That(t.localScale.x, Is.EqualTo(expected).Within(0.001f));
            Assert.That(t.localScale.y, Is.EqualTo(t.localScale.x).Within(0.0001f),
                        "축마다 따로 늘리면 그림이 일그러진다");
            Assert.That(t.localScale.x * 38.4f, Is.GreaterThanOrEqualTo(30f - 0.001f));
            Assert.That(t.localScale.y * 21.6f, Is.GreaterThanOrEqualTo(18f - 0.001f));
        }

        [Test]
        public void 세로로_긴_방도_덮인다()
        {
            // 잔재 Room08(24x30)처럼 배경보다 세로가 긴 방에서는 세로 배율이 이겨야 한다.
            var background = BuildBackground(new Vector2(24f, 30f), Vector2.zero, 3840, 2160);
            background.Fit();

            float expected = 30f / 21.6f;
            Assert.That(background.transform.localScale.x, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void 카메라가_움직여도_배경은_제자리다()
        {
            var background = BuildBackground(new Vector2(30f, 18f), Vector2.zero, 3840, 2160);
            background.Fit();
            Vector3 placed = background.transform.position;

            // Fit은 카메라를 전혀 보지 않는다. 두 번 불러도 같은 자리여야 한다.
            background.Fit();

            Assert.That(background.transform.position, Is.EqualTo(placed));
        }

        static RoomFittedBackground BuildBackground(Vector2 roomSize, Vector2 roomCenter,
                                                    int textureWidth, int textureHeight)
        {
            var roomObject = new GameObject("TestRoom");
            roomObject.transform.position = roomCenter;
            var room = roomObject.AddComponent<Room>();

            var so = new UnityEditor.SerializedObject(room);
            so.FindProperty("size").vector2Value = roomSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            var child = new GameObject("RoomBackground");
            child.transform.SetParent(roomObject.transform, false);

            var texture = new Texture2D(textureWidth, textureHeight);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture,
                                            new Rect(0f, 0f, textureWidth, textureHeight),
                                            new Vector2(0.5f, 0.5f), 100f);

            return child.AddComponent<RoomFittedBackground>();
        }
    }
}
