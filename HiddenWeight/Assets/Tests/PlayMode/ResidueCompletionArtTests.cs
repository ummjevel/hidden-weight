using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
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
        public IEnumerator 공격체가_등록되고_날아가다_사라진다()
        {
            yield return LoadResidue();

            Assert.IsNotNull(ProjectileSpawner.Instance,
                "지역에 ProjectileSpawner가 없다 — 공격체 시트가 연결되지 않았다.");

            var report = new StringBuilder();
            foreach (var name in new[] { "ProjSplinter", "ProjClaw", "ProjShockwave",
                                         "BossWave", "BossNeedle", "BossRewindOrb" })
            {
                report.AppendLine("  " + name + " " + (ProjectileSpawner.Instance.Has(name) ? "있음" : "없음"));
                Assert.IsTrue(ProjectileSpawner.Instance.Has(name), name + "이 등록되지 않았다.");
            }
            Debug.Log("===== 공격체 =====\n" + report);

            // 실제로 쏘아 본다. 지형이 없는 높은 곳에서 쏘아 벽에 막히지 않게 한다.
            var origin = PlayerController.Instance.transform.position + new Vector3(0f, 6f, 0f);
            ProjectileSpawner.Fire("BossNeedle", origin, Vector2.right);
            yield return null;

            var projectile = Object.FindFirstObjectByType<Projectile>();
            Assert.IsNotNull(projectile, "Fire를 불렀는데 공격체가 생기지 않았다.");

            float startX = projectile.transform.position.x;
            for (int i = 0; i < 20 && projectile != null; i++) yield return null;

            Assert.IsTrue(projectile == null || projectile.transform.position.x > startX,
                "공격체가 제자리에 멈춰 있다.");

            // 수명이 다하면 스스로 사라진다.
            float deadline = Time.realtimeSinceStartup + 5f;
            while (projectile != null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(projectile == null, "공격체가 수명이 지나도 사라지지 않는다.");
        }

        [UnityTest]
        public IEnumerator 방마다_단일_배경만_있다()
        {
            yield return LoadResidue();

            int rooms = 0;
            var missing = new List<string>();
            var unwanted = new List<string>();

            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                rooms++;
                var background = room.transform.Find("Art/RoomBackground");
                if (background == null) missing.Add(room.name + "/RoomBackground");
                else if (background.GetComponent<RoomFittedBackground>() == null)
                    missing.Add(room.name + "/RoomFittedBackground");
                if (room.transform.Find("MotionBack") != null) unwanted.Add(room.name + "/MotionBack");
                if (room.transform.Find("MotionFront") != null) unwanted.Add(room.name + "/MotionFront");
            }

            Debug.Log("===== 잔재 단일 배경 ===== " + rooms + "방");
            Assert.IsEmpty(missing, "단일 배경이 빠진 방이 있다: " + string.Join(", ", missing));
            Assert.IsEmpty(unwanted, "제거해야 할 전경·배경 모션이 있다: " + string.Join(", ", unwanted));
            Assert.AreEqual(15, rooms, "잔재는 15룸이다.");

            var tilemaps = Object.FindObjectsByType<TilemapRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(tilemaps.Length, 0, "잔재 TilemapRenderer가 없다.");
            foreach (var tilemap in tilemaps)
                // 4K 배경은 카메라 고정 벽지라 실제 바닥을 그려 줄 수 없다 — 타일맵이 꺼지면
                // 바닥 전체가 "안 보이는데 부딪히는" 충돌이 된다.
                Assert.IsTrue(tilemap.enabled, tilemap.name + " 바닥 렌더러가 꺼져 있다 — 안 보이는 바닥이 된다.");

            AssertNoVisibleCollisionPlaceholders();
        }

        static void AssertNoVisibleCollisionPlaceholders()
        {
            var visible = new List<string>();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (renderer.enabled && renderer.sprite != null &&
                    renderer.sprite.name == "Tile" &&
                    renderer.GetComponent<Collider2D>() != null)
                    visible.Add(renderer.name);
            Assert.IsEmpty(visible, "충돌용 단색 블록이 보인다: " + string.Join(", ", visible));
        }

        [UnityTest]
        public IEnumerator 두_보스가_공격체_무브를_가진다()
        {
            yield return LoadResidue();

            int found = 0;
            foreach (var boss in Object.FindObjectsByType<HiddenWeight.Enemies.BossController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var moves = new SerializedObjectLikeReader(boss).Moves;
                Assert.IsTrue(moves.Contains("Projectile"),
                    boss.name + "에 공격체 무브가 없다: " + string.Join(",", moves));
                found++;
            }

            Debug.Log("===== 보스 공격체 무브 ===== " + found + "체");
            Assert.AreEqual(2, found, "잔재의 보스는 중간·지역 둘이다.");
        }

        // BossController의 moves는 직렬화 전용 필드라 런타임에서 읽을 방법이 없다.
        // 테스트에서만 리플렉션으로 들여다본다 — 이 하나 때문에 공개 API를 넓히지 않는다.
        sealed class SerializedObjectLikeReader
        {
            public readonly List<string> Moves = new List<string>();

            public SerializedObjectLikeReader(HiddenWeight.Enemies.BossController boss)
            {
                var field = typeof(HiddenWeight.Enemies.BossController).GetField("moves",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return;

                if (field.GetValue(boss) is System.Array array)
                    foreach (var move in array) Moves.Add(move.ToString());
            }
        }

        [UnityTest]
        public IEnumerator 상태_문양이_등록되고_위험할_때_켜진다()
        {
            yield return LoadResidue();

            var emblem = Object.FindFirstObjectByType<HiddenWeight.UI.StatusEmblem>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(emblem, "HUD에 상태 문양이 없다 — 상태 UI 시트가 연결되지 않았다.");

            foreach (var name in new[] { "StatusRewind", "StatusDanger", "StatusProgress" })
                Assert.IsTrue(emblem.Has(name), name + " 시퀀스가 등록되지 않았다.");

            // 마지막 한 칸이 남으면 위험 문양이 켜진다.
            var health = PlayerController.Instance.GetComponent<PlayerHealth>();
            while (health.Current > 1)
            {
                health.TakeDamage(1, (Vector2)PlayerController.Instance.transform.position + Vector2.right);
                // 무적 시간이 있으므로 풀릴 때까지 기다린다.
                float wait = Time.realtimeSinceStartup + 2f;
                while (health.IsInvulnerable && Time.realtimeSinceStartup < wait)
                { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            }
            yield return null;

            Debug.Log("===== 상태 문양 ===== 체력=" + health.Current + " 문양=" + emblem.CurrentSequence);
            Assert.AreEqual("StatusDanger", emblem.CurrentSequence,
                "체력이 한 칸 남았는데 위험 문양이 켜지지 않는다.");

            // 회복하면 꺼진다.
            health.RestoreFull();
            yield return null;

            Assert.AreNotEqual("StatusDanger", emblem.CurrentSequence,
                "회복했는데 위험 문양이 계속 켜져 있다.");
        }

        [UnityTest]
        public IEnumerator 지역_지도_상태_아이콘이_연결돼_있다()
        {
            yield return LoadResidue();
            var zone = GameManager.Instance.CurrentZoneData;
            Assert.IsNotNull(zone);
            Assert.IsNotNull(zone.mapStateIcons);
            Assert.AreEqual(8, zone.mapStateIcons.Length);
            Assert.IsTrue(System.Array.TrueForAll(zone.mapStateIcons, icon => icon != null),
                "지역 UI 아이콘 시트의 상태 행이 지도 데이터에 모두 연결돼야 한다.");
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
