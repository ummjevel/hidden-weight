using System.Collections.Generic;
using NUnit.Framework;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Tests.EditMode
{
    public class ConeTargetingUtilityTests
    {
        [Test]
        public void 부채꼴_각도_밖에_있으면_제외된다()
        {
            var candidates = new List<Vector2> { new Vector2(0, -5) }; // 정면(위쪽)의 정반대

            var hits = ConeTargetingUtility.FindTargetsInCone(
                Vector2.zero, Vector2.up, halfAngleDegrees: 60f, range: 10f, candidates, maxTargets: 5);

            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void 사거리_밖에_있으면_제외된다()
        {
            var candidates = new List<Vector2> { new Vector2(0, 100) }; // 정면이지만 너무 멀다

            var hits = ConeTargetingUtility.FindTargetsInCone(
                Vector2.zero, Vector2.up, halfAngleDegrees: 60f, range: 10f, candidates, maxTargets: 5);

            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void 부채꼴_범위_안의_대상은_가까운_순서로_반환된다()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(0, 5), // 더 멀다
                new Vector2(0, 2), // 더 가까움
            };

            var hits = ConeTargetingUtility.FindTargetsInCone(
                Vector2.zero, Vector2.up, halfAngleDegrees: 60f, range: 10f, candidates, maxTargets: 5);

            CollectionAssert.AreEqual(new[] { 1, 0 }, hits);
        }

        [Test]
        public void maxTargets를_넘는_대상은_잘려나간다()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(0, 1),
                new Vector2(0, 2),
                new Vector2(0, 3),
            };

            var hits = ConeTargetingUtility.FindTargetsInCone(
                Vector2.zero, Vector2.up, halfAngleDegrees: 60f, range: 10f, candidates, maxTargets: 2);

            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void 좁은_각도의_스테이플러형_판정은_옆에_있는_적을_제외한다()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(0, 5),  // 정면
                new Vector2(3, 1),  // 많이 옆
            };

            var hits = ConeTargetingUtility.FindTargetsInCone(
                Vector2.zero, Vector2.up, halfAngleDegrees: 8f, range: 10f, candidates, maxTargets: 5);

            CollectionAssert.AreEqual(new[] { 0 }, hits);
        }
    }
}
