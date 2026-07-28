using NUnit.Framework;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class ProgressStateTests
    {
        [Test]
        public void UI_진행_이벤트와_기억_텍스트는_중복없이_보존된다()
        {
            var progress = new ProgressState();
            int currencyEvents = 0;
            int fragmentEvents = 0;
            int roomEvents = 0;
            progress.CurrencyChanged += (amount, total) =>
            {
                Assert.AreEqual(3, amount);
                Assert.AreEqual(3, total);
                currencyEvents++;
            };
            progress.FragmentCollected += (id, text) =>
            {
                Assert.AreEqual("memory-1", id);
                Assert.AreEqual("잊힌 목소리", text);
                fragmentEvents++;
            };
            progress.RoomVisited += _ => roomEvents++;

            progress.AddCurrency(3);
            Assert.IsTrue(progress.CollectFragment("memory-1", "잊힌 목소리"));
            Assert.IsFalse(progress.CollectFragment("memory-1", "중복"));
            progress.VisitRoom("Room_A");
            progress.VisitRoom("Room_A");

            Assert.AreEqual(1, currencyEvents);
            Assert.AreEqual(1, fragmentEvents);
            Assert.AreEqual(1, roomEvents);
            Assert.AreEqual("잊힌 목소리", progress.FragmentTexts["memory-1"]);
        }
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
            Assert.IsFalse(_p.CanOpenFinalGate(), "세 조건이 전부 없으면 닫혀 있어야 한다");

            // 세 조건 중 정확히 하나씩만 빠진 상태를 각각 검증한다.
            // 단조 증가 경로(하나씩 채워가기)만 쓰면 && 체인에서 조건 하나가
            // 통째로 빠지는 회귀를 잡아내지 못하므로, 매번 나머지 둘은 채운 채
            // 나머지 하나만 비운 별도의 ProgressState로 확인한다.
            var missingRewind = new ProgressState();
            missingRewind.GrantAwareness();
            missingRewind.MarkFractureCleared();
            Assert.IsFalse(missingRewind.CanOpenFinalGate(), "Rewind 스킬만 빠졌는데 열리면 안 된다");

            var missingAwareness = new ProgressState();
            missingAwareness.UnlockSkill(EmotionId.Rewind);
            missingAwareness.MarkFractureCleared();
            Assert.IsFalse(missingAwareness.CanOpenFinalGate(), "자각만 빠졌는데 열리면 안 된다");

            var missingFractureCleared = new ProgressState();
            missingFractureCleared.UnlockSkill(EmotionId.Rewind);
            missingFractureCleared.GrantAwareness();
            Assert.IsFalse(missingFractureCleared.CanOpenFinalGate(), "균열 클리어만 빠졌는데 열리면 안 된다");

            _p.UnlockSkill(EmotionId.Rewind);
            _p.GrantAwareness();
            _p.MarkFractureCleared();
            Assert.IsTrue(_p.CanOpenFinalGate(), "세 조건이 전부 충족되면 열려야 한다");
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
