using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 앞선 검토에서 "코드 연결만 확인했고 실제로 되는지는 안 봤다"고 남긴 것들을 직접 돌려 본다.
    // 숏컷 개방, S3 조건, 보스 회피 가능성, 그리고 적이 실제로 순찰하는지.
    public class ResidueVerificationTests
    {
        const string SceneName = "Zone_Residue_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static void ResetProgress()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
        }

        // 적이 제자리에서 떠는지, 실제로 순찰하는지.
        [UnityTest]
        public IEnumerator 적이_제자리에서_떨지_않고_순찰한다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            // 활성 상태의 순찰 적만 본다(조우 소속은 비활성으로 대기 중이다).
            var patrols = new System.Collections.Generic.List<EnemyPatrol>();
            foreach (var patrol in Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None))
                if (patrol.isActiveAndEnabled) patrols.Add(patrol);
            Assert.IsNotEmpty(patrols, "활성 순찰 적이 하나도 없다.");

            // 바닥에 안착할 시간을 준다.
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();

            var start = new Vector3[patrols.Count];
            var minX = new float[patrols.Count];
            var maxX = new float[patrols.Count];
            var flips = new int[patrols.Count];
            var lastDir = new float[patrols.Count];

            for (int i = 0; i < patrols.Count; i++)
            {
                start[i] = patrols[i].transform.position;
                minX[i] = maxX[i] = start[i].x;
                lastDir[i] = Mathf.Sign(patrols[i].transform.localScale.x);
            }

            for (int step = 0; step < 200; step++)
            {
                yield return new WaitForFixedUpdate();
                for (int i = 0; i < patrols.Count; i++)
                {
                    if (patrols[i] == null) continue;
                    float x = patrols[i].transform.position.x;
                    minX[i] = Mathf.Min(minX[i], x);
                    maxX[i] = Mathf.Max(maxX[i], x);

                    float dir = Mathf.Sign(patrols[i].transform.localScale.x);
                    if (!Mathf.Approximately(dir, lastDir[i])) { flips[i]++; lastDir[i] = dir; }
                }
            }

            var report = new StringBuilder();
            int shaking = 0;
            for (int i = 0; i < patrols.Count; i++)
            {
                float range = maxX[i] - minX[i];
                bool bad = range < 1f || flips[i] > 20;
                if (bad) shaking++;
                string where = "(밖)";
                if (patrols[i] != null)
                    foreach (var candidate in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                        if (candidate.WorldBounds.Contains(new Vector3(patrols[i].transform.position.x,
                                                                      patrols[i].transform.position.y, 0f)))
                            where = candidate.name;

                string extra = "";
                if (patrols[i] != null)
                {
                    var body = patrols[i].GetComponent<Rigidbody2D>();
                    var down = Physics2D.Raycast(patrols[i].transform.position, Vector2.down, 5f,
                        LayerMask.GetMask("Ground"));
                    var touching = Physics2D.OverlapBoxAll(patrols[i].transform.position, new Vector2(1.1f, 1.1f), 0f);
                    var names = new StringBuilder();
                    foreach (var t in touching)
                        if (t.transform != patrols[i].transform)
                            names.Append(t.name).Append('/').Append(LayerMask.LayerToName(t.gameObject.layer)).Append(' ');

                    extra = " pos=" + patrols[i].transform.position.ToString("F1")
                          + " vel=" + body.linearVelocity.ToString("F1")
                          + " sleep=" + body.IsSleeping()
                          + " enabled=" + patrols[i].enabled
                          + " speed=" + patrols[i].GetComponent<Enemy>().Data.moveSpeed
                          + " 접촉=[" + names + "]";
                }

                report.AppendLine("  " + where + extra
                    + " 이동폭 " + range.ToString("F2") + " 방향전환 " + flips[i] + (bad ? "  ← 떨림" : ""));
            }
            Debug.Log("===== 적 순찰 (4초) =====\n" + report);

            Assert.AreEqual(0, shaking,
                shaking + "마리가 제자리에서 떨고 있다(이동폭 1 미만 또는 4초에 방향전환 20회 초과).\n" + report);
        }

        // 숏컷 A: R05 첫 필수 복원물을 되감으면 즉시 열리는가.
        [UnityTest]
        public IEnumerator 숏컷A가_되감기로_열린다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Shortcut shortcutA = null;
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
                if (shortcut.Id == "residue_shortcut_a") shortcutA = shortcut;
            Assert.IsNotNull(shortcutA, "숏컷 A가 씬에 없다.");
            Assert.IsFalse(shortcutA.IsOpen, "숏컷 A가 처음부터 열려 있다.");

            GameObject primaryRestore = null;
            foreach (var rewindable in Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
                if (rewindable.name == "R05_PrimaryRestore") primaryRestore = rewindable.gameObject;
            Assert.IsNotNull(primaryRestore, "R05 첫 필수 복원물을 찾지 못했다.");

            // 중력으로 밀려난 뒤 되감는다.
            for (int i = 0; i < 150; i++) yield return new WaitForFixedUpdate();
            primaryRestore.GetComponent<Rewindable>().Rewind();
            yield return null;

            Debug.Log("===== 숏컷 A ===== 되감기 후 열림=" + shortcutA.IsOpen
                + " 저장됨=" + GameManager.Instance.Progress.IsShortcutOpen("residue_shortcut_a"));

            Assert.IsTrue(shortcutA.IsOpen, "첫 필수 복원물을 복원했는데 숏컷 A가 열리지 않았다.");
            Assert.IsTrue(GameManager.Instance.Progress.IsShortcutOpen("residue_shortcut_a"),
                "숏컷 A 개방이 저장되지 않았다.");
        }

        // 숏컷 B: 안전/속도 경로 중 어느 도르래를 택해도 즉시 열린다.
        [UnityTest]
        public IEnumerator 숏컷B는_도르래_하나를_복원하면_열린다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Shortcut shortcutB = null;
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
                if (shortcut.Id == "residue_shortcut_b") shortcutB = shortcut;
            Assert.IsNotNull(shortcutB, "숏컷 B가 씬에 없다.");

            var room = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            Bounds r08 = default;
            foreach (var candidate in room) if (candidate.name == "Room08") r08 = candidate.WorldBounds;

            var pulleys = new System.Collections.Generic.List<Rewindable>();
            foreach (var rewindable in Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
                if (r08.Contains(new Vector3(rewindable.transform.position.x, rewindable.transform.position.y, 0f)))
                    pulleys.Add(rewindable);
            Assert.GreaterOrEqual(pulleys.Count, 2, "R08에 도르래가 2개 있어야 한다.");

            for (int i = 0; i < 150; i++) yield return new WaitForFixedUpdate();

            pulleys[0].Rewind();
            yield return null;
            bool openedAfterFirst = shortcutB.IsOpen;

            Debug.Log("===== 숏컷 B ===== 하나만 복원 후 열림=" + openedAfterFirst
                + " 저장됨=" + GameManager.Instance.Progress.IsShortcutOpen("residue_shortcut_b"));

            Assert.IsTrue(openedAfterFirst, "도르래 하나를 복원했는데 숏컷 B가 열리지 않았다.");
            Assert.IsTrue(GameManager.Instance.Progress.IsShortcutOpen("residue_shortcut_b"),
                "숏컷 B 개방이 저장되지 않았다.");
        }

        // S3: 조건을 만족하기 전에는 샤프트 입구가 막혀 있어야 한다.
        [UnityTest]
        public IEnumerator S3_입구는_조건_전에는_막혀_있다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var progress = GameManager.Instance.Progress;
            Assert.IsFalse(progress.CanOpenFinalGate(), "시작부터 최종 조건이 충족돼 있다.");

            Gate finalGate = null;
            foreach (var gate in Object.FindObjectsByType<Gate>(FindObjectsSortMode.None))
                if (!gate.IsOpen) finalGate = gate;
            Assert.IsNotNull(finalGate, "닫힌 최종 게이트를 찾지 못했다.");

            bool closedBefore = !finalGate.IsOpen;

            // 조건 3가지를 모두 채운다: 되감기 보유 + 자각 + 균열 클리어.
            progress.UnlockSkill(EmotionId.Rewind);
            progress.GrantAwareness();
            progress.MarkFractureCleared();
            yield return null;

            Debug.Log("===== S3 조건 ===== 조건 전 닫힘=" + closedBefore
                + " / 조건 후 열림=" + finalGate.IsOpen);

            Assert.IsTrue(closedBefore, "조건을 만족하기 전인데 게이트가 열려 있다.");
            Assert.IsTrue(finalGate.IsOpen, "조건을 모두 채웠는데 게이트가 열리지 않는다.");
        }
    }
}
