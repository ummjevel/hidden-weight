using System.Collections.Generic;
using NUnit.Framework;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Tests.EditMode
{
    public class TargetingUtilityTests
    {
        [Test]
        public void 후보가_없으면_마이너스1을_반환한다()
        {
            var result = TargetingUtility.FindNearestIndex(Vector2.zero, new List<Vector2>());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void 가장_가까운_후보의_인덱스를_반환한다()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(10, 10),
                new Vector2(1, 0),
                new Vector2(5, 5),
            };

            var result = TargetingUtility.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void 플레이어_위치가_바뀌면_가장_가까운_대상도_바뀐다()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(20, 20),
            };

            var result = TargetingUtility.FindNearestIndex(new Vector2(19, 19), candidates);

            Assert.AreEqual(1, result);
        }
    }
}
