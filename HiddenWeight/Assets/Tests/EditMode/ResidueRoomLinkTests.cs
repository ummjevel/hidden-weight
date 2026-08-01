using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using HiddenWeight.Data;
using HiddenWeight.EditorTools;

namespace HiddenWeight.Tests
{
    public class ResidueRoomLinkTests
    {
        [Test]
        public void HasFourteenLinks()
        {
            Assert.That(ResidueRoomLinks.Links.Length, Is.EqualTo(14));
        }

        [Test]
        public void HasFifteenRooms()
        {
            Assert.That(ResidueRoomLinks.RoomNames.Length, Is.EqualTo(15));
        }

        [Test]
        public void LinkIdsAreUnique()
        {
            var ids = ResidueRoomLinks.Links.Select(l => l.linkId).ToArray();
            Assert.That(ids, Is.Unique);
        }

        [Test]
        public void EverySideFacesItsPair()
        {
            foreach (var link in ResidueRoomLinks.Links)
                Assert.That(link.toSide, Is.EqualTo(RoomLink.Opposite(link.fromSide)),
                    link.linkId + " 의 두 방향이 마주 보지 않는다.");
        }

        [Test]
        public void EveryRoomNameIsKnown()
        {
            var known = new HashSet<string>(ResidueRoomLinks.RoomNames);
            foreach (var link in ResidueRoomLinks.Links)
            {
                Assert.That(known, Does.Contain(link.fromRoom), link.linkId + " fromRoom");
                Assert.That(known, Does.Contain(link.toRoom), link.linkId + " toRoom");
            }
        }

        // 문 28개가 서로 다른 id를 가져야 대상 문을 유일하게 찾을 수 있다.
        [Test]
        public void EveryDoorIdIsUnique()
        {
            var doorIds = ResidueRoomLinks.Links
                .SelectMany(l => new[] { l.FromDoorId, l.ToDoorId })
                .ToArray();

            Assert.That(doorIds.Length, Is.EqualTo(28));
            Assert.That(doorIds, Is.Unique);
        }

        // 주 동선 12방이 R01부터 R12까지 끊기지 않아야 완주가 가능하다.
        [Test]
        public void MainRouteConnectsR01ToR12()
        {
            var byFrom = ResidueRoomLinks.Links.ToLookup(l => l.fromRoom);
            string current = "R01";

            for (int i = 1; i < 12; i++)
            {
                string expected = "R" + (i + 1).ToString("00");
                var link = byFrom[current].FirstOrDefault(l => l.toRoom == expected);
                Assert.That(link.linkId, Is.Not.Null, current + " 에서 " + expected + " 로 가는 링크가 없다.");
                current = expected;
            }

            Assert.That(current, Is.EqualTo("R12"));
        }
    }
}
