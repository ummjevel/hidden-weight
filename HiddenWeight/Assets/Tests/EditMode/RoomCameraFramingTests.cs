using NUnit.Framework;
using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 카메라 구도 계산은 플레이어·물리 없이 검증할 수 있게 순수 함수로 빼 뒀다.
    // 체감(추적 시간·예측 거리)은 수치 조정 대상이라 여기서 고정하지 않는다.
    public class RoomCameraFramingTests
    {
        const float HalfWidth = 2.5f;
        const float Up = 1.5f;
        const float Down = 1f;

        [Test]
        public void 데드존_안에서_움직이면_카메라는_그대로다()
        {
            var camera = new Vector2(10f, 5f);

            var target = RoomCamera.ApplyDeadZone(camera, camera + new Vector2(2f, 0.9f),
                                                  HalfWidth, Up, Down);

            Assert.That(target, Is.EqualTo(camera).Using(Vector2Within(0.0001f)));
        }

        [Test]
        public void 데드존을_벗어난_만큼만_목표가_밀린다()
        {
            var camera = new Vector2(10f, 5f);

            var target = RoomCamera.ApplyDeadZone(camera, camera + new Vector2(HalfWidth + 3f, 0f),
                                                  HalfWidth, Up, Down);

            Assert.That(target.x, Is.EqualTo(camera.x + 3f).Within(0.0001f));
            Assert.That(target.y, Is.EqualTo(camera.y).Within(0.0001f));
        }

        [Test]
        public void 세로_데드존은_위아래가_다르다()
        {
            var camera = new Vector2(0f, 0f);

            // 위로 1.4는 상단 데드존(1.5) 안이라 카메라가 버틴다.
            var rising = RoomCamera.ApplyDeadZone(camera, new Vector2(0f, 1.4f), HalfWidth, Up, Down);
            Assert.That(rising.y, Is.EqualTo(0f).Within(0.0001f));

            // 같은 거리라도 아래로는 하단 데드존(1.0)을 넘어 따라간다 — 낙하를 빨리 보여주기 위해서다.
            var falling = RoomCamera.ApplyDeadZone(camera, new Vector2(0f, -1.4f), HalfWidth, Up, Down);
            Assert.That(falling.y, Is.EqualTo(-0.4f).Within(0.0001f));
        }

        [Test]
        public void 추적점은_매_프레임_다시_걸어도_안정적이다()
        {
            // 데드존은 걸러내는 필터가 아니라 제약이다. 같은 관심점에 두 번 걸어도 결과가
            // 더 밀리지 않아야 추적점을 매 프레임 갱신해도 안전하다.
            var focus = new Vector2(0f, 0f);
            var player = new Vector2(9f, -4f);

            var once = RoomCamera.ApplyDeadZone(focus, player, HalfWidth, Up, Down);
            var twice = RoomCamera.ApplyDeadZone(once, player, HalfWidth, Up, Down);

            Assert.That(twice, Is.EqualTo(once).Using(Vector2Within(0.0001f)));
        }

        [Test]
        public void 앵커_구도는_플레이어에게서_정해진_거리_이상_멀어지지_않는다()
        {
            var player = new Vector2(0f, 0f);

            // 가까운 앵커는 그대로 쓴다.
            Assert.That(RoomCamera.Leash(new Vector2(1f, 0f), player, 2.5f),
                        Is.EqualTo(new Vector2(1f, 0f)).Using(Vector2Within(0.0001f)));

            // 먼 앵커는 방향만 남기고 거리를 자른다.
            var leashed = RoomCamera.Leash(new Vector2(30f, 40f), player, 2.5f);
            Assert.That(leashed.magnitude, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(Vector2.Angle(leashed, new Vector2(30f, 40f)), Is.LessThan(0.01f));

            // 0 이하는 제한 없음 — 퍼즐방처럼 완전히 고정된 구도용이다.
            Assert.That(RoomCamera.Leash(new Vector2(99f, 99f), player, 0f),
                        Is.EqualTo(new Vector2(99f, 99f)).Using(Vector2Within(0.0001f)));
        }

        [Test]
        public void 방_경계_밖으로는_나가지_않는다()
        {
            var room = NewRoom(new Vector2(30f, 18f), Vector2.zero);
            var halfScreen = new Vector2(10.67f, 6f);

            var clamped = RoomCamera.ClampToRoom(new Vector2(100f, 100f), room, halfScreen);

            Assert.That(clamped.x, Is.EqualTo(15f - halfScreen.x).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(9f - halfScreen.y).Within(0.0001f));
        }

        [Test]
        public void 화면보다_작은_방은_중심에_고정된다()
        {
            // 잔재 Secret01(18x14)처럼 화면(21.3x12)보다 좁은 방은 가로만 고정되고 세로는 움직인다.
            var room = NewRoom(new Vector2(18f, 14f), new Vector2(4f, 3f));
            var halfScreen = new Vector2(10.67f, 6f);

            var clamped = RoomCamera.ClampToRoom(new Vector2(999f, 999f), room, halfScreen);

            Assert.That(clamped.x, Is.EqualTo(4f).Within(0.0001f), "가로가 화면보다 좁으면 방 중심에 고정된다");
            Assert.That(clamped.y, Is.EqualTo(3f + 7f - 6f).Within(0.0001f), "세로는 아직 여유가 있어 클램프만 된다");
        }

        [Test]
        public void 방이_없으면_목표를_그대로_돌려준다()
        {
            var target = new Vector2(3f, 4f);
            Assert.That(RoomCamera.ClampToRoom(target, null, new Vector2(10.67f, 6f)),
                        Is.EqualTo(target).Using(Vector2Within(0.0001f)));
        }

        static Room NewRoom(Vector2 size, Vector2 center)
        {
            var go = new GameObject("TestRoom");
            go.transform.position = center;
            var room = go.AddComponent<Room>();

            // size는 인스펙터 전용 필드라 SerializedObject로 넣는다.
            var so = new UnityEditor.SerializedObject(room);
            so.FindProperty("size").vector2Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();

            return room;
        }

        static System.Collections.IComparer Vector2Within(float tolerance)
            => new Vector2Comparer(tolerance);

        class Vector2Comparer : System.Collections.IComparer
        {
            readonly float _tolerance;
            public Vector2Comparer(float tolerance) => _tolerance = tolerance;

            public int Compare(object x, object y)
                => Vector2.Distance((Vector2)x, (Vector2)y) <= _tolerance ? 0 : 1;
        }
    }
}
