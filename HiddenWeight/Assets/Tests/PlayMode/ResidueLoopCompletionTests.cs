using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.UI;
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
            Assert.That(exitLabel.text, Does.Contain("처치하면"),
                "잠긴 출구가 보스 승리 조건을 알려 주지 않는다.");

            GameManager.Instance.Progress.MarkEncounterCleared("residue_r12_boss");
            yield return null;
            Assert.That(exitLabel.text, Does.Contain("응시"),
                "열린 출구가 다음 지역인 응시를 알려 주지 않는다.");
        }

        [UnityTest]
        public IEnumerator R09_일반_조우는_선택이고_R10과_R12_보스전은_통로를_잠근다()
        {
            yield return LoadResidue();

            foreach (string id in new[] { "residue_r09_main", "residue_r09_elite" })
            {
                var encounter = FindEncounter(id);
                Assert.IsNotNull(encounter, id + " 조우가 없다.");
                Assert.IsFalse(encounter.LocksTraversal,
                    id + "가 여전히 통로를 잠근다 — 적을 건너뛰고 R12에 도달할 수 있어야 한다.");
            }

            var midBoss = FindEncounter("residue_r10_boss");
            Assert.IsNotNull(midBoss, "R10 중간 보스 조우가 없다.");
            Assert.IsTrue(midBoss.LocksTraversal,
                "R10 중간 보스전이 선택 전투가 되었다 — 도착 전의 일반 적은 건너뛰되 보스전은 완료해야 한다.");

            var finalBoss = FindEncounter("residue_r12_boss");
            Assert.IsNotNull(finalBoss, "R12 최종 보스 조우가 없다.");
            Assert.IsTrue(finalBoss.LocksTraversal,
                "R12 최종 보스전까지 선택 전투가 되었다 — 최종 전장만은 승리 전까지 잠겨야 한다.");
        }

        [UnityTest]
        public IEnumerator R10_입구는_평상시에_열리고_보스전_잠금만_남는다()
        {
            yield return LoadResidue();

            foreach (string name in new[] { "R10_Wall_L", "R10_Wall_R" })
            {
                var wall = FindInactive(name);
                // 기존 Full 씬에는 벽이 저장돼 있어 런타임에서 끄고, 새로 생성한 씬에는
                // 빌더가 아예 만들지 않는다. 두 경우 모두 활성 벽만 없으면 된다.
                if (wall != null)
                    Assert.IsFalse(wall.activeSelf,
                        name + "이 상시 켜져 있어 R10 바닥 입구 또는 출구를 막는다.");
            }

            var encounter = FindEncounter("residue_r10_boss");
            Assert.IsNotNull(encounter);
            Assert.IsTrue(encounter.LocksTraversal,
                "상시 벽을 없애면서 보스전의 임시 잠금까지 제거되었다.");
        }

        [UnityTest]
        public IEnumerator R10_중간보스는_축소되고_승리후_출구계단은_일반점프로_이어진다()
        {
            yield return LoadResidue();

            var encounter = FindEncounter("residue_r10_boss");
            Assert.IsNotNull(encounter);
            var boss = encounter.BossEnemy;
            Assert.IsNotNull(boss);
            Assert.AreEqual(8, boss.Data.maxHealth, "R10 중간보스 체력이 완화되지 않았다.");
            Assert.That(boss.transform.localScale.x, Is.EqualTo(1.25f).Within(0.01f),
                "R10 중간보스가 여전히 공통 보스의 2배 크기를 사용한다.");

            var renderer = boss.GetComponentInChildren<SpriteRenderer>(true);
            Assert.IsNotNull(renderer);
            Assert.That(renderer.bounds.size.y, Is.LessThanOrEqualTo(4.1f),
                "R10 중간보스 그림이 플레이 화면을 지나치게 가린다.");
            Assert.That(renderer.bounds.center.x, Is.EqualTo(boss.transform.position.x).Within(0.08f),
                "R10 중간보스 실루엣 중심이 몸체 판정 중앙과 어긋난다.");
            var presentation = boss.GetComponent<ResidueBossPresentationGuard>();
            Assert.IsNotNull(presentation, "R10 중간보스의 잘린 공격 프레임 방지 보정이 없다.");
            Assert.AreEqual("WatcherAnimIdle", presentation.SafeClip);
            var bossAnimator = boss.GetComponentInChildren<SpriteAnimator>(true);
            Assert.IsNotNull(bossAnimator);
            yield return null;
            Assert.IsNotNull(bossAnimator.CurrentClip,
                "R10 중간보스 애니메이션이 정지되어 있다.");
            yield return new WaitForFixedUpdate();
            AssertBossGrounded(boss, "R10 중간보스");

            var low = FindInactive("R10_ExitStep_Low")?.GetComponent<BoxCollider2D>();
            var high = FindInactive("R10_ExitStep_High")?.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(low, "R10 출구의 낮은 계단이 없다.");
            Assert.IsNotNull(high, "R10 출구의 높은 계단이 없다.");

            float floorSurface = FindRoom("Room10").WorldBounds.min.y + 3f;
            Assert.That(low.bounds.max.y - floorSurface, Is.LessThanOrEqualTo(2.3f),
                "바닥에서 첫 출구 계단까지 일반 점프로 닿지 않는다.");
            Assert.That(high.bounds.max.y - low.bounds.max.y, Is.LessThanOrEqualTo(2.3f),
                "첫 계단에서 두 번째 출구 계단까지 일반 점프로 닿지 않는다.");

            Assert.That(high.bounds.min.x - low.bounds.max.x, Is.LessThanOrEqualTo(0.25f),
                "R10 출구 계단 사이의 수평 간격이 끊겨 있다.");
            Assert.That(FindRoom("Room10").WorldBounds.max.x - high.bounds.max.x,
                Is.LessThanOrEqualTo(0.6f), "마지막 계단과 R11 출구 사이가 점프로 닿지 않는다.");

            var connector = FindInactive("R10_R11_Connector")?.GetComponent<BoxCollider2D>();
            var room11 = FindRoom("Room11");
            Assert.IsNotNull(connector, "R10과 R11 사이의 이완 구간 연결 발판이 없다.");
            Assert.That(connector.bounds.min.x, Is.EqualTo(FindRoom("Room10").WorldBounds.max.x).Within(0.05f),
                "R10 마지막 계단 뒤에 보이지 않는 틈이 남아 있다.");
            Assert.That(connector.bounds.max.x, Is.EqualTo(room11.WorldBounds.min.x).Within(0.05f),
                "연결 발판과 R11 바닥 사이에 틈이 남아 있다.");
            Assert.IsNotNull(connector.transform.Find("PlatformSurface_Runtime/Art")
                    ?.GetComponent<SpriteRenderer>()?.sprite,
                "R10-R11 연결 발판은 밟히지만 그림이 없어 허공처럼 보인다.");
        }

        [UnityTest]
        public IEnumerator R12_최종보스_조우와_안전한_스프라이트_표시가_연결된다()
        {
            yield return LoadResidue();

            var encounter = FindEncounter("residue_r12_boss");
            Assert.IsNotNull(encounter, "R12 최종 보스 조우가 없다.");
            var trigger = encounter.GetComponent<Collider2D>();
            Assert.IsNotNull(trigger, "R12 최종 보스 조우 판정이 없다.");
            Assert.IsTrue(trigger.isTrigger, "R12 조우 판정이 고체 벽이라 전장에 진입할 수 없다.");

            var boss = encounter.BossEnemy;
            Assert.IsNotNull(boss, "R12 최종 보스가 조우에 연결되지 않았다.");
            var presentation = boss.GetComponent<ResidueBossPresentationGuard>();
            Assert.IsNotNull(presentation, "R12 최종 보스의 잘린 공격 프레임 방지 보정이 없다.");
            Assert.AreEqual("InstructorHalo", presentation.SafeClip);

            var animator = boss.GetComponentInChildren<SpriteAnimator>(true);
            Assert.IsNotNull(animator);
            yield return null;
            Assert.IsNotNull(animator.CurrentClip,
                "R12 최종 보스 애니메이션이 정지되어 있다.");
            yield return new WaitForFixedUpdate();
            AssertBossGrounded(boss, "R12 최종 보스");
        }

        [UnityTest]
        public IEnumerator R10_이후_R11_주동선은_점프가능하고_가짜장애물이_없다()
        {
            yield return LoadResidue();

            var room = FindRoom("Room11");
            Assert.IsNotNull(room);
            var first = FindInactive("R11_MainStep_A")?.GetComponent<BoxCollider2D>();
            var second = FindInactive("R11_MainStep_B")?.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(first, "R11 첫 진행 발판이 없다.");
            Assert.IsNotNull(second, "R11 두 번째 진행 발판이 없다.");

            float entrySurface = room.WorldBounds.min.y + 3f;
            Assert.That(first.bounds.max.y - entrySurface, Is.LessThanOrEqualTo(1.3f),
                "R11 진입 바닥에서 첫 발판까지 일반 점프로 닿지 않는다.");
            Assert.That(first.bounds.min.x - (room.WorldBounds.min.x + 10f), Is.LessThanOrEqualTo(2.1f),
                "R11 진입 턱과 첫 발판 사이의 수평 간격이 너무 넓다.");
            Assert.That(second.bounds.min.x - first.bounds.max.x, Is.LessThanOrEqualTo(1.6f),
                "R11 두 진행 발판 사이의 간격이 너무 넓다.");
            Assert.That(room.WorldBounds.min.x + 22f - second.bounds.max.x, Is.LessThanOrEqualTo(2.6f),
                "두 번째 발판과 R12 안전지대 사이의 간격이 너무 넓다.");

            foreach (string name in new[] { "R11_GallowsSilhouette", "R11_S3_Hint" })
            {
                var decoration = FindInactive(name);
                if (decoration == null) continue;
                foreach (var renderer in decoration.GetComponentsInChildren<SpriteRenderer>(true))
                    Assert.IsFalse(renderer.enabled, name + "가 통과 가능한 가짜 장애물로 남아 있다.");
            }
        }

        [UnityTest]
        public IEnumerator R12_입구는_평상시에_열리고_최종보스전_잠금만_남는다()
        {
            yield return LoadResidue();

            foreach (string name in new[] { "R12_Wall_L", "R12_Wall_R" })
            {
                var wall = FindInactive(name);
                if (wall != null)
                    Assert.IsFalse(wall.activeSelf,
                        name + "이 상시 켜져 있어 R12 최종 전장에 진입할 수 없다.");
            }

            var encounter = FindEncounter("residue_r12_boss");
            Assert.IsNotNull(encounter);
            Assert.IsTrue(encounter.LocksTraversal,
                "상시 벽을 없애면서 최종 보스전의 임시 잠금까지 제거되었다.");
        }

        [UnityTest]
        public IEnumerator 잔재_기억_제목과_본문이_확정된_직접적인_문구를_사용한다()
        {
            yield return LoadResidue();

            var expected = new Dictionary<string, (string title, string body)>
            {
                { "residue_s1", ("반복된 형상", "철망 안에 같은 형상이 반복되어 있다.") },
                { "residue_skill", ("되감기", "무너진 구조물을 복원한다.") },
                { "residue_r11", ("회랑의 형상", "벽의 홈마다 서로 다른 형상이 놓여 있다.") },
                { "residue_final", ("감춰진 눈", "벽 뒤에서 거대한 눈이 드러났다.") },
                { "residue_core", ("기억 파편", "기억의 교수대에서 발견한 파편.") },
            };
            var textField = typeof(StoryFragment).GetField("text",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(textField);

            var found = new HashSet<string>();
            foreach (var fragment in Object.FindObjectsByType<StoryFragment>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!expected.TryGetValue(fragment.FragmentId, out var copy)) continue;
                found.Add(fragment.FragmentId);
                Assert.AreEqual(copy.title, MemoryCatalog.TitleFor(fragment.FragmentId));
                Assert.AreEqual(copy.body, textField.GetValue(fragment));
            }

            CollectionAssert.AreEquivalent(expected.Keys, found,
                "잔재 기억 다섯 개 중 씬에 없거나 문구가 다른 항목이 있다.");
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

        static void AssertBossGrounded(Enemy boss, string label)
        {
            Physics2D.SyncTransforms();
            var body = boss.GetComponent<Collider2D>();
            var art = boss.GetComponentInChildren<SpriteAnimator>(true)?.Renderer;
            Assert.IsNotNull(body, $"{label} 몸체 판정이 없다.");
            Assert.IsNotNull(art, $"{label} 그림이 없다.");

            var hit = Physics2D.Raycast(body.bounds.center, Vector2.down,
                body.bounds.extents.y + 0.25f, LayerMask.GetMask("Ground", "Wall"));
            Assert.IsNotNull(hit.collider, $"{label}가 바닥에서 떠 있다.");
            Assert.That(body.bounds.min.y - hit.point.y, Is.LessThanOrEqualTo(0.03f),
                $"{label}의 물리 몸체가 바닥에 닿지 않았다.");
            Assert.That(Mathf.Abs(art.bounds.min.y - body.bounds.min.y), Is.LessThanOrEqualTo(0.05f),
                $"{label} 그림의 발 기준과 몸체 바닥이 어긋났다.");
        }

        static GameObject FindInactive(string name)
        {
            foreach (var transform in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name == name) return transform.gameObject;
            return null;
        }

        static Room FindRoom(string name)
        {
            foreach (var room in Object.FindObjectsByType<Room>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (room.name == name) return room;
            return null;
        }
    }
}
