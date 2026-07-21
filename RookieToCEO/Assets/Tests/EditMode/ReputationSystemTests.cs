using NUnit.Framework;
using RookieToCEO.Core;

namespace RookieToCEO.Tests.EditMode
{
    public class ReputationSystemTests
    {
        [Test]
        public void 초기값은_평판3에_풀HP다()
        {
            var rep = new ReputationSystem(100);

            Assert.AreEqual(3, rep.Reputation);
            Assert.AreEqual(100, rep.CurrentHp);
            Assert.IsFalse(rep.IsGameOver);
        }

        [Test]
        public void HP가_0이_되면_평판이_1_감소하고_부활_대기_상태가_된다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(100);

            Assert.AreEqual(2, rep.Reputation);
            Assert.IsTrue(rep.IsReviving);
            Assert.AreEqual(0, rep.CurrentHp);
        }

        [Test]
        public void 부활_대기_2초_후_HP전체회복과_3초_무적이_시작된다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(100);

            rep.Tick(2f);

            Assert.IsFalse(rep.IsReviving);
            Assert.IsTrue(rep.IsInvulnerable);
            Assert.AreEqual(100, rep.CurrentHp);
        }

        [Test]
        public void 무적_시간_동안은_데미지를_받지_않는다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(100);
            rep.Tick(2f); // 부활 + 무적 시작

            rep.TakeDamage(50);

            Assert.AreEqual(100, rep.CurrentHp);
        }

        [Test]
        public void 무적_3초가_끝나면_다시_데미지를_받는다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(100);
            rep.Tick(2f); // 부활 + 무적 시작
            rep.Tick(3f); // 무적 종료

            rep.TakeDamage(30);

            Assert.AreEqual(70, rep.CurrentHp);
        }

        [Test]
        public void 평판이_0이_되면_게임오버_이벤트가_발생한다()
        {
            var rep = new ReputationSystem(100);
            var gameOverCalled = false;
            rep.OnGameOver += () => gameOverCalled = true;

            rep.TakeDamage(100); // 평판 2, 부활 대기
            rep.Tick(2f);
            rep.Tick(3f);
            rep.TakeDamage(100); // 평판 1, 부활 대기
            rep.Tick(2f);
            rep.Tick(3f);
            rep.TakeDamage(100); // 평판 0 -> 게임오버

            Assert.IsTrue(rep.IsGameOver);
            Assert.IsTrue(gameOverCalled);
        }

        [Test]
        public void ResetForNewRun_호출하면_평판과_HP가_초기화된다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(100);
            rep.Tick(2f);
            rep.Tick(3f);
            rep.TakeDamage(100);

            rep.ResetForNewRun();

            Assert.AreEqual(3, rep.Reputation);
            Assert.AreEqual(100, rep.CurrentHp);
            Assert.IsFalse(rep.IsGameOver);
            Assert.IsFalse(rep.IsInvulnerable);
        }

        [Test]
        public void 커피를_먹으면_최대치를_넘지_않게_회복한다()
        {
            var rep = new ReputationSystem(100);
            rep.TakeDamage(30); // 무적/부활 상태 아님 (HP 70)

            rep.Heal(50);

            Assert.AreEqual(100, rep.CurrentHp);
        }

        [Test]
        public void LoseReputationDirectly는_HP를_건드리지_않고_평판만_깎는다()
        {
            var rep = new ReputationSystem(100);

            rep.LoseReputationDirectly();

            Assert.AreEqual(2, rep.Reputation);
            Assert.AreEqual(100, rep.CurrentHp);
            Assert.IsFalse(rep.IsReviving);
        }

        [Test]
        public void LoseReputationDirectly로_평판이_0이_되면_게임오버다()
        {
            var rep = new ReputationSystem(100);

            rep.LoseReputationDirectly();
            rep.LoseReputationDirectly();
            rep.LoseReputationDirectly();

            Assert.IsTrue(rep.IsGameOver);
        }
    }
}
