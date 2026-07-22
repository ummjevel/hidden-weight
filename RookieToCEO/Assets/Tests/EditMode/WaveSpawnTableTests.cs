using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class WaveSpawnTableTests
    {
        [Test]
        public void 경과시간_30초_이전에는_생성_속도_배율이_1이다()
        {
            Assert.AreEqual(1f, WaveSpawnTable.GetSpawnRateMultiplier(10f));
        }

        [Test]
        public void 경과시간_30초_이후에는_생성_속도가_증가한다()
        {
            Assert.Greater(WaveSpawnTable.GetSpawnRateMultiplier(31f), 1f);
        }

        [Test]
        public void 층_1_초반에는_이메일_봉투만_등장한다()
        {
            var types = WaveSpawnTable.GetActiveEnemyTypes(1, 5f);

            Assert.IsTrue(types.Contains(EnemyType.EmailEnvelope));
            Assert.IsFalse(types.Contains(EnemyType.DocumentStack));
        }

        [Test]
        public void 층_1_15초_이후에는_서류_더미도_등장한다()
        {
            var types = WaveSpawnTable.GetActiveEnemyTypes(1, 16f);

            Assert.IsTrue(types.Contains(EnemyType.EmailEnvelope));
            Assert.IsTrue(types.Contains(EnemyType.DocumentStack));
        }

        [Test]
        public void 층_2_15초_이후에는_포스트잇이_추가된다()
        {
            var types = WaveSpawnTable.GetActiveEnemyTypes(2, 16f);

            Assert.IsTrue(types.Contains(EnemyType.PostItRush));
        }

        [Test]
        public void 층_3_15초_이후에는_방해형_적_두종이_모두_추가된다()
        {
            var types = WaveSpawnTable.GetActiveEnemyTypes(3, 16f);

            Assert.IsTrue(types.Contains(EnemyType.MeetingCalendar));
            Assert.IsTrue(types.Contains(EnemyType.ClaimPhone));
        }

        [Test]
        public void 층_4_40초_이후에만_CEO_최종_지시서가_등장한다()
        {
            var before = WaveSpawnTable.GetActiveEnemyTypes(4, 39f);
            var after = WaveSpawnTable.GetActiveEnemyTypes(4, 40f);

            Assert.IsFalse(before.Contains(EnemyType.CeoFinalOrder));
            Assert.IsTrue(after.Contains(EnemyType.CeoFinalOrder));
        }

        [Test]
        public void 유효하지_않은_층을_넘기면_예외가_발생한다()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                WaveSpawnTable.GetActiveEnemyTypes(5, 10f));
        }

        [Test]
        public void 층이_올라갈수록_기준_스폰_간격이_짧아진다()
        {
            var floor1 = WaveSpawnTable.GetBaseSpawnIntervalSeconds(1);
            var floor2 = WaveSpawnTable.GetBaseSpawnIntervalSeconds(2);
            var floor3 = WaveSpawnTable.GetBaseSpawnIntervalSeconds(3);
            var floor4 = WaveSpawnTable.GetBaseSpawnIntervalSeconds(4);

            Assert.Greater(floor1, floor2);
            Assert.Greater(floor2, floor3);
            Assert.Greater(floor3, floor4);
        }

        [Test]
        public void 유효하지_않은_층으로_기준_스폰_간격을_요청하면_예외가_발생한다()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                WaveSpawnTable.GetBaseSpawnIntervalSeconds(0));
        }
    }
}
