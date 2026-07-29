using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 응시 아트가 실제로 씬에 붙었는지 본다.
    //
    // 잔재에서 겪은 것과 같은 실패를 막는 것이 목적이다 — 시트가 규격대로 잘려 있어도
    // 빌더가 이름으로 찾지 못하면 그 오브젝트만 조용히 플레이스홀더로 남는다.
    // 실제로 밀고하는 입 3마리가 그 상태였다(서브에셋이 갱신되지 않아 이름 조회가 실패).
    public class GazeArtWiringTests
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
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator 모든_적과_보스에_전용_애니메이션이_붙어_있다()
        {
            yield return LoadGaze();

            var counts = new Dictionary<string, int>();
            var missing = new List<string>();

            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var animator = enemy.GetComponentInChildren<SpriteAnimator>(true);
                if (animator == null)
                {
                    missing.Add(enemy.name + " @ " + enemy.transform.position.ToString("F0"));
                    continue;
                }

                string clip = animator.CurrentClip;
                // 자동 재생이 켜져 있으므로 첫 클립 이름으로 어느 시트를 쓰는지 알 수 있다.
                foreach (var prefix in new[] { "Pilgrim", "Mouth", "Audience", "Judge",
                                               "Gatekeeper", "AllEyes" })
                {
                    if (!animator.Has(prefix + "Idle")) continue;
                    counts.TryGetValue(prefix, out int n);
                    counts[prefix] = n + 1;
                    break;
                }
            }

            var report = new StringBuilder();
            foreach (var pair in counts) report.AppendLine($"  {pair.Key,-12} {pair.Value}체");
            Debug.Log("===== 응시 적·보스 아트 =====\n" + report);

            Assert.IsEmpty(missing, "애니메이터가 없는 적이 있다: " + string.Join(", ", missing));

            // 배치 수는 빌더가 정한 값이다. 하나라도 아트를 못 찾으면 여기서 어긋난다.
            Assert.AreEqual(9, counts.GetValueOrDefault("Pilgrim"), "눈먼 순례자 9체");
            Assert.AreEqual(3, counts.GetValueOrDefault("Mouth"), "밀고하는 입 3체");
            Assert.AreEqual(1, counts.GetValueOrDefault("Audience"), "매달린 관객 1체");
            Assert.AreEqual(1, counts.GetValueOrDefault("Judge"), "얼굴 없는 재판관 1체");
            Assert.AreEqual(1, counts.GetValueOrDefault("Gatekeeper"), "홍채의 문지기 1체");
            Assert.AreEqual(1, counts.GetValueOrDefault("AllEyes"), "만인의 시선 1체");
        }

        [UnityTest]
        public IEnumerator 열다섯_방에_배경_3층과_모션이_있다()
        {
            yield return LoadGaze();

            int rooms = 0;
            var missing = new List<string>();

            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                rooms++;
                var art = room.transform.Find("Art");
                if (art == null) { missing.Add(room.name + "(Art 없음)"); continue; }

                foreach (var layer in new[] { "BG_Far", "BG_Mid", "FG_Overlay" })
                    if (art.Find(layer) == null) missing.Add($"{room.name}/{layer}");

                if (room.transform.Find("MotionBack") == null) missing.Add(room.name + "/MotionBack");
                if (room.transform.Find("MotionFront") == null) missing.Add(room.name + "/MotionFront");
            }

            Debug.Log("===== 응시 방 아트 ===== " + rooms + "방");
            Assert.AreEqual(15, rooms, "응시는 15룸이다.");
            Assert.IsEmpty(missing, "배경·모션이 빠진 방이 있다: " + string.Join(", ", missing));
        }

        [UnityTest]
        public IEnumerator 충돌_연출과_공격체가_응시_전용으로_등록된다()
        {
            yield return LoadGaze();

            Assert.IsNotNull(ImpactVFX.Instance, "응시에 ImpactVFX가 없다.");
            Assert.IsNotNull(ProjectileSpawner.Instance, "응시에 ProjectileSpawner가 없다.");

            foreach (var name in new[] { "GazeImpactMelee", "GazeImpactBeam",
                                         "GazeImpactLand", "GazeImpactGuardBreak" })
                Assert.IsTrue(ImpactVFX.Instance.Has(name), name + " 효과가 없다.");

            foreach (var name in new[] { "GazeProjSound", "GazeProjScream", "GazeProjShadow",
                                         "GazeProjVerdict", "GazeBossScanBeam", "GazeBossFalseEye" })
                Assert.IsTrue(ProjectileSpawner.Instance.Has(name), name + " 공격체가 없다.");

            // 잔재 이름이 섞여 들어오지 않았는지도 본다 — 지역별 아트 폴더 분리가 깨지면
            // 여기서 잡힌다.
            Assert.IsFalse(ProjectileSpawner.Instance.Has("ProjSplinter"),
                "잔재 공격체가 응시에 등록됐다 — 아트 폴더 분리가 깨졌다.");
        }

        [UnityTest]
        public IEnumerator 숏컷에_응시_봉쇄_애니메이션이_붙어_있다()
        {
            yield return LoadGaze();

            int found = 0;
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
            {
                var sealTransform = shortcut.transform.Find("SealAnimation");
                Assert.IsNotNull(sealTransform, shortcut.Id + "에 봉쇄 애니메이션이 없다.");

                var animator = sealTransform.GetComponent<SpriteAnimator>();
                Assert.IsNotNull(animator, shortcut.Id + " 봉쇄 오브젝트에 애니메이터가 없다.");
                Assert.IsTrue(animator.Has("GazeSealClose"), shortcut.Id + ": GazeSealClose가 없다.");
                Assert.IsTrue(animator.Has("GazeSealOpen"), shortcut.Id + ": GazeSealOpen이 없다.");
                found++;
            }

            Assert.AreEqual(3, found, "응시의 숏컷은 3개다.");
        }
    }
}
