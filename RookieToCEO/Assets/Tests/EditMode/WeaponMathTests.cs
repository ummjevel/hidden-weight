using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class WeaponMathTests
    {
        [Test]
        public void 배율이_1이면_기본_데미지_그대로다()
        {
            var damage = WeaponMath.EffectiveDamage(10, 1f);

            Assert.AreEqual(10, damage);
        }

        [Test]
        public void 데미지_배율이_커지면_데미지도_커진다()
        {
            var damage = WeaponMath.EffectiveDamage(10, 1.5f);

            Assert.AreEqual(15, damage);
        }

        [Test]
        public void 공격속도_배율이_커지면_공격_간격은_짧아진다()
        {
            var interval = WeaponMath.EffectiveAttackInterval(1.2f, 2f);

            Assert.AreEqual(0.6f, interval, 0.0001f);
        }

        [Test]
        public void 사거리_배율이_커지면_유효_사거리도_커진다()
        {
            var range = WeaponMath.EffectiveRange(3f, 1.5f);

            Assert.AreEqual(4.5f, range, 0.0001f);
        }
    }
}
