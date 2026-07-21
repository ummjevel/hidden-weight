using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class CooldownTests
    {
        [Test]
        public void 처음_생성하면_바로_사용_가능하다()
        {
            var cooldown = new Cooldown(12f);

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void 사용하면_지정한_시간동안_준비되지_않는다()
        {
            var cooldown = new Cooldown(12f);

            var used = cooldown.TryUse();

            Assert.IsTrue(used);
            Assert.IsFalse(cooldown.IsReady);
        }

        [Test]
        public void 쿨타임이_아직_남았으면_TryUse가_실패한다()
        {
            var cooldown = new Cooldown(12f);
            cooldown.TryUse();

            var usedAgain = cooldown.TryUse();

            Assert.IsFalse(usedAgain);
        }

        [Test]
        public void 쿨타임_시간이_다_지나면_다시_준비된다()
        {
            var cooldown = new Cooldown(12f);
            cooldown.TryUse();

            cooldown.Tick(12f);

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void SetDuration으로_길이를_바꾸면_다음_사용부터_적용된다()
        {
            var cooldown = new Cooldown(12f);
            cooldown.SetDuration(6f); // 짬 스탯으로 쿨타임이 줄어든 상황을 흉내
            cooldown.TryUse();

            cooldown.Tick(6f);

            Assert.IsTrue(cooldown.IsReady);
        }
    }
}
