using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 잔재 마감 세트(9종)가 실제로 게임에서 쓰이는지 본다.
    //
    // 이 프로젝트에서 아트가 죽는 자리는 늘 같았다 — 규격대로 잘려 있는데 아무도 재생을
    // 요청하지 않는 것. 여기서는 잘린 스프라이트가 있는지가 아니라, 발판을 밟았을 때 상태
    // 클립이 바뀌고 충돌 연출이 실제로 생성되는지를 확인한다.
    public class ResidueCompletionArtTests
    {
        const string SceneName = "Zone_Residue_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static IEnumerator LoadResidue()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            GameManager.Instance.SetState(GameState.Playing);
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator 붕괴_발판이_상태_애니메이션을_재생한다()
        {
            yield return LoadResidue();

            var platform = Object.FindFirstObjectByType<CrumblingPlatform>();
            Assert.IsNotNull(platform, "잔재에 붕괴 발판이 없다.");

            var animator = platform.GetComponentInChildren<SpriteAnimator>();
            Assert.IsNotNull(animator, "붕괴 발판에 상태 애니메이터가 붙지 않았다 — 시트가 연결되지 않았다.");

            foreach (var clip in new[] { "PlatformCrack", "PlatformCollapse", "PlatformBroken", "PlatformRestore" })
                Assert.IsTrue(animator.Has(clip), clip + " 클립이 없다.");

            // 밟기 전에는 아무 클립도 돌지 않아야 한다. 자동 재생이 켜져 있으면 발판이
            // 처음부터 금 간 상태로 보인다.
            Assert.IsNull(animator.CurrentClip,
                "밟지도 않았는데 상태 클립이 재생 중이다(현재=" + animator.CurrentClip + ").");

            // 실제로 밟는다.
            var player = PlayerController.Instance;
            player.TeleportTo(platform.transform.position + new Vector3(0f, 2f, 0f));

            string seen = null;
            float deadline = Time.realtimeSinceStartup + 4f;
            while (seen == null && Time.realtimeSinceStartup < deadline)
            {
                PlayerInput.Injected = default;
                yield return new WaitForFixedUpdate();
                if (animator.CurrentClip != null) seen = animator.CurrentClip;
            }

            Debug.Log("===== 발판 상태 ===== 밟은 뒤 클립=" + seen + " 무너짐=" + platform.HasCrumbled);
            Assert.IsNotNull(seen, "발판을 밟았는데 상태 애니메이션이 재생되지 않는다.");

            // 되감기로 복구하면 복구 클립으로 넘어간다.
            deadline = Time.realtimeSinceStartup + 4f;
            while (!platform.HasCrumbled && Time.realtimeSinceStartup < deadline)
            { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(platform.HasCrumbled, "발판이 무너지지 않았다.");

            platform.Rewind();
            yield return null;

            Debug.Log("===== 발판 복구 ===== 클립=" + animator.CurrentClip);
            Assert.AreEqual("PlatformRestore", animator.CurrentClip,
                "되감기로 복구했는데 복구 클립이 재생되지 않는다.");
        }

        [UnityTest]
        public IEnumerator 충돌_연출이_실제로_생성된다()
        {
            yield return LoadResidue();

            Assert.IsNotNull(ImpactVFX.Instance, "지역에 ImpactVFX가 없다 — 충돌 시트가 연결되지 않았다.");

            var report = new StringBuilder();
            foreach (var name in new[] { "ImpactMelee", "ImpactWall", "ImpactLand", "ImpactHeavy" })
            {
                report.AppendLine("  " + name + " " + (ImpactVFX.Instance.Has(name) ? "있음" : "없음"));
                Assert.IsTrue(ImpactVFX.Instance.Has(name), name + " 효과가 등록되지 않았다.");
            }
            Debug.Log("===== 충돌 연출 =====\n" + report);

            // 실제로 오브젝트가 생기는지. 생성 → 재생 → 스스로 소멸까지가 한 세트다.
            int before = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length;
            ImpactVFX.Play("ImpactMelee", PlayerController.Instance.transform.position);
            yield return null;

            var spawned = GameObject.Find("ImpactVFX_ImpactMelee");
            Assert.IsNotNull(spawned, "ImpactVFX.Play를 불렀는데 연출 오브젝트가 생기지 않았다.");

            float deadline = Time.realtimeSinceStartup + 4f;
            while (spawned != null && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.IsTrue(spawned == null, "충돌 연출이 재생 후 스스로 사라지지 않는다.");
            Assert.Greater(before, 0, "씬에 렌더러가 하나도 없다.");
        }

        [UnityTest]
        public IEnumerator 숏컷에_봉쇄_해제_애니메이션이_붙어_있다()
        {
            yield return LoadResidue();

            int checkedCount = 0;
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
            {
                var sealTransform = shortcut.transform.Find("SealAnimation");
                Assert.IsNotNull(sealTransform, shortcut.Id + "에 봉쇄 애니메이션이 없다.");

                var animator = sealTransform.GetComponent<SpriteAnimator>();
                Assert.IsNotNull(animator, shortcut.Id + " 봉쇄 오브젝트에 애니메이터가 없다.");
                Assert.IsTrue(animator.Has("SealClose"), shortcut.Id + ": SealClose 클립이 없다.");
                Assert.IsTrue(animator.Has("SealOpen"), shortcut.Id + ": SealOpen 클립이 없다.");
                checkedCount++;
            }

            Debug.Log("===== 숏컷 봉쇄 애니메이션 " + checkedCount + "곳 =====");
            Assert.AreEqual(3, checkedCount, "잔재의 숏컷은 3개다.");
        }
    }
}
