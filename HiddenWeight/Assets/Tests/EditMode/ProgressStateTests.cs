using NUnit.Framework;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class ProgressStateTests
    {
        ProgressState _p;

        [SetUp]
        public void SetUp() => _p = new ProgressState();

        [Test]
        public void 시작할때는_아무_스킬도_없다()
        {
            Assert.IsFalse(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasSkill(EmotionId.Hush));
            Assert.IsFalse(_p.HasSkill(EmotionId.Foresight));
            Assert.IsFalse(_p.HasAwareness);
        }

        [Test]
        public void 스킬을_해금하면_보유로_바뀐다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsTrue(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasSkill(EmotionId.Hush));
        }

        [Test]
        public void None_게이트는_스킬_없이도_열린다()
        {
            Assert.IsTrue(_p.CanOpenGate(EmotionId.None));
        }

        [Test]
        public void 필요스킬이_없으면_게이트가_닫혀있다()
        {
            Assert.IsFalse(_p.CanOpenGate(EmotionId.Hush));
            _p.UnlockSkill(EmotionId.Hush);
            Assert.IsTrue(_p.CanOpenGate(EmotionId.Hush));
        }

        [Test]
        public void 최종게이트는_세_조건이_전부_충족돼야_열린다()
        {
            Assert.IsFalse(_p.CanOpenFinalGate());

            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsFalse(_p.CanOpenFinalGate(), "자각과 균열 클리어가 아직 없다");

            _p.GrantAwareness();
            Assert.IsFalse(_p.CanOpenFinalGate(), "균열 클리어가 아직 없다");

            _p.MarkFractureCleared();
            Assert.IsTrue(_p.CanOpenFinalGate());
        }

        [Test]
        public void 파편은_처음_수집할때만_true를_돌려준다()
        {
            Assert.IsTrue(_p.CollectFragment("residue_01"));
            Assert.IsFalse(_p.CollectFragment("residue_01"));
            Assert.AreEqual(1, _p.FragmentCount);
            Assert.IsTrue(_p.HasFragment("residue_01"));
        }

        [Test]
        public void 같은_스킬을_두번_해금해도_상태가_그대로다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsTrue(_p.HasSkill(EmotionId.Rewind));
        }

        [Test]
        public void ResetAll은_모든_진행도를_지운다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            _p.GrantAwareness();
            _p.MarkFractureCleared();
            _p.CollectFragment("a");

            _p.ResetAll();

            Assert.IsFalse(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasAwareness);
            Assert.IsFalse(_p.HasClearedFracture);
            Assert.AreEqual(0, _p.FragmentCount);
        }
    }
}
