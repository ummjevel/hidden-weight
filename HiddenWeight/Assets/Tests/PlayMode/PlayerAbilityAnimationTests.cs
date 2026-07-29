using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // 능력·피격 애니메이션이 실제로 화면에서 바뀌는지 본다.
    //
    // PlayerAnimationWiringTests는 "클립이 프리팹에 붙어 있는가"까지만 본다. 붙어 있어도
    // 아무도 재생을 요청하지 않으면 게임에서는 여전히 안 보인다 — 실제로 숨죽이기·자각·반응
    // VFX가 그 상태였다. 여기서는 입력을 흘려넣어 클립이 바뀌는 것까지 확인한다.
    public class PlayerAbilityAnimationTests
    {
        const string SceneName = "Zone_Gaze_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static IEnumerator LoadGaze()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            // 지역 씬을 단독으로 로드하면 상태가 Boot로 남는다. 자각은 Playing에서만
            // 동작하므로(AwarenessSystem), 실제 플레이 흐름과 같은 상태로 맞춰 준다.
            GameManager.Instance.SetState(GameState.Playing);
            yield return new WaitForFixedUpdate();
        }

        static PlayerAnimator Animator => PlayerController.Instance.GetComponent<PlayerAnimator>();

        // 지정한 클립이 나올 때까지 최대 steps만큼 기다린다. 어떤 클립을 거쳐 갔는지도 남긴다.
        static IEnumerator WaitForClip(string clip, int steps, PlayerInput.Frame frame)
        {
            for (int i = 0; i < steps; i++)
            {
                PlayerInput.Injected = frame;
                yield return new WaitForFixedUpdate();
                if (Animator != null && Animator.CurrentClip == clip) yield break;
            }
        }

        [UnityTest]
        public IEnumerator 숨죽이기가_웅크린_클립으로_바뀌고_걸어도_유지된다()
        {
            yield return LoadGaze();

            var progress = GameManager.Instance.Progress;
            progress.UnlockSkill(EmotionId.Hush);
            EmotionSkillController.Instance.RefreshActive();
            yield return null;

            Assert.IsNotNull(Animator, "PlayerAnimator가 없다.");
            string before = Animator.CurrentClip;

            // K 홀드 — 실제 입력 경로로 스킬을 켠다.
            var hold = new PlayerInput.Frame { skillHeld = true };
            yield return WaitForClip("HushMove", 200, hold);

            Debug.Log("===== 숨죽이기 ===== 이전=" + before + " → " + Animator.CurrentClip);
            Assert.AreEqual("HushMove", Animator.CurrentClip,
                "숨죽였는데 웅크린 클립(HushMove)이 재생되지 않는다.");

            // 웅크린 채 걸어도 자세가 풀리면 안 된다. 상태 클립이 덮어쓰면 여기서 깨진다.
            var walk = new PlayerInput.Frame { skillHeld = true, horizontal = 1f };
            for (int i = 0; i < 60; i++) { PlayerInput.Injected = walk; yield return new WaitForFixedUpdate(); }

            Assert.AreEqual("HushMove", Animator.CurrentClip,
                "숨죽인 채 걸었더니 자세가 이동 클립으로 덮어써졌다(현재=" + Animator.CurrentClip + ").");

            // 손을 떼면 마무리 클립을 거쳐 상태 클립으로 돌아온다.
            yield return WaitForClip("HushEnd", 60, default);
            Assert.AreEqual("HushEnd", Animator.CurrentClip, "숨죽이기를 풀 때 마무리 클립이 나오지 않는다.");

            for (int i = 0; i < 90; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Debug.Log("===== 숨죽이기 해제 후 ===== " + Animator.CurrentClip);
            Assert.IsTrue(Animator.CurrentClip != null && Animator.CurrentClip.StartsWith("Player"),
                "숨죽이기를 풀었는데 상태 클립으로 돌아오지 않았다(현재=" + Animator.CurrentClip + ").");
        }

        [UnityTest]
        public IEnumerator 자각이_지각_루프_클립으로_바뀐다()
        {
            yield return LoadGaze();

            GameManager.Instance.Progress.GrantAwareness();
            yield return null;

            var hold = new PlayerInput.Frame { awarenessHeld = true };
            yield return WaitForClip("AwarenessLoop", 200, hold);

            Debug.Log("===== 자각 ===== " + Animator.CurrentClip
                + " / IsActive=" + AwarenessSystem.Instance.IsActive);

            Assert.AreEqual("AwarenessLoop", Animator.CurrentClip,
                "자각을 켰는데 지각 루프 클립이 재생되지 않는다.");

            // 손을 떼면 상태 클립으로 돌아온다.
            for (int i = 0; i < 90; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(Animator.CurrentClip != null && Animator.CurrentClip.StartsWith("Player"),
                "자각을 풀었는데 상태 클립으로 돌아오지 않았다(현재=" + Animator.CurrentClip + ").");
        }

        [UnityTest]
        public IEnumerator 피격이_반응_클립으로_바뀐다()
        {
            yield return LoadGaze();

            var health = PlayerController.Instance.GetComponent<PlayerHealth>();
            Assert.IsNotNull(health, "PlayerHealth가 없다.");

            health.TakeDamage(1, (Vector2)PlayerController.Instance.transform.position + Vector2.right * 2f);
            yield return null;

            Debug.Log("===== 피격 ===== " + Animator.CurrentClip + " 체력=" + health.Current);
            Assert.AreEqual("PlayerHit", Animator.CurrentClip,
                "피격했는데 반응 클립이 재생되지 않는다.");

            // 짧게 끼워 넣는 클립이라 끝나면 상태 클립으로 돌아와야 한다.
            for (int i = 0; i < 90; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(Animator.CurrentClip != null && Animator.CurrentClip.StartsWith("Player")
                && Animator.CurrentClip != "PlayerHit",
                "피격 클립이 끝난 뒤 상태 클립으로 돌아오지 않았다(현재=" + Animator.CurrentClip + ").");
        }
    }
}
