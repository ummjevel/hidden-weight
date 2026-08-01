using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomDoorTests
    {
        GameObject _go;
        RoomDoor _door;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Door", typeof(BoxCollider2D));
            _door = _go.AddComponent<RoomDoor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // 방으로 들어가는 쪽으로 밀어내지 않으면 도착하자마자 문에 낀다.
        [Test]
        public void DefaultArrivalOffset_PushesIntoRoom()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.W), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.E), Is.EqualTo(new Vector2(-1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.U), Is.EqualTo(new Vector2(0f, -1.5f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.D), Is.EqualTo(new Vector2(0f, 1.5f)));
        }

        // 대각은 가로 성분만 따른다. 실제로 쓰는 지역이 나오면 그때 다시 정한다.
        [Test]
        public void DefaultArrivalOffset_TreatsDiagonalsAsHorizontal()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.NW), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.SW), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.NE), Is.EqualTo(new Vector2(-1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.SE), Is.EqualTo(new Vector2(-1.5f, 0f)));
        }

        [Test]
        public void DefaultArrivalOffset_LeavesSecretAtZero()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.S), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Configure_StoresLinkFields()
        {
            _door.Configure("residue_R01_R02:E", Side.E, "R02", "residue_R01_R02:W", new Vector2(-1.5f, 0f));

            Assert.That(_door.DoorId, Is.EqualTo("residue_R01_R02:E"));
            Assert.That(_door.Side, Is.EqualTo(Side.E));
            Assert.That(_door.TargetRoom, Is.EqualTo("R02"));
            Assert.That(_door.TargetDoorId, Is.EqualTo("residue_R01_R02:W"));
        }

        [Test]
        public void ArrivalPosition_AddsOffsetToTransform()
        {
            _go.transform.position = new Vector3(10f, 4f, 0f);
            _door.Configure("d", Side.W, "R02", "e", new Vector2(1.5f, 0f));

            Assert.That(_door.ArrivalPosition, Is.EqualTo(new Vector2(11.5f, 4f)));
        }

        // 도착한 문 위에 서 있는 상태로 시작하므로, 벗어나기 전까지 발동하면 안 된다.
        [Test]
        public void Door_StartsArmedAndCanBeDisarmed()
        {
            Assert.That(_door.Armed, Is.True);

            _door.Disarm();

            Assert.That(_door.Armed, Is.False);
        }

        [Test]
        public void Collider_IsTrigger()
        {
            _door.Configure("d", Side.E, "R02", "e", Vector2.zero);

            Assert.That(_go.GetComponent<BoxCollider2D>().isTrigger, Is.True);
        }
    }
}
