using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class LevelSystemTests
    {
        [Test]
        public void 처음에는_레벨_1이다()
        {
            var level = new LevelSystem();

            Assert.AreEqual(1, level.Level);
        }

        [Test]
        public void 필요_경험치에_못_미치면_레벨업하지_않는다()
        {
            var level = new LevelSystem();

            var leveledUp = level.AddXp(10f);

            Assert.IsFalse(leveledUp);
            Assert.AreEqual(1, level.Level);
        }

        [Test]
        public void 필요_경험치를_채우면_레벨업하고_true를_반환한다()
        {
            var level = new LevelSystem();

            var leveledUp = level.AddXp(level.XpToNextLevel);

            Assert.IsTrue(leveledUp);
            Assert.AreEqual(2, level.Level);
        }

        [Test]
        public void 초과분_경험치는_다음_레벨로_이월된다()
        {
            var level = new LevelSystem();
            var needed = level.XpToNextLevel;

            level.AddXp(needed + 5f);

            Assert.AreEqual(5f, level.CurrentXp, 0.0001f);
        }

        [Test]
        public void 레벨업_이벤트가_발생한다()
        {
            var level = new LevelSystem();
            var called = false;
            level.OnLevelUp += () => called = true;

            level.AddXp(level.XpToNextLevel);

            Assert.IsTrue(called);
        }

        [Test]
        public void 레벨이_오를수록_다음_레벨까지_필요한_경험치도_늘어난다()
        {
            var level = new LevelSystem();
            var firstRequirement = level.XpToNextLevel;

            level.AddXp(firstRequirement);
            var secondRequirement = level.XpToNextLevel;

            Assert.Greater(secondRequirement, firstRequirement);
        }
    }
}
