using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class NightMissionStateTests
    {
        [Test]
        public void 시작하면_진행중_상태다()
        {
            var mission = new NightMissionState();

            Assert.AreEqual(NightMissionOutcome.InProgress, mission.Outcome);
            Assert.IsFalse(mission.IsFinished);
        }

        [Test]
        public void 경과시간_60초가_지나면_시간초과로_실패한다()
        {
            var mission = new NightMissionState();

            mission.Tick(60f);

            Assert.AreEqual(NightMissionOutcome.FailedTimeout, mission.Outcome);
            Assert.IsTrue(mission.IsFailure);
        }

        [Test]
        public void 경과시간_60초_전에는_아직_진행중이다()
        {
            var mission = new NightMissionState();

            mission.Tick(59f);

            Assert.AreEqual(NightMissionOutcome.InProgress, mission.Outcome);
        }

        [Test]
        public void 발각되면_즉시_실패한다()
        {
            var mission = new NightMissionState();

            mission.MarkDetected();

            Assert.AreEqual(NightMissionOutcome.FailedDetected, mission.Outcome);
            Assert.IsTrue(mission.IsFailure);
        }

        [Test]
        public void 조사_없이_탈출하면_무기없이_성공한다()
        {
            var mission = new NightMissionState();

            mission.ReachExit();

            Assert.AreEqual(NightMissionOutcome.SuccessWithoutWeapon, mission.Outcome);
            Assert.IsFalse(mission.IsFailure);
        }

        [Test]
        public void 조사_후_탈출하면_무기를_획득하며_성공한다()
        {
            var mission = new NightMissionState();

            mission.MarkInvestigated();
            mission.ReachExit();

            Assert.AreEqual(NightMissionOutcome.Success, mission.Outcome);
        }

        [Test]
        public void 이미_끝난_미션은_다른_이벤트로_상태가_안_바뀐다()
        {
            var mission = new NightMissionState();
            mission.MarkDetected();

            mission.MarkInvestigated();
            mission.ReachExit();
            mission.Tick(100f);

            Assert.AreEqual(NightMissionOutcome.FailedDetected, mission.Outcome);
        }
    }
}
