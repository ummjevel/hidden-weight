using System;
using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class StatChoiceGeneratorTests
    {
        [Test]
        public void 항상_서로_다른_3개를_뽑는다()
        {
            var random = new Random(42);

            var choices = StatChoiceGenerator.PickThree(random);

            Assert.AreEqual(3, choices.Count);
            Assert.AreEqual(3, new System.Collections.Generic.HashSet<StatType>(choices).Count);
        }

        [Test]
        public void 시드가_같으면_결과도_같다()
        {
            var choicesA = StatChoiceGenerator.PickThree(new Random(1));
            var choicesB = StatChoiceGenerator.PickThree(new Random(1));

            CollectionAssert.AreEqual(choicesA, choicesB);
        }
    }
}
