using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class BossGlanceSystemTests
    {
        [Test]
        public void 처음에는_Idle_상태다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.1f, sweepCount: 1);

            Assert.AreEqual(BossGlancePhase.Idle, glance.Phase);
        }

        [Test]
        public void 경과시간_28초가_지나면_경고_단계로_들어간다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.1f, sweepCount: 1);

            glance.Tick(28f, 0.5f);

            Assert.AreEqual(BossGlancePhase.Warning, glance.Phase);
        }

        [Test]
        public void 경과시간_30초가_지나면_시선이_이동하기_시작한다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.1f, sweepCount: 1);

            glance.Tick(30f, 0.5f);

            Assert.AreEqual(BossGlancePhase.Sweeping, glance.Phase);
        }

        [Test]
        public void 시선_범위_안에_있으면_일하는_척_상태가_된다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.5f, sweepCount: 1);

            glance.Tick(30f, 0.5f); // 발동 시작, SweepProgress01 = 0
            glance.Tick(0.01f, 0f); // 플레이어가 시선 시작 지점(0)에 있음 -> 범위 안(0.5)

            Assert.IsTrue(glance.IsPretendingToWork);
        }

        [Test]
        public void 시선_범위_밖에_있으면_걸리지_않는다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.05f, sweepCount: 1);

            glance.Tick(30f, 0.5f);
            glance.Tick(0.01f, 0.9f); // 시선은 왼쪽 끝 근처, 플레이어는 오른쪽 끝 근처

            Assert.IsFalse(glance.IsPretendingToWork);
        }

        [Test]
        public void 층_3처럼_두번_이동하면_한번_다_지나도_Idle로_안_돌아간다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.1f, sweepCount: 2);
            glance.Tick(30f, 0.5f); // Sweeping 시작

            glance.Tick(4f, 0.5f); // 첫 번째 스윕 종료 -> 두 번째 스윕 시작해야 함

            Assert.AreEqual(BossGlancePhase.Sweeping, glance.Phase);
        }

        [Test]
        public void 지정된_횟수만큼_스윕하면_Idle로_돌아간다()
        {
            var glance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.1f, sweepCount: 2);
            glance.Tick(30f, 0.5f);
            glance.Tick(4f, 0.5f);  // 두 번째 스윕 시작
            glance.Tick(4f, 0.5f);  // 두 번째 스윕도 종료

            Assert.AreEqual(BossGlancePhase.Idle, glance.Phase);
        }
    }
}
