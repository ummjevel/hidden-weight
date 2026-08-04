using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 균열 아트가 실제로 씬에 붙었는지 본다. GazeArtWiringTests와 같은 목적이다 —
    // 시트가 규격대로 잘려 있어도 빌더가 이름으로 찾지 못하면 그 오브젝트만 조용히
    // 플레이스홀더로 남는다.
    //
    // 균열에서는 실제로 그 실패를 한 번 재현했다: 시트를 자른 직후 같은 세션에서 이름을
    // 조회하면 서브에셋이 아직 보이지 않아 적 12체 전부가 플레이스홀더로 지어졌다.
    public class FractureArtWiringTests
    {
        const string SceneName = "Zone_Fracture_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static IEnumerator LoadFracture()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator 모든_적과_보스에_전용_애니메이션이_붙어_있다()
        {
            yield return LoadFracture();

            var counts = new Dictionary<string, int>();
            var missing = new List<string>();

            foreach (var enemy in Object.FindObjectsByType<Enemy>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var animator = enemy.GetComponentInChildren<SpriteAnimator>(true);
                if (animator == null)
                {
                    missing.Add(enemy.name + " @ " + enemy.transform.position.ToString("F0"));
                    continue;
                }

                foreach (var prefix in new[] { "Sprout", "Precursor", "Collector", "SplitSelf",
                                               "SecondHand", "NotYetMe" })
                {
                    if (!animator.Has(prefix + "Idle")) continue;
                    counts.TryGetValue(prefix, out int n);
                    counts[prefix] = n + 1;
                    break;
                }
            }

            var report = new StringBuilder();
            foreach (var pair in counts) report.AppendLine($"  {pair.Key,-12} {pair.Value}체");
            Debug.Log("===== 균열 적·보스 아트 =====\n" + report);

            Assert.IsEmpty(missing, "애니메이터가 없는 적이 있다: " + string.Join(", ", missing));

            // 배치 수는 빌더가 정한 값이다. 하나라도 아트를 못 찾으면 여기서 어긋난다.
            Assert.AreEqual(7, counts.GetValueOrDefault("Sprout"), "불안 새싹 7체");
            Assert.AreEqual(2, counts.GetValueOrDefault("Precursor"), "선행 그림자 2체");
            Assert.AreEqual(2, counts.GetValueOrDefault("Collector"), "가능성 수집자 2체");
            Assert.AreEqual(1, counts.GetValueOrDefault("SplitSelf"), "갈라진 자아 1체");
            Assert.AreEqual(1, counts.GetValueOrDefault("SecondHand"), "초침의 감시자 1체");
            Assert.AreEqual(1, counts.GetValueOrDefault("NotYetMe"), "아직 오지 않은 나 1체");
        }

        // 거울상이 본체와 같은 그림이어야 한다. 한쪽만 플레이스홀더면 실루엣만 보고
        // 실체를 골라낼 수 있어 갈라진 자아 전투가 성립하지 않는다.
        [UnityTest]
        public IEnumerator 갈라진_자아의_거울상도_같은_아트를_쓴다()
        {
            yield return LoadFracture();

            var mirrors = new List<GameObject>();
            foreach (var transform in Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name == "SplitSelf_Mirror") mirrors.Add(transform.gameObject);

            Assert.AreEqual(1, mirrors.Count, "거울상은 1개다.");

            var animator = mirrors[0].GetComponentInChildren<SpriteAnimator>(true);
            Assert.IsNotNull(animator, "거울상에 애니메이터가 없다 — 플레이스홀더로 남았다.");
            Assert.IsTrue(animator.Has("SplitSelfIdle"), "거울상이 본체와 다른 아트를 쓴다.");
        }

        [UnityTest]
        public IEnumerator 열다섯_방에_단일_배경만_있다()
        {
            yield return LoadFracture();

            int rooms = 0;
            var missing = new List<string>();
            var unwanted = new List<string>();

            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                rooms++;
                var art = room.transform.Find("Art");
                if (art == null) { missing.Add(room.name + "(Art 없음)"); continue; }

                var background = art.Find("RoomBackground");
                if (background == null) missing.Add(room.name + "/RoomBackground");
                else if (background.GetComponent<CameraLockedRoomBackground>() == null)
                    missing.Add(room.name + "/CameraLockedRoomBackground");

                if (art.GetComponent<RoomVisualCuller>() == null)
                    missing.Add(room.name + "/RoomVisualCuller");

                foreach (var path in new[]
                         {
                             "BG_Far", "BG_Mid", "FG_Overlay",
                             "Far", "Mid", "Foreground"
                         })
                    if (art.Find(path) != null) unwanted.Add($"{room.name}/Art/{path}");
                if (room.transform.Find("MotionBack") != null) unwanted.Add(room.name + "/MotionBack");
                if (room.transform.Find("MotionFront") != null) unwanted.Add(room.name + "/MotionFront");
            }

            Debug.Log("===== 균열 방 아트 ===== " + rooms + "방");
            Assert.AreEqual(15, rooms, "균열은 15룸이다.");
            Assert.IsEmpty(missing, "단일 배경이 빠진 방이 있다: " + string.Join(", ", missing));
            Assert.IsEmpty(unwanted, "제거해야 할 레거시 전경·모션이 있다: " + string.Join(", ", unwanted));

            // 4K 배경은 카메라 고정 벽지라 실제 바닥 위치를 그려 줄 수 없다 — 타일맵이
            // 꺼져 있으면 바닥 전체가 "안 보이는데 부딪히는" 충돌이 된다.
            var tilemaps = Object.FindObjectsByType<TilemapRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(tilemaps.Length, 0, "균열 TilemapRenderer가 없다.");
            foreach (var tilemap in tilemaps)
                Assert.IsTrue(tilemap.enabled, tilemap.name + " 바닥 렌더러가 꺼져 있다 — 안 보이는 바닥이 된다.");

            var visibleCollisionPlaceholders = new List<string>();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (renderer.enabled && renderer.sprite != null &&
                    renderer.sprite.name == "Tile" &&
                    renderer.GetComponent<Collider2D>() != null)
                    visibleCollisionPlaceholders.Add(renderer.name);
            Assert.IsEmpty(
                visibleCollisionPlaceholders,
                "충돌용 단색 블록이 보인다: " +
                string.Join(", ", visibleCollisionPlaceholders));
        }

        // 같은 방의 고정 발판과 회전 발판이 같은 그림이어야 한다. 회전 발판만 플레이스홀더로
        // 남으면 "겉모습으로 구분되지 않는다"는 균열의 규칙이 깨진다.
        [UnityTest]
        public IEnumerator 회전_발판도_균열_발판_아트를_쓴다()
        {
            yield return LoadFracture();

            int orbits = 0;
            foreach (var orbit in Object.FindObjectsByType<OrbitPlatform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // 지역 아트는 루트를 끄고 스케일 1 자식에 그린다(ApplyPlatformArt) —
                // 보이는 렌더러를 찾아 검사해야 한다.
                SpriteRenderer visible = null;
                foreach (var renderer in orbit.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled) { visible = renderer; break; }

                Assert.IsNotNull(visible, orbit.name + "에 보이는 렌더러가 없다.");
                Assert.IsNotNull(visible.sprite, orbit.name + "에 스프라이트가 없다.");
                StringAssert.StartsWith("FracturePlatform", visible.sprite.name,
                    orbit.name + "이 플레이스홀더 발판으로 남았다.");
                orbits++;
            }

            Debug.Log("===== 균열 회전 발판 ===== " + orbits + "개");
            Assert.Greater(orbits, 0, "회전 발판이 하나도 없다.");
        }

        [UnityTest]
        public IEnumerator 안전_발판도_균열_발판_아트를_쓴다()
        {
            yield return LoadFracture();

            int platforms = 0;
            foreach (var transform in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.name != "SafePlatform") continue;

                SpriteRenderer visible = null;
                foreach (var renderer in
                         transform.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled) { visible = renderer; break; }

                Assert.IsNotNull(visible, transform.name + "에 보이는 렌더러가 없다.");
                Assert.IsNotNull(visible.sprite, transform.name + "에 스프라이트가 없다.");
                StringAssert.StartsWith(
                    "FracturePlatform",
                    visible.sprite.name,
                    transform.name + "이 흰색 플레이스홀더 발판으로 남았다.");
                platforms++;
            }

            Assert.Greater(platforms, 0, "검사할 안전 발판이 없다.");
        }

        [UnityTest]
        public IEnumerator 이동_발판도_균열_발판_아트를_쓴다()
        {
            yield return LoadFracture();

            int platforms = 0;
            foreach (var platform in Object.FindObjectsByType<MovingPlatform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SpriteRenderer visible = null;
                foreach (var renderer in
                         platform.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled) { visible = renderer; break; }

                Assert.IsNotNull(visible, platform.name + "에 보이는 렌더러가 없다.");
                Assert.IsNotNull(visible.sprite, platform.name + "에 스프라이트가 없다.");
                StringAssert.StartsWith(
                    "FracturePlatform",
                    visible.sprite.name,
                    platform.name + "이 흰색 플레이스홀더 발판으로 남았다.");
                platforms++;
            }

            Assert.Greater(platforms, 0, "검사할 이동 발판이 없다.");
        }

        [UnityTest]
        public IEnumerator 충돌_연출과_공격체가_균열_전용으로_등록된다()
        {
            yield return LoadFracture();

            Assert.IsNotNull(ImpactVFX.Instance, "균열에 ImpactVFX가 없다.");
            Assert.IsNotNull(ProjectileSpawner.Instance, "균열에 ProjectileSpawner가 없다.");

            // 런타임이 이름 그대로 부르는 연출들이다(PlayerAttack / PlayerController /
            // BossController). 하나라도 없으면 그 순간에 아무것도 보이지 않는다.
            foreach (var name in new[] { "ImpactMelee", "ImpactHeavy", "ImpactLand", "ImpactWall",
                                         "BossRing", "BossRupture" })
                Assert.IsTrue(ImpactVFX.Instance.Has(name), name + " 효과가 없다.");

            foreach (var name in new[] { "FractureProjShards", "FractureProjArc", "FractureProjRing",
                                         "FractureBossShard", "FractureBossCrystals" })
                Assert.IsTrue(ProjectileSpawner.Instance.Has(name), name + " 공격체가 없다.");

            // 다른 지역 이름이 섞여 들어오지 않았는지도 본다 — 아트 폴더 분리가 깨지면 여기서 잡힌다.
            Assert.IsFalse(ProjectileSpawner.Instance.Has("ProjSplinter"),
                "잔재 공격체가 균열에 등록됐다 — 아트 폴더 분리가 깨졌다.");
            Assert.IsFalse(ProjectileSpawner.Instance.Has("GazeProjSound"),
                "응시 공격체가 균열에 등록됐다 — 아트 폴더 분리가 깨졌다.");
        }

        [UnityTest]
        public IEnumerator 숏컷에_균열_봉쇄_애니메이션이_붙어_있다()
        {
            yield return LoadFracture();

            int found = 0;
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
            {
                var sealTransform = shortcut.transform.Find("SealAnimation");
                Assert.IsNotNull(sealTransform, shortcut.Id + "에 봉쇄 애니메이션이 없다.");

                var animator = sealTransform.GetComponent<SpriteAnimator>();
                Assert.IsNotNull(animator, shortcut.Id + " 봉쇄 오브젝트에 애니메이터가 없다.");
                Assert.IsTrue(animator.Has("FractureSealClose"), shortcut.Id + ": FractureSealClose가 없다.");
                Assert.IsTrue(animator.Has("FractureSealOpen"), shortcut.Id + ": FractureSealOpen이 없다.");
                found++;
            }

            // 숏컷 3개 + FS3 입구를 막는 미래 문 1개.
            Assert.AreEqual(4, found, "균열의 Shortcut은 4개다.");
        }

        // 아트를 입히면서 루트 localScale을 만지면 콜라이더가 같이 줄어든다 — 실제로 안전
        // 발판 15개가 0.56x0.05 슬리버가 됐었다. 발판 판정이 실제 월드 크기를 유지하는지 본다.
        [UnityTest]
        public IEnumerator 발판_콜라이더가_스케일로_줄어있지_않다()
        {
            yield return LoadFracture();

            int checked_ = 0;
            var broken = new List<string>();

            foreach (var col in Object.FindObjectsByType<BoxCollider2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string name = col.gameObject.name;
                bool isPlatform = name == "SafePlatform" || name == "CrumblingPlatform"
                    || col.GetComponent<OrbitPlatform>() != null
                    || col.GetComponent<LiftPlatform>() != null;
                if (!isPlatform) continue;

                checked_++;
                var world = Vector2.Scale(col.size, col.transform.lossyScale);
                if (world.x < 2.5f || world.y < 0.3f)
                    broken.Add($"{name} @ {col.transform.position:F0} → {world.x:F2}x{world.y:F2}");
            }

            Debug.Log("===== 발판 콜라이더 ===== " + checked_ + "개 검사");
            Assert.Greater(checked_, 10, "검사할 발판이 너무 적다.");
            Assert.IsEmpty(broken, "콜라이더가 줄어든 발판: " + string.Join(", ", broken));
        }

        // 방별 4K 원본이 지형까지 담당하므로 길게 늘인 장식형 FloorArt 전경은 없어야 한다.
        // 충돌은 비가시 Tilemap이 그대로 담당한다.
        [UnityTest]
        public IEnumerator 장식형_바닥_전경이_제거되어_있다()
        {
            yield return LoadFracture();

            var floors = new List<string>();
            foreach (var transform in Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.name == "FloorArt")
                    floors.Add(transform.name + " @ " + transform.position);
            }

            Assert.IsEmpty(
                floors,
                "4K 배경 위를 가리는 FloorArt 전경이 남아 있다: " +
                string.Join(", ", floors));
        }

        // v1 지형은 24칸이 전부 같은 납작한 사각형이었고, 런타임은 그중 한 칸만 뽑아 모든
        // 바닥에 늘여 썼다 — 공간이 통짜 박스로 보이던 이유다. 끝단과 중간이 실제로 다른
        // 그림인지 본다. 한 종류로 되돌아가면 이 검사가 먼저 깨진다.
        [UnityTest]
        public IEnumerator 바닥이_한_종류의_타일로만_그려지지_않는다()
        {
            yield return LoadFracture();

            var used = new HashSet<string>();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.name != "TraversalSurface" && renderer.name != "PlatformSurface")
                    continue;
                if (renderer.sprite != null) used.Add(renderer.sprite.name);
            }

            Assert.Greater(used.Count, 2,
                "지형이 사실상 한 그림으로 그려지고 있다. 쓰인 타일: " +
                string.Join(", ", used));
        }

        // 형광 테두리 밴드는 "안 보이는데 부딪히는 벽"을 막으려던 응급처치였다. 지형 그림이
        // 네 면을 마감하게 되면서 걷어냈다 — 되살아나면 화면이 다시 디버그 뷰처럼 보인다.
        [UnityTest]
        public IEnumerator 형광_테두리_밴드가_남아있지_않다()
        {
            yield return LoadFracture();

            var bands = new List<string>();
            foreach (var transform in Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.name.StartsWith("PlatformEdge")
                    || transform.name == "TraversalWallEdge"
                    || transform.name == "TraversalEdge"
                    || transform.name.StartsWith("WallClimbEdge"))
                    bands.Add(transform.name);
            }

            Assert.IsEmpty(bands,
                "균열에 형광 테두리 밴드가 남아 있다: " + string.Join(", ", bands));
        }

        // 방 문은 트리거 콜라이더뿐이라 화면에서는 아무것도 아니었다. 처음 오는 사람이
        // 방의 어디가 출구인지 알 수 없던 가장 큰 이유다.
        [UnityTest]
        public IEnumerator 모든_방문에_눈에_보이는_표시가_있다()
        {
            yield return LoadFracture();

            var blind = new List<string>();
            foreach (var door in Object.FindObjectsByType<RoomDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool visible = false;
                foreach (var renderer in door.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.sprite != null) { visible = true; break; }
                if (!visible) blind.Add(door.name);
            }

            Assert.IsEmpty(blind,
                "겉모습이 없는 방 문이 있다: " + string.Join(", ", blind));
        }
    }
}
