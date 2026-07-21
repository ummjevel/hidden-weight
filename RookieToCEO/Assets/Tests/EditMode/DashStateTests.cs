using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class DashStateTests
    {
        [Test]
        public void 처음에는_Windup_상태다()
        {
            var dash = new DashState(1f, 0.4f, 1.2f);

            Assert.AreEqual(DashPhase.Windup, dash.Phase);
        }

        [Test]
        public void Windup_시간이_지나면_Dashing으로_바뀐다()
        {
            var dash = new DashState(1f, 0.4f, 1.2f);

            dash.Tick(1f);

            Assert.AreEqual(DashPhase.Dashing, dash.Phase);
        }

        [Test]
        public void Dashing_시간이_지나면_Cooldown으로_바뀐다()
        {
            var dash = new DashState(1f, 0.4f, 1.2f);
            dash.Tick(1f);   // Windup -> Dashing

            dash.Tick(0.4f); // Dashing -> Cooldown

            Assert.AreEqual(DashPhase.Cooldown, dash.Phase);
        }

        [Test]
        public void Cooldown_시간이_지나면_다시_Windup으로_돌아간다()
        {
            var dash = new DashState(1f, 0.4f, 1.2f);
            dash.Tick(1f);
            dash.Tick(0.4f);

            dash.Tick(1.2f);

            Assert.AreEqual(DashPhase.Windup, dash.Phase);
        }

        [Test]
        public void Dashing_상태에서만_돌진_배율이_적용된다()
        {
            var dash = new DashState(1f, 0.4f, 1.2f);

            Assert.AreEqual(0.3f, dash.CurrentSpeedMultiplier(0.3f, 3f));

            dash.Tick(1f); // Dashing

            Assert.AreEqual(3f, dash.CurrentSpeedMultiplier(0.3f, 3f));
        }
    }
}
