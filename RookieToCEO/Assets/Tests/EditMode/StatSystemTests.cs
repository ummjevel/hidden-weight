using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class StatSystemTests
    {
        [Test]
        public void 레벨업_전에는_배율이_기본값이다()
        {
            var stats = new StatSystem();

            Assert.AreEqual(1f, stats.DamageMultiplier);
            Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
            Assert.AreEqual(1f, stats.MoveSpeedMultiplier);
            Assert.AreEqual(1f, stats.RangeMultiplier);
            Assert.AreEqual(1f, stats.CooldownMultiplier);
            Assert.AreEqual(0, stats.BonusMaxHp);
        }

        [Test]
        public void 업무처리력을_세번_올리면_공격력_배율이_그만큼_증가한다()
        {
            var stats = new StatSystem();
            stats.LevelUp(StatType.WorkPower);
            stats.LevelUp(StatType.WorkPower);
            stats.LevelUp(StatType.WorkPower);

            Assert.AreEqual(3, stats.GetLevel(StatType.WorkPower));
            Assert.Greater(stats.DamageMultiplier, 1f);
        }

        [Test]
        public void 멘탈관리를_올리면_보너스_최대HP가_증가한다()
        {
            var stats = new StatSystem();
            stats.LevelUp(StatType.MentalCare);

            Assert.Greater(stats.BonusMaxHp, 0);
        }

        [Test]
        public void 짬_스탯이_아무리_높아도_쿨타임_배율은_0_1_밑으로_내려가지_않는다()
        {
            var stats = new StatSystem();
            for (var i = 0; i < 100; i++)
            {
                stats.LevelUp(StatType.Seniority);
            }

            Assert.GreaterOrEqual(stats.CooldownMultiplier, 0.1f);
        }

        [Test]
        public void 서로_다른_스탯은_독립적으로_레벨이_오른다()
        {
            var stats = new StatSystem();
            stats.LevelUp(StatType.WorkPower);

            Assert.AreEqual(1, stats.GetLevel(StatType.WorkPower));
            Assert.AreEqual(0, stats.GetLevel(StatType.HandSpeed));
        }
    }
}
