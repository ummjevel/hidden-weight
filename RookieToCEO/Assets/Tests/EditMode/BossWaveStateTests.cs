using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class BossWaveStateTests
    {
        [Test]
        public void 시작하면_업무_폭탄_단계다()
        {
            var wave = new BossWaveState();

            Assert.AreEqual(BossWavePhase.WorkBombardment, wave.CurrentPhase);
        }

        [Test]
        public void 경과시간_20초가_되면_전면_수정_단계로_바뀐다()
        {
            var wave = new BossWaveState();

            wave.Tick(20f);

            Assert.AreEqual(BossWavePhase.FullRevision, wave.CurrentPhase);
        }

        [Test]
        public void 경과시간_40초가_되면_퇴근_취소_단계로_바뀐다()
        {
            var wave = new BossWaveState();

            wave.Tick(40f);

            Assert.AreEqual(BossWavePhase.CommuteCancelled, wave.CurrentPhase);
        }

        [Test]
        public void 경과시간_60초가_되면_시간이_다_됐다고_표시한다()
        {
            var wave = new BossWaveState();

            wave.Tick(60f);

            Assert.IsTrue(wave.IsTimeUp);
        }

        [Test]
        public void 시간이_다_되면_더이상_진행되지_않는다()
        {
            var wave = new BossWaveState();
            wave.Tick(60f);

            wave.Tick(100f);

            Assert.AreEqual(60f, wave.ElapsedSeconds, 0.0001f);
        }
    }
}
