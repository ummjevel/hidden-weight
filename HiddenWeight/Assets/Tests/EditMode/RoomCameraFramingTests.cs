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

        // 화면 전체가 아니라 카메라 '중심'만 방 안에 갇힌다. 방이 씬으로 갈라진 뒤로는 이
        // 동작이 필수다 — 문 너머는 아직 로드되지 않은 다른 씬이라, 경계에서 이웃 공간을
        // 미리 보여 주지 않으면 플레이어만 빈 화면으로 걸어 나간다.
        [Test]
        public void 카메라_중심은_방_경계_밖으로_나가지_않는다()
        {
            var room = NewRoom(new Vector2(30f, 18f), Vector2.zero);

            var clamped = RoomCamera.ClampToRoom(new Vector2(100f, 100f), room);

            Assert.That(clamped.x, Is.EqualTo(15f).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(9f).Within(0.0001f));
        }

        [Test]
        public void 방_가장자리에서는_이웃_공간이_화면에_들어온다()
        {
            var room = NewRoom(new Vector2(30f, 18f), Vector2.zero);
            var halfScreen = new Vector2(10.67f, 6f);

            var clamped = RoomCamera.ClampToRoom(new Vector2(100f, 0f), room);

            // 중심이 방 오른쪽 끝(15)에 서므로 화면 오른쪽 절반은 방 밖을 비춘다.
            Assert.That(clamped.x + halfScreen.x, Is.GreaterThan(15f),
                        "방 끝에서 다음 통로를 미리 보여 주지 않으면 전환이 끊겨 보인다.");
        }

        [Test]
        public void 방이_없으면_목표를_그대로_돌려준다()
        {
            var target = new Vector2(3f, 4f);
            Assert.That(RoomCamera.ClampToRoom(target, null),
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
