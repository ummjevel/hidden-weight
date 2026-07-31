using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class RoomLinkTableTests
    {
        [Test]
        public void DoorId_JoinsLinkIdAndSide()
        {
            Assert.That(RoomLink.DoorId("residue_R01_R02", Side.E), Is.EqualTo("residue_R01_R02:E"));
        }

        [Test]
        public void Opposite_PairsHorizontalAndVertical()
        {
            Assert.That(RoomLink.Opposite(Side.W), Is.EqualTo(Side.E));
            Assert.That(RoomLink.Opposite(Side.E), Is.EqualTo(Side.W));
            Assert.That(RoomLink.Opposite(Side.U), Is.EqualTo(Side.D));
            Assert.That(RoomLink.Opposite(Side.D), Is.EqualTo(Side.U));
            Assert.That(RoomLink.Opposite(Side.NW), Is.EqualTo(Side.SE));
            Assert.That(RoomLink.Opposite(Side.NE), Is.EqualTo(Side.SW));
        }

        // 비밀 연결은 방향이 아니라 연결의 성격이라 마주 보는 짝이 없다.
        [Test]
        public void Opposite_ReturnsSecretUnchanged()
        {
            Assert.That(RoomLink.Opposite(Side.S), Is.EqualTo(Side.S));
        }

        [Test]
        public void FromDoorId_ExtractsLinkId()
        {
            Assert.That(RoomLinkTable.FromDoorId("residue_R01_R02:E"), Is.EqualTo("residue_R01_R02"));
        }

        [Test]
        public void FromDoorId_ReturnsNullWhenMalformed()
        {
            Assert.That(RoomLinkTable.FromDoorId("residue_R01_R02"), Is.Null);
            Assert.That(RoomLinkTable.FromDoorId(null), Is.Null);
        }
    }
}
