using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomLoaderTests
    {
        GameObject _root;
        RoomLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RoomLoader");
            _loader = _root.AddComponent<RoomLoader>();
            _loader.ConfigureForTests("Room_Residue_");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void SceneNameFor_PrefixesRoomName()
        {
            Assert.That(_loader.SceneNameFor("R01"), Is.EqualTo("Room_Residue_R01"));
            Assert.That(_loader.SceneNameFor("S3"), Is.EqualTo("Room_Residue_S3"));
        }

        [Test]
        public void StartsIdle()
        {
            Assert.That(_loader.IsTransitioning, Is.False);
            Assert.That(_loader.CurrentRoom, Is.Null);
        }

        // 전환 중 다른 문이 발동하면 두 전환이 겹쳐 플레이어가 사라진다.
        [Test]
        public void RequestTransition_IgnoredWhileTransitioning()
        {
            var doorGo = new GameObject("Door", typeof(BoxCollider2D));
            var door = doorGo.AddComponent<RoomDoor>();
            door.Configure("a:E", Side.E, "R02", "a:W", Vector2.zero);

            _loader.SetTransitioningForTests(true);
            _loader.RequestTransition(door);

            Assert.That(_loader.CurrentRoom, Is.Null);

            Object.DestroyImmediate(doorGo);
        }

        // 문을 통과하면 그 문은 무장 해제된다 — 돌아왔을 때 즉시 되튕기지 않게.
        [Test]
        public void RequestTransition_DisarmsSourceDoor()
        {
            var doorGo = new GameObject("Door", typeof(BoxCollider2D));
            var door = doorGo.AddComponent<RoomDoor>();
            door.Configure("a:E", Side.E, "R02", "a:W", Vector2.zero);

            // EditMode에는 실제 씬 픽스처가 없어 RequestTransition이 코루틴을 첫 yield까지
            // 동기 실행하며 "빌드 세팅에 씬 없음" 에러를 곧바로 찍는다 — 이 테스트가 검증하려는
            // 무장 해제 자체는 그 이전에 이미 끝나 있으므로, 예상된 에러 로그로 받아들인다.
            LogAssert.Expect(LogType.Error, "[RoomLoader] 씬 Room_Residue_R02 을 빌드 세팅에서 찾을 수 없다. 전환을 취소한다.");

            _loader.RequestTransition(door);

            Assert.That(door.Armed, Is.False);

            Object.DestroyImmediate(doorGo);
        }
    }
}
