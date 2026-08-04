using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Player;
using HiddenWeight.UI;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 배치물이 지형 안에 파묻히거나 허공에 뜬 채로 놓이지 않았는지, 그리고 되감기가 새 지역에서
    // 실제로 대상을 되돌리는지 확인한다.
    public class ResiduePlacementTests
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

        [UnityTest]
        public IEnumerator R02_하부_통로의_천장_충돌면이_보인다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            foreach (string bridgeName in new[] { "R02_Bridge_A", "R02_Bridge_B" })
            {
                var bridge = GameObject.Find(bridgeName);
                Assert.IsNotNull(bridge, $"{bridgeName}를 찾지 못했다.");

                var collider = bridge.GetComponent<BoxCollider2D>();
                var art = bridge.transform.Find("ResiduePlatformV3")?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(collider, $"{bridgeName} 충돌면이 없다.");
                Assert.IsNotNull(art, $"{bridgeName} 하부 아트가 없다.");
                Assert.IsTrue(art.enabled && art.sprite != null, $"{bridgeName} 하부 아트가 표시되지 않는다.");
                Assert.LessOrEqual(Mathf.Abs(art.bounds.max.y - collider.bounds.max.y), 0.05f,
                    $"{bridgeName} 그림 윗면이 충돌면과 어긋났다.");
                Assert.LessOrEqual(art.bounds.min.y, collider.bounds.min.y + 0.05f,
                    $"{bridgeName} 그림이 밑면까지 덮지 못한다.");

                foreach (var renderer in bridge.GetComponentsInChildren<SpriteRenderer>(true))
                    Assert.That(renderer.name, Does.Not.StartWith("PlatformEdge"),
                        $"{bridgeName}에 두 몬스터 사이를 가르는 굵은 외곽선이 남아 있다.");
            }
        }

        [UnityTest]
        public IEnumerator R01_낮은_계단에_통과불가_기둥처럼_솟은_그림이_없다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var room = GameObject.Find("Room01").GetComponent<Room>();
            int flatFaces = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(renderer.bounds.center)) continue;
                Assert.AreNotEqual("ResidueWallModule", renderer.name,
                    "R01의 낮은 계단에 등반 기둥 이미지가 솟아 있다.");
                if (renderer.name != "ResidueFlatWallFace") continue;
                Assert.LessOrEqual(renderer.bounds.size.y, 1.2f,
                    "R01 계단 측면 이미지가 실제 높이보다 크게 솟았다.");
                flatFaces++;
            }
            Assert.Greater(flatFaces, 0, "R01 계단의 평평한 측면 이미지가 없다.");
        }

        [UnityTest]
        public IEnumerator R07_중간계단은_충돌보다_높은_가짜벽을_보이지_않는다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            GameObject fakeStair = null;
            foreach (var transform in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name == "R07_StairVisual") { fakeStair = transform.gameObject; break; }
            if (fakeStair != null)
                Assert.IsFalse(fakeStair.activeSelf,
                    "R07 계단 옆에 통과 가능한 높은 아치 벽 이미지가 남아 있다.");

            var crashWall = GameObject.Find("R07_CrashWall");
            Assert.IsTrue(crashWall == null || !crashWall.activeSelf,
                "R07 중간 계단과 겹치는 보이지 않는 돌진 충돌벽이 남아 있다.");

            var room = GameObject.Find("Room07").GetComponent<Room>();
            Vector2 expected = (Vector2)room.WorldBounds.min + new Vector2(21.5f, 6.5f);
            BoxCollider2D step = null;
            float best = float.MaxValue;
            foreach (var candidate in Object.FindObjectsByType<BoxCollider2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name != "SafePlatform" || !room.WorldBounds.Contains(candidate.bounds.center))
                    continue;
                float distance = Vector2.Distance(candidate.bounds.center, expected);
                if (distance >= best) continue;
                best = distance;
                step = candidate;
            }
            Assert.IsNotNull(step, "R07 출구로 오르는 실제 중간 계단이 없다.");

            foreach (var renderer in step.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled)
                    Assert.LessOrEqual(renderer.bounds.max.y, step.bounds.max.y + 0.05f,
                        "R07 중간 계단 그림이 실제로 밟는 높이보다 위로 솟아 있다.");
        }

        [UnityTest]
        public IEnumerator R01_발밑_바닥에_구형_바닥그림이_중복되어_솟지_않는다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var room = GameObject.Find("Room01").GetComponent<Room>();
            int legacyCount = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.name != "FloorArt" || !room.WorldBounds.Contains(renderer.bounds.center))
                    continue;
                legacyCount++;
                Assert.IsFalse(renderer.enabled,
                    "R01에 구형 FloorArt와 새 바닥 모듈이 겹쳐 가짜 단차가 보인다.");
            }
            Assert.Greater(legacyCount, 0, "중복 방지 검증용 R01 구형 FloorArt를 찾지 못했다.");
        }

        [UnityTest]
        public IEnumerator R01_입구_바닥은_실제_계단이_없는_한_높이의_평지다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var room = GameObject.Find("Room01").GetComponent<Room>();
            var tilemap = Object.FindFirstObjectByType<Tilemap>();
            int expectedTop = int.MinValue;
            for (float worldX = room.WorldBounds.min.x + 0.5f;
                 worldX < room.WorldBounds.max.x; worldX += 1f)
            {
                int cellX = tilemap.WorldToCell(new Vector3(worldX, room.WorldBounds.center.y, 0f)).x;
                int top = int.MinValue;
                for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
                    if (tilemap.HasTile(new Vector3Int(cellX, y, 0))) top = y;

                if (expectedTop == int.MinValue) expectedTop = top;
                Assert.AreEqual(expectedTop, top,
                    $"R01 x={worldX:F1}에 실제 계단이 아닌 높은 지형 셀이 남아 있다.");
            }

            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(renderer.bounds.center)
                    || renderer.name != "ResidueGroundModule")
                    continue;
                Assert.AreNotSame(palette.residueGroundLeft, renderer.sprite,
                    "R01 통과 가능한 바닥에 왼쪽 석주 캡이 남아 있다.");
                Assert.AreNotSame(palette.residueGroundRight, renderer.sprite,
                    "R01 통과 가능한 바닥에 오른쪽 석주 캡이 남아 있다.");
            }
        }

        [UnityTest]
        public IEnumerator R04_굴뚝은_양쪽_아래로_통과할_수_있다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var left = GameObject.Find("R04_Chimney_L").GetComponent<BoxCollider2D>();
            var rightObject = GameObject.Find("R04_Chimney_R");
            float centerX = left.bounds.max.x + 1.5f;
            var ground = Physics2D.Raycast(
                new Vector2(centerX, left.bounds.min.y - 0.05f), Vector2.down, 5f,
                LayerMask.GetMask("Ground"));

            Assert.IsNotNull(ground.collider, "R04 굴뚝 아래의 안전 바닥을 찾지 못했다.");
            Assert.GreaterOrEqual(left.bounds.min.y - ground.point.y, 2.8f,
                "R04 왼쪽 벽 아래에 굴뚝 진입 공간이 없다.");
            if (rightObject != null)
            {
                Assert.IsFalse(rightObject.GetComponent<BoxCollider2D>().enabled,
                    "R04 마지막 오른쪽 벽의 충돌이 남아 통과 경로를 막는다.");
                foreach (var renderer in rightObject.GetComponentsInChildren<SpriteRenderer>(true))
                    Assert.IsFalse(renderer.enabled,
                        "R04 마지막 오른쪽 벽에 통과 가능한 가짜 벽 이미지가 남아 있다.");
            }

            foreach (var wall in new[] { left })
            foreach (var renderer in wall.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name != "ResidueWallModule" || !renderer.enabled) continue;
                Assert.GreaterOrEqual(renderer.bounds.min.y, wall.bounds.min.y - 0.05f,
                    $"{wall.name} 그림이 실제 벽 아래로 돌출됐다.");
                Assert.LessOrEqual(renderer.bounds.max.y, wall.bounds.max.y + 0.05f,
                    $"{wall.name} 그림이 실제 벽 위로 돌출됐다.");
            }

        }

        [UnityTest]
        public IEnumerator R05_첫_복원_디딤판은_그림과_충돌이_맞고_오른쪽_턱까지_이어진다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();

            var room = GameObject.Find("Room05").GetComponent<Room>();
            var primary = GameObject.Find("R05_PrimaryRestore").GetComponent<Rewindable>();
            primary.Rewind();
            yield return null;

            var collider = primary.GetComponent<BoxCollider2D>();
            var art = primary.transform.Find("RestoredPlatformVisual")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(art, "R05 첫 복원물의 완성된 디딤판 그림이 없다.");
            Assert.GreaterOrEqual(collider.bounds.size.x, 2.95f,
                "R05 복원 디딤판이 너무 좁아 턱 사이로 떨어질 수 있다.");
            float visibleSurfaceY = art.bounds.max.y
                - art.bounds.size.y * primary.RestoredSurfaceInsetNormalized;
            Assert.That(visibleSurfaceY, Is.EqualTo(collider.bounds.max.y).Within(0.05f),
                "R05 복원 디딤판의 돌 보행면과 실제 발판 충돌면이 어긋났다.");
            Assert.Greater(art.bounds.max.y, collider.bounds.max.y + 0.25f,
                "난간 장식을 보행면으로 오인해 발판 그림을 아래로 내렸다.");
            Assert.GreaterOrEqual(collider.bounds.max.x,
                room.WorldBounds.min.x + 12f - 0.05f,
                "R05 복원 디딤판이 오른쪽 높은 턱까지 이어지지 않는다.");
        }

        [UnityTest]
        public IEnumerator 배치물이_지형_안에_파묻혀_있지_않다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            int groundMask = LayerMask.GetMask("Ground", "Wall");
            var buried = new StringBuilder();
            int checkedCount = 0;

            // 플레이어가 상호작용해야 하는 것들. 지형에 박혀 있으면 닿을 수도, 볼 수도 없다.
            foreach (var mono in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool interactive = mono is Rewindable || mono is CurrencyPickup || mono is HealingPickup
                                || mono is StoryFragment || mono is Checkpoint || mono is RewardChest;
                if (!interactive) continue;

                checkedCount++;
                // 자기 자신(되감기 블록은 Ground 레이어다)과 자식 콜라이더는 빼고 본다.
                var hits = Physics2D.OverlapPointAll(mono.transform.position, groundMask);
                Collider2D hit = null;
                foreach (var candidate in hits)
                    if (!candidate.transform.IsChildOf(mono.transform)) { hit = candidate; break; }

                if (hit != null)
                    buried.AppendLine("  " + mono.GetType().Name + " " + mono.name
                        + " @ " + mono.transform.position.ToString("F1")
                        + " ← " + hit.name + "(" + LayerMask.LayerToName(hit.gameObject.layer) + ")"
                        + " bounds=" + hit.bounds.ToString("F1"));
            }

            Debug.Log("===== 배치 검사 ===== 검사 " + checkedCount + "개 / 파묻힘 "
                + (buried.Length == 0 ? "0" : "발견") + "\n" + buried);

            Assert.IsTrue(buried.Length == 0,
                "지형 안에 파묻힌 배치물이 있다 — 플레이어가 닿을 수 없다.\n" + buried);
        }

        [UnityTest]
        public IEnumerator 벽타기_굴뚝의_충돌면이_눈에_보인다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            foreach (string wallName in new[]
                     {
                         "R04_Chimney_L",
                         "R08_Chimney_L", "R08_Chimney_R",
                         "R12_Wall_L", "R12_Wall_R",
                     })
            {
                var wall = GameObject.Find(wallName);
                Assert.IsNotNull(wall, wallName + " 벽 충돌체가 없다.");
                var visual = wall.transform.Find("WallClimbSurfaces_Runtime");
                Assert.IsNotNull(visual, wallName + "에 보이는 벽타기 표면이 없다.");
                var modules = visual.GetComponentsInChildren<SpriteRenderer>();
                Assert.IsNotEmpty(modules, wallName + "의 V3 세로 벽 모듈이 없다.");
                foreach (var module in modules)
                {
                    Assert.IsTrue(module.enabled && module.sprite != null,
                        wallName + "에 비활성 벽 모듈이 있다.");
                    Assert.That(module.sprite.name, Does.StartWith("ResidueClimbPillar"),
                        wallName + "이 생성된 벽타기 전용 이미지를 쓰지 않는다.");
                    float scaleRatio = module.transform.lossyScale.x
                        / module.transform.lossyScale.y;
                    Assert.That(scaleRatio, Is.EqualTo(1f).Within(0.02f),
                        wallName + "의 벽 이미지가 세로로 늘어났다.");
                }
            }

            var left = GameObject.Find("R08_Chimney_L").GetComponent<BoxCollider2D>();
            var right = GameObject.Find("R08_Chimney_R").GetComponent<BoxCollider2D>();
            var room08 = GameObject.Find("Room08").GetComponent<Room>();
            Assert.That(left.bounds.size.y, Is.EqualTo(9f).Within(0.05f),
                "R08 왼쪽 벽이 이미지 높이와 다르다.");
            Assert.That(right.bounds.size.y, Is.EqualTo(9f).Within(0.05f),
                "R08 오른쪽 벽이 이미지 높이와 다르다.");
            Assert.That(left.bounds.max.y,
                Is.EqualTo(room08.WorldBounds.min.y + 13f).Within(0.05f),
                "R08 왼쪽 벽 꼭대기가 이미지 상단과 다르다.");
            Assert.That(right.bounds.max.y,
                Is.EqualTo(room08.WorldBounds.min.y + 13f).Within(0.05f),
                "R08 오른쪽 벽 꼭대기가 이미지 상단과 다르다.");
            var landingHit = Physics2D.Raycast(
                new Vector2(room08.WorldBounds.min.x + 9f, room08.WorldBounds.min.y + 15f),
                Vector2.down, 5f, LayerMask.GetMask("Ground"));
            Assert.IsNotNull(landingHit.collider, "R08 굴뚝 오른쪽 착지대 충돌면이 없다.");
            Assert.That(landingHit.point.y, Is.EqualTo(right.bounds.max.y).Within(0.05f),
                "R08 오른쪽 벽 꼭대기와 우측 착지대 보행면 높이가 달라 캐릭터가 걸린다.");

            var movers = new System.Collections.Generic.List<MovingPlatform>();
            foreach (var mover in Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
                if (room08.WorldBounds.Contains(mover.transform.position)) movers.Add(mover);
            movers.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            Assert.AreEqual(2, movers.Count, "R08 이동 발판 수가 달라졌다.");
            var lowerLift = movers[0].GetComponent<Collider2D>();
            var upperLift = movers[1].GetComponent<Collider2D>();
            Assert.That(lowerLift.bounds.max.y - landingHit.point.y, Is.LessThanOrEqualTo(0.4f),
                "R08 착지대에서 첫 이동 발판으로 올라갈 수 없다.");
            Assert.That(upperLift.bounds.max.y - lowerLift.bounds.max.y,
                Is.LessThanOrEqualTo(2.6f),
                "R08 두 이동 발판 사이 높이가 최대 점프보다 높다.");

            var upperStep = GameObject.Find("R08_UpperStep")
                ?? GameObject.Find("R08_UpperStep_Runtime");
            Assert.IsNotNull(upperStep, "R08 두 번째 이동 발판 위의 중간 안전 발판이 없다.");
            var upperStepCollider = upperStep.GetComponent<Collider2D>();
            Assert.IsNotNull(upperStepCollider, "R08 중간 안전 발판에 충돌면이 없다.");
            Assert.That(upperStepCollider.bounds.max.y - upperLift.bounds.max.y,
                Is.LessThanOrEqualTo(2.6f),
                "R08 두 번째 이동 발판에서 중간 안전 발판으로 올라갈 수 없다.");

            var upperFloorHit = Physics2D.Raycast(
                new Vector2(room08.WorldBounds.min.x + 17f, room08.WorldBounds.min.y + 22f),
                Vector2.down, 3f, LayerMask.GetMask("Ground"));
            Assert.IsNotNull(upperFloorHit.collider, "R08 중단 고정 바닥 충돌면이 없다.");
            Assert.That(upperFloorHit.point.y - upperStepCollider.bounds.max.y,
                Is.LessThanOrEqualTo(2.8f),
                "R08 중간 안전 발판에서 중단 고정 바닥으로 올라갈 수 없다.");

            foreach (var wall in new[] { left, right })
            {
                var visual = wall.transform.Find("WallClimbSurfaces_Runtime");
                var renderers = visual.GetComponentsInChildren<SpriteRenderer>();
                Bounds artBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    artBounds.Encapsulate(renderers[i].bounds);
                Assert.That(artBounds.min.y, Is.EqualTo(wall.bounds.min.y).Within(0.05f),
                    wall.name + " 벽 이미지 아래가 실제 충돌면과 다르다.");
                Assert.That(artBounds.max.y, Is.EqualTo(wall.bounds.max.y).Within(0.05f),
                    wall.name + " 벽 이미지 위 1/3에 충돌이 없다.");
            }

            float innerWidth = right.bounds.min.x - left.bounds.max.x;
            Assert.That(innerWidth, Is.EqualTo(3f).Within(0.05f),
                "R08 굴뚝의 유효 폭이 튜토리얼과 다르다.");

            Vector2 blockedCenter = new Vector2(
                (left.bounds.max.x + right.bounds.min.x) * 0.5f,
                left.bounds.max.y - 1.5f);
            var blocker = Physics2D.OverlapBox(blockedCenter,
                new Vector2(innerWidth - 0.2f, 0.6f), 0f, LayerMask.GetMask("Ground"));
            Assert.IsNull(blocker,
                "R08 굴뚝 내부를 안전 발판이 천장처럼 가로막고 있다: " + blocker?.name);

            // 한 지점만 확인하면 굴뚝 중간에 남은 옛 타일을 놓칠 수 있다. 바닥 바로 위부터
            // 벽 꼭대기까지 플레이어가 통과할 세로 공간 전체가 비어 있어야 한다.
            Vector2 shaftCenter = new Vector2(
                (left.bounds.max.x + right.bounds.min.x) * 0.5f,
                left.bounds.center.y);
            var shaftBlockers = Physics2D.OverlapBoxAll(shaftCenter,
                new Vector2(innerWidth - 0.2f, left.bounds.size.y - 0.2f), 0f,
                LayerMask.GetMask("Ground"));
            Assert.IsEmpty(shaftBlockers,
                "R08 굴뚝 세로 통로에 남은 충돌체가 있다: "
                + string.Join(", ", System.Array.ConvertAll(shaftBlockers, hit => hit.name)));

            var exitFloor = GameObject.Find("R07_ExitFloorVisual");
            Assert.IsNotNull(exitFloor, "R07 오른쪽 높은 출구 바닥에 명시적인 시각물이 없다.");
            var exitRenderer = exitFloor.GetComponent<SpriteRenderer>();
            Assert.IsTrue(exitRenderer.enabled && exitRenderer.sprite != null,
                "R07 출구 바닥 충돌은 있지만 바닥 그림이 보이지 않는다.");
            Assert.GreaterOrEqual(exitRenderer.bounds.size.x, 6.8f,
                "R07 출구 바닥 그림이 실제 7유닛 바닥보다 짧다.");

            var fakeStairWall = GameObject.Find("R07_StairVisual");
            if (fakeStairWall != null)
            foreach (var renderer in fakeStairWall.GetComponentsInChildren<SpriteRenderer>(true))
                Assert.IsFalse(renderer.enabled,
                    "R07에 충돌 없이 통과되는 아치형 계단 벽 이미지가 남아 있다.");
        }

        [UnityTest]
        public IEnumerator R08_굴뚝을_실제_벽점프로_꼭대기까지_오른다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var left = GameObject.Find("R08_Chimney_L").GetComponent<BoxCollider2D>();
            var right = GameObject.Find("R08_Chimney_R").GetComponent<BoxCollider2D>();
            var player = PlayerController.Instance;
            var body = player.GetComponent<Rigidbody2D>();
            float centerX = (left.bounds.max.x + right.bounds.min.x) * 0.5f;
            player.TeleportTo(new Vector3(centerX, left.bounds.min.y - 1f, 0f));
            for (int i = 0; i < 45; i++)
            {
                PlayerInput.Injected = default;
                yield return new WaitForFixedUpdate();
            }

            float bestY = player.transform.position.y;
            float direction = 1f;
            Collider2D lastCeiling = null;
            for (int i = 0; i < 1200 && bestY < left.bounds.max.y + 0.6f; i++)
            {
                var frame = new PlayerInput.Frame { jumpHeld = true };
                if (player.IsGrounded)
                {
                    frame.horizontal = direction;
                    frame.jumpPressed = true;
                }
                else if (player.IsOnWall)
                {
                    frame.horizontal = player.Facing;
                    frame.jumpPressed = true;
                    direction = -player.Facing;
                }
                else
                {
                    frame.horizontal = Mathf.Abs(body.linearVelocity.x) > 0.1f
                        ? Mathf.Sign(body.linearVelocity.x) : direction;
                }

                PlayerInput.Injected = frame;
                yield return new WaitForFixedUpdate();
                bestY = Mathf.Max(bestY, player.transform.position.y);
                var ceiling = Physics2D.Raycast(player.transform.position, Vector2.up, 1.2f,
                    LayerMask.GetMask("Ground", "Wall"));
                if (ceiling.collider != null) lastCeiling = ceiling.collider;
            }

            Assert.Greater(bestY, left.bounds.max.y + 0.5f,
                "R08 굴뚝을 실제 벽점프로 못 올랐다. 최고 y=" + bestY.ToString("F2")
                + ", 벽 top=" + left.bounds.max.y.ToString("F2")
                + ", 머리 위=" + (lastCeiling == null ? "없음" : lastCeiling.name));

            // 오른쪽 벽 정상과 착지대가 같은 높이여도 실제 캐릭터가 걸어서 빠져나가지
            // 못하면 QA에서 본 "상단에 붙은 채 멈춤"이 재발한 것이다.
            var playerCollider = player.GetComponent<Collider2D>();
            player.TeleportTo(new Vector3(
                right.bounds.center.x,
                right.bounds.max.y + playerCollider.bounds.extents.y + 0.04f,
                0f));
            body.linearVelocity = Vector2.zero;
            PlayerInput.Injected = default;
            for (int i = 0; i < 20; i++)
                yield return new WaitForFixedUpdate();

            for (int i = 0; i < 30; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { horizontal = 1f };
                yield return new WaitForFixedUpdate();
            }
            PlayerInput.Injected = default;
            Assert.Greater(player.transform.position.x, right.bounds.max.x + 1f,
                "R08 오른쪽 벽 정상에서 우측 착지대로 이동하지 못하고 멈췄다.");
            Assert.IsTrue(player.IsGrounded,
                "R08 오른쪽 벽 정상에서 빠져나온 뒤 착지대에 서지 못했다.");
        }

        [UnityTest]
        public IEnumerator K홀드_구간의_타일맵_세로턱이_눈에_보인다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var skillFragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                fragment => fragment.FragmentId == "residue_skill");
            Assert.IsNotNull(skillFragment, "K 홀드 구간의 되감기 파편이 없다.");

            bool foundTallWall = false;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name != "ResidueWallModule") continue;
                if (Vector2.Distance(renderer.bounds.center, skillFragment.transform.position) > 8f) continue;
                if (renderer.bounds.size.y >= 2.5f) foundTallWall = true;
            }

            Assert.IsTrue(foundTallWall,
                "K 홀드 안내 옆의 3유닛 턱에 보이는 세로 벽면이 만들어지지 않았다.");

            TutorialHint rewindHint = null;
            foreach (var hint in Object.FindObjectsByType<TutorialHint>(FindObjectsSortMode.None))
                if (Vector2.Distance(hint.transform.position, skillFragment.transform.position) < 6f)
                    rewindHint = hint;
            Assert.IsNotNull(rewindHint, "최초 되감기 대상 옆에 사용 설명이 없다.");
            Assert.That(rewindHint.GetComponentInChildren<TextMesh>().text,
                Does.Contain("구조물이 복원됩니다"),
                "K를 누르면 무엇이 일어나는지 안내하지 않는다.");

            UnityEngine.UI.Text channelLabel = null;
            foreach (var text in Object.FindObjectsByType<UnityEngine.UI.Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.name == "ChannelLabel") channelLabel = text;
            Assert.IsNotNull(channelLabel, "되감기 진행 표시에 설명 문구가 없다.");
            Assert.AreEqual("되감는 중", channelLabel.text);
            var channel = channelLabel.transform.parent.GetComponent<RectTransform>();
            Assert.Greater(channel.rect.width, channel.rect.height * 2f,
                "되감기 진행 표시가 의미 없는 노란 정사각형으로 보인다.");
        }

        [UnityTest]
        public IEnumerator 새_지역에서_되감기가_대상을_되돌린다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            var progress = GameManager.Instance.Progress;

            // R05의 되감기 파편을 밟아 스킬을 연다.
            var fragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                f => f.FragmentId == "residue_skill");
            Assert.IsNotNull(fragment, "R05에 되감기 파편이 없다.");

            player.TeleportTo(fragment.transform.position + new Vector3(3f, 1f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragment.transform.position);
            for (int i = 0; i < 30; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(progress.HasSkill(EmotionId.Rewind), "되감기가 해금되지 않았다.");

            var controller = EmotionSkillController.Instance;
            yield return null;
            var skill = controller.Active;
            Assert.IsNotNull(skill, "되감기가 활성 스킬로 잡히지 않았다.");

            // 블록이 중력으로 밀려나 되돌릴 거리가 생길 때까지 기다린다.
            for (int i = 0; i < 150; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Rewindable target = null;
            float best = float.MaxValue;
            foreach (var rewindable in Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
            {
                if (!rewindable.CanRewind) continue;
                float d = Vector2.Distance(fragment.transform.position, rewindable.transform.position);
                if (d < best) { best = d; target = rewindable; }
            }
            Assert.IsNotNull(target, "밀려난 되감기 대상이 하나도 없다 — 되감을 것이 없다.");

            Transform outline = null;
            Transform marker = null;
            foreach (var child in target.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RewindOutline") outline = child;
                if (child.name == "RewindTargetMarker") marker = child;
            }
            Assert.IsNotNull(outline, "K 홀드 대상에 실제 외형을 따르는 골드 강조가 없다.");
            Assert.IsNotNull(marker, "K 홀드 대상 위에 위치 표식이 없다.");
            Assert.IsTrue(outline.GetComponent<SpriteRenderer>().enabled,
                "되감기 가능한데 골드 강조가 보이지 않는다.");
            Assert.IsTrue(marker.GetComponent<MeshRenderer>().enabled,
                "되감기 가능한데 대상 위치 표식이 보이지 않는다.");

            // 대상 옆에 서서 채널링한다.
            var displaced = target.transform.position;
            player.TeleportTo(displaced + new Vector3(-1.5f, 0.5f, 0f));
            for (int i = 0; i < 10; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            controller.enabled = false; // 실제 K 홀드를 흉내낼 수 없으므로 스킬을 직접 돌린다
            skill.Begin();
            Assert.IsTrue(skill.IsActive, "되감기가 시작되지 않았다(대상 없음으로 즉시 취소).");

            for (float t = 0f; t < skill.Data.channelTime + 0.5f && skill.IsActive; t += Time.fixedDeltaTime)
            {
                skill.Tick(Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Debug.Log("===== 새 지역 되감기 ===== 밀려난 위치 " + displaced.ToString("F2")
                + " → 되감기 후 " + target.transform.position.ToString("F2")
                + " CanRewind=" + target.CanRewind);

            Assert.IsFalse(target.CanRewind, "채널링을 마쳤는데 대상이 원위치로 돌아오지 않았다.");
        }
    }
}
