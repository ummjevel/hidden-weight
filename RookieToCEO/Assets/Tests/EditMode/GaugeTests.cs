using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class GaugeTests
    {
        [Test]
        public void 처음에는_비어있고_가득_차지_않았다()
        {
            var gauge = new Gauge(100f);

            Assert.AreEqual(0f, gauge.Value);
            Assert.IsFalse(gauge.IsFull);
        }

        [Test]
        public void 최대치를_넘게_채워도_Max를_넘지_않는다()
        {
            var gauge = new Gauge(100f);

            gauge.Add(150f);

            Assert.AreEqual(100f, gauge.Value);
            Assert.IsTrue(gauge.IsFull);
        }

        [Test]
        public void 가득_차지_않으면_TryConsume이_실패한다()
        {
            var gauge = new Gauge(100f);
            gauge.Add(50f);

            var consumed = gauge.TryConsume();

            Assert.IsFalse(consumed);
            Assert.AreEqual(50f, gauge.Value);
        }

        [Test]
        public void 가득_차면_TryConsume이_성공하고_0으로_돌아간다()
        {
            var gauge = new Gauge(100f);
            gauge.Add(100f);

            var consumed = gauge.TryConsume();

            Assert.IsTrue(consumed);
            Assert.AreEqual(0f, gauge.Value);
        }
    }
}
