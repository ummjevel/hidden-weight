using System.Collections;
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
    public class ResidueLoopCompletionTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 잔재_숏컷과_비밀방이_실제_양방향_통로를_가진다()
        {
            yield return LoadResidue();
            var passages = Object.FindObjectsByType<ShortcutPassage>(FindObjectsSortMode.None);
            Assert.AreEqual(10, passages.Length, "A/B/C/S1/S2 각각 양방향 통로 두 개가 필요하다.");
            int gated = 0;
            foreach (var passage in passages)
            {
                Assert.IsNotNull(passage.Destination, passage.name + "의 도착 지점이 없다.");
                if (passage.RequiredShortcut != null) gated++;
            }
            Assert.AreEqual(8, gated, "A/B/C/S2는 잠긴 통로, S1만 처음부터 열린 통로여야 한다.");
        }

        [UnityTest]
        public IEnumerator 잔재_마지막_출구가_잠금_조건과_다음_지역을_보여준다()
        {
            yield return LoadResidue();

            ZoneTrigger trigger = null;
            foreach (var candidate in Object.FindObjectsByType<ZoneTrigger>(FindObjectsSortMode.None))
                if (candidate.RequiredEncounterId == "residue_r12_boss") { trigger = candidate; break; }
            Assert.IsNotNull(trigger, "R12 보스 클리어 조건이 연결된 출구가 없다.");

            var exitVisual = trigger.transform.Find("RegionExitVisual");
            Assert.IsNotNull(exitVisual, "R12 우측 끝에 다음 지역 출구가 보이지 않는다.");
            var exitLabel = exitVisual.Find("ExitLabel").GetComponent<TextMesh>();
            Assert.That(exitLabel.text, Does.Contain("쓰러뜨려야"),
                "잠긴 출구가 보스 승리 조건을 알려 주지 않는다.");

            GameManager.Instance.Progress.MarkEncounterCleared("residue_r12_boss");
            yield return null;
            Assert.That(exitLabel.text, Does.Contain("응시"),
                "열린 출구가 다음 지역인 응시를 알려 주지 않는다.");
        }

        [UnityTest]
        public IEnumerator 지역_보스_전에는_나갈_수_없고_승리_후_응시로_전환한다()
        {
            yield return LoadResidue();

            var encounter = FindEncounter("residue_r12_boss");
            Assert.IsNotNull(encounter);
            var boss = encounter.BossEnemy;
            Assert.IsNotNull(boss);
            boss.GetComponent<BossController>().enabled = false; // 통합 조건만 검증하므로 공격은 멈춘다.

            ZoneTrigger trigger = null;
            foreach (var candidate in Object.FindObjectsByType<ZoneTrigger>(FindObjectsSortMode.None))
                if (candidate.RequiredEncounterId == "residue_r12_boss") { trigger = candidate; break; }
            Assert.IsNotNull(trigger, "R12 보스 클리어 조건이 연결된 출구가 없다.");
            var player = PlayerController.Instance;

            // 보스를 잡기 전 출구 접촉은 무시돼야 한다.
            player.TeleportTo(trigger.transform.position + Vector3.left * 3f);
            yield return new WaitForFixedUpdate();
            player.TeleportTo(trigger.transform.position);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("Zone_Residue_Full", SceneManager.GetActiveScene().name);

            // 전장을 시작한 뒤 보스를 처치한다.
            player.TeleportTo(encounter.transform.position);
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(encounter.IsRunning);
            while (boss.IsAlive) boss.TakeDamage(1, boss.transform.position + Vector3.left);
            yield return null;
            yield return null;

            Assert.IsTrue(GameManager.Instance.Progress.IsEncounterCleared("residue_r12_boss"));
            StoryFragment core = null;
            foreach (var fragment in Object.FindObjectsByType<StoryFragment>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (fragment.FragmentId == "residue_core") core = fragment;
            Assert.IsNotNull(core, "보스 승리 핵심 기억이 없다.");
            Assert.IsTrue(core.gameObject.activeSelf, "보스를 잡았는데 핵심 기억이 나타나지 않았다.");

            player.TeleportTo(trigger.transform.position + Vector3.left * 3f);
            yield return new WaitForFixedUpdate();
            player.TeleportTo(trigger.transform.position);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name == "Zone_Residue_Full" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual("Zone_Gaze_Full", SceneManager.GetActiveScene().name);
        }

        static IEnumerator LoadResidue()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;
            yield return null; // ResidueLoopRuntime의 한 프레임 지연 구성까지 기다린다.
        }

        static Encounter FindEncounter(string id)
        {
            foreach (var encounter in Object.FindObjectsByType<Encounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (encounter.Id == id) return encounter;
            return null;
        }
    }
}
