using System.Collections;
using System.Collections.Generic;
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
    // 응시(Zone_Gaze_Full)와 균열(Zone_Fracture_Full) 전체 지역을
    // docs/GAZE_LEVEL_DESIGN.md, docs/FRACTURE_LEVEL_DESIGN.md에 대고 검증한다.
    //
    // 검사 대상은 "플레이해 보지 않으면 모르는 것"이 아니라 "명세가 숫자로 못박은 것"이다:
    // 방 개수와 크기, 체크포인트·숏컷 수, 방 사이가 실제 지형으로 이어졌는지, 숨죽이기
    // 게이트의 규격, 그리고 균열의 두 가지 금지 규칙(자각을 열쇠로 쓰지 않는다 / 되살아나지
    // 않는 붕괴 발판을 두지 않는다).
    public class GazeFractureZoneTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static void ResetProgress()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
        }

        static Room FindRoom(string name)
        {
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (room.name == name) return room;
            return null;
        }

        // 명세 4장의 방 목록. (이름, 가로, 세로)
        static readonly (string name, float w, float h)[] GazeRooms =
        {
            ("GazeRoom01", 26, 14), ("GazeRoom02", 28, 16), ("GazeRoom03", 30, 18),
            ("GazeRoom04", 24, 22), ("GazeSecret01", 18, 14), ("GazeRoom05", 26, 14),
            ("GazeRoom06", 32, 16), ("GazeSecret02", 24, 16), ("GazeRoom07", 34, 18),
            ("GazeRoom08", 24, 30), ("GazeRoom09", 32, 16), ("GazeRoom10", 24, 18),
            ("GazeRoom11", 28, 16), ("GazeSecret03", 20, 14), ("GazeRoom12", 30, 18),
        };

        static readonly (string name, float w, float h)[] FractureRooms =
        {
            ("FractureRoom01", 26, 14), ("FractureRoom02", 28, 16), ("FractureRoom03", 30, 18),
            ("FractureRoom04", 24, 22), ("FractureSecret01", 18, 14), ("FractureRoom05", 26, 14),
            ("FractureRoom06", 32, 16), ("FractureSecret02", 24, 16), ("FractureRoom07", 34, 18),
            ("FractureRoom08", 24, 30), ("FractureRoom09", 32, 16), ("FractureRoom10", 24, 18),
            ("FractureRoom11", 28, 16), ("FractureSecret03", 20, 14), ("FractureRoom12", 30, 18),
        };

        // 주 동선 12룸을 순서대로. 비밀방은 통로가 아니라 수직 통로로 잇기 때문에 뺀다.
        static readonly string[] GazeMainPath =
        {
            "GazeRoom01", "GazeRoom02", "GazeRoom03", "GazeRoom04", "GazeRoom05", "GazeRoom06",
            "GazeRoom07", "GazeRoom08", "GazeRoom09", "GazeRoom10", "GazeRoom11", "GazeRoom12",
        };

        static readonly string[] FractureMainPath =
        {
            "FractureRoom01", "FractureRoom02", "FractureRoom03", "FractureRoom04", "FractureRoom05",
            "FractureRoom06", "FractureRoom07", "FractureRoom08", "FractureRoom09", "FractureRoom10",
            "FractureRoom11", "FractureRoom12",
        };

        static IEnumerator LoadZone(string sceneName)
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        static void AssertRooms((string name, float w, float h)[] expected)
        {
            var report = new StringBuilder();
            foreach (var (name, w, h) in expected)
            {
                var room = FindRoom(name);
                Assert.IsNotNull(room, name + " 방이 씬에 없다.");

                var size = room.WorldBounds.size;
                report.AppendLine(name + " " + size.x + "x" + size.y);
                Assert.AreEqual(w, size.x, 0.01f, name + " 가로 크기가 명세와 다르다.");
                Assert.AreEqual(h, size.y, 0.01f, name + " 세로 크기가 명세와 다르다.");
            }
            Debug.Log("===== 방 목록 =====\n" + report);
        }

        // 두 방 사이의 빈 구간을 실제로 걸어서 지날 수 있는지 본다. 오프셋 계산이 어긋나면
        // (출구 높이 != 입구 높이) 여기가 허공으로 남아 지역이 끊긴다.
        //
        // 통로는 바닥과 천장이 한 쌍이라, 위에서 그냥 쏘면 천장에 먼저 맞는다. 그래서 첫
        // 히트(천장 윗면) 아래에서 다시 쏘아 진짜 밟을 바닥을 찾고, 둘 사이 높이가 플레이어
        // 키(1.4)보다 넉넉한지까지 확인한다 — "지형은 있는데 못 지나가는" 통로를 잡기 위함이다.
        const float CorridorCeilingThickness = 1f;

        static void AssertConnected(string[] mainPath)
        {
            int groundMask = LayerMask.GetMask("Ground");
            var report = new StringBuilder();

            for (int i = 0; i + 1 < mainPath.Length; i++)
            {
                var a = FindRoom(mainPath[i]);
                var b = FindRoom(mainPath[i + 1]);
                Assert.IsNotNull(a, mainPath[i] + " 방이 없다.");
                Assert.IsNotNull(b, mainPath[i + 1] + " 방이 없다.");

                string link = mainPath[i] + " → " + mainPath[i + 1];
                float gapX = (a.WorldBounds.max.x + b.WorldBounds.min.x) * 0.5f;
                float top = Mathf.Max(a.WorldBounds.max.y, b.WorldBounds.max.y) + 8f;
                float bottom = Mathf.Min(a.WorldBounds.min.y, b.WorldBounds.min.y) - 8f;

                var ceiling = Physics2D.Raycast(new Vector2(gapX, top), Vector2.down, top - bottom, groundMask);
                Assert.IsNotNull(ceiling.collider, link + " 연결 통로에 지형이 전혀 없다(x=" + gapX + ").");

                float ceilingBottom = ceiling.point.y - CorridorCeilingThickness;
                var floor = Physics2D.Raycast(new Vector2(gapX, ceilingBottom - 0.05f), Vector2.down,
                    ceilingBottom - bottom, groundMask);
                Assert.IsNotNull(floor.collider, link + " 연결 통로에 밟을 바닥이 없다(x=" + gapX + ").");

                float clearance = ceilingBottom - floor.point.y;
                report.AppendLine(link + " x=" + gapX + " 바닥y=" + floor.point.y + " 통과높이=" + clearance);

                Assert.Greater(clearance, 1.5f, link + " 통로가 플레이어 키보다 낮다.");

                // 통로 바닥은 두 방의 세로 범위 안에 있어야 한다. 밖에 있으면 통로가 방과
                // 붙지 않고 엉뚱한 높이에 떠 있다는 뜻이다.
                float lo = Mathf.Min(a.WorldBounds.min.y, b.WorldBounds.min.y);
                float hi = Mathf.Max(a.WorldBounds.max.y, b.WorldBounds.max.y);
                Assert.IsTrue(floor.point.y >= lo && floor.point.y <= hi,
                    link + " 통로 바닥(y=" + floor.point.y + ")이 두 방의 높이 범위 밖이다.");
            }
            Debug.Log("===== 방 연결 =====\n" + report);
        }

        // ------------------------------------------------------------
        // 응시
        // ------------------------------------------------------------

        [UnityTest]
        public IEnumerator 응시_열다섯_개_방이_명세_크기로_존재한다()
        {
            yield return LoadZone("Zone_Gaze_Full");
            AssertRooms(GazeRooms);
        }

        [UnityTest]
        public IEnumerator 응시_주_동선_열두_방이_지형으로_이어진다()
        {
            yield return LoadZone("Zone_Gaze_Full");
            AssertConnected(GazeMainPath);
        }

        [UnityTest]
        public IEnumerator 응시_체크포인트_셋과_숏컷_셋이_있다()
        {
            yield return LoadZone("Zone_Gaze_Full");

            var checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            Assert.AreEqual(3, checkpoints.Length, "명세 8.1절: 체크포인트는 3개다.");

            var ids = new HashSet<string>();
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
                ids.Add(shortcut.Id);

            Assert.IsTrue(ids.Contains("gaze_shortcut_a"), "숏컷 A(G05→G03)가 없다.");
            Assert.IsTrue(ids.Contains("gaze_shortcut_b"), "숏컷 B(G08→G03)가 없다.");
            Assert.IsTrue(ids.Contains("gaze_shortcut_c"), "숏컷 C(G10→G07)가 없다.");
        }

        // 숨죽이기 게이트가 "숨죽인 몸만" 통과시키는 규격인지 본다. 평상시 콜라이더는
        // 0.8x1.4, 숨죽이면 0.6배라 0.48x0.84다. 규격을 벗어나면 게이트가 아니라
        // 그냥 막힌 벽이거나 그냥 뚫린 길이 된다.
        [UnityTest]
        public IEnumerator 응시_숨죽이기_게이트가_규격_안에_있다()
        {
            yield return LoadZone("Zone_Gaze_Full");

            int groundMask = LayerMask.GetMask("Ground");
            var lowCeilings = new[] { "G05_LowCeiling", "G06_LowCeiling_A", "G06_LowCeiling_B" };

            foreach (var name in lowCeilings)
            {
                var ceiling = GameObject.Find(name);
                Assert.IsNotNull(ceiling, name + " 낮은 천장이 없다.");

                var box = ceiling.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(box, name + "에 콜라이더가 없다.");

                float ceilingBottom = ceiling.transform.position.y
                    - box.size.y * ceiling.transform.localScale.y * 0.5f;

                // 천장 바로 아래에서 아래로 쏴 바닥 높이를 찾는다.
                var floor = Physics2D.Raycast(
                    new Vector2(ceiling.transform.position.x, ceilingBottom - 0.05f),
                    Vector2.down, 30f, groundMask);
                Assert.IsNotNull(floor.collider, name + " 아래에 바닥이 없다.");

                float clearance = ceilingBottom - floor.point.y;
                Debug.Log(name + " 통과 높이 = " + clearance);
                Assert.Greater(clearance, 0.84f, name + ": 숨죽여도 못 지나간다.");
                Assert.Less(clearance, 1.4f, name + ": 숨죽이지 않아도 지나간다.");
            }

            // GS2 입구의 좁은 세로 틈.
            var left = GameObject.Find("G06_Slot_L");
            var right = GameObject.Find("G06_Slot_R");
            Assert.IsNotNull(left, "GS2 틈의 왼쪽 벽이 없다.");
            Assert.IsNotNull(right, "GS2 틈의 오른쪽 벽이 없다.");

            float leftEdge = left.transform.position.x + left.transform.localScale.x * 0.5f;
            float rightEdge = right.transform.position.x - right.transform.localScale.x * 0.5f;
            float slot = rightEdge - leftEdge;

            Debug.Log("GS2 틈 폭 = " + slot);
            Assert.Greater(slot, 0.48f, "GS2 틈: 숨죽여도 못 들어간다.");
            Assert.Less(slot, 0.8f, "GS2 틈: 숨죽이지 않아도 들어간다.");
        }

        // ------------------------------------------------------------
        // 균열
        // ------------------------------------------------------------

        [UnityTest]
        public IEnumerator 균열_열다섯_개_방이_명세_크기로_존재한다()
        {
            yield return LoadZone("Zone_Fracture_Full");
            AssertRooms(FractureRooms);
        }

        [UnityTest]
        public IEnumerator 균열_주_동선_열두_방이_지형으로_이어진다()
        {
            yield return LoadZone("Zone_Fracture_Full");
            AssertConnected(FractureMainPath);
        }

        [UnityTest]
        public IEnumerator 균열_체크포인트_셋과_숏컷_셋이_있다()
        {
            yield return LoadZone("Zone_Fracture_Full");

            var checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            Assert.AreEqual(3, checkpoints.Length, "명세 8.1절: 체크포인트는 3개다.");

            var ids = new HashSet<string>();
            foreach (var shortcut in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
                ids.Add(shortcut.Id);

            Assert.IsTrue(ids.Contains("fracture_shortcut_a"), "숏컷 A(F05→F03)가 없다.");
            Assert.IsTrue(ids.Contains("fracture_shortcut_b"), "숏컷 B(F08→F03)가 없다.");
            Assert.IsTrue(ids.Contains("fracture_shortcut_c"), "숏컷 C(F10→F07)가 없다.");
        }

        // 명세 1.2절·5절: 균열에서 자각은 아무것도 드러내지 않으며, 비밀방의 조건으로도
        // 쓰이지 않는다. 자각 반응 오브젝트를 하나라도 두면 그 규칙이 깨진다.
        [UnityTest]
        public IEnumerator 균열에는_자각으로_여는_문이_하나도_없다()
        {
            yield return LoadZone("Zone_Fracture_Full");

            var revealed = Object.FindObjectsByType<AwarenessRevealed>(FindObjectsSortMode.None);
            Assert.AreEqual(0, revealed.Length,
                "균열에 자각 반응 오브젝트가 있다: " + (revealed.Length > 0 ? revealed[0].name : ""));

            var hidden = Object.FindObjectsByType<HiddenFragment>(FindObjectsSortMode.None);
            Assert.AreEqual(0, hidden.Length, "균열에 자각으로만 보이는 파편이 있다.");
        }

        // 명세 10절: 붕괴 발판은 사망 후 항상 같은 시작 위상으로 돌아간다. 이 지역에는
        // 되감기가 없으므로 스스로 되살아나지 않으면 한 번 무너진 발판이 영영 사라진다.
        [UnityTest]
        public IEnumerator 균열의_붕괴_발판은_스스로_되살아난다()
        {
            yield return LoadZone("Zone_Fracture_Full");

            var platforms = Object.FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None);
            Assert.Greater(platforms.Length, 0, "균열에 붕괴 발판이 하나도 없다.");

            foreach (var platform in platforms)
                Assert.Greater(platform.RespawnDelay, 0f,
                    platform.name + ": 되감기가 없는 지역인데 스스로 되살아나지 않는다.");

            Debug.Log("===== 붕괴 발판 " + platforms.Length + "개 모두 자동 복구 =====");
        }

        // 예지가 볼 대상이 실제로 배치되어 있는지. 이 지역의 유일한 정보원이 예지이므로
        // 예측 가능한 대상이 없으면 능력 자체가 의미를 잃는다.
        [UnityTest]
        public IEnumerator 균열에_예지로_볼_대상이_충분히_있다()
        {
            yield return LoadZone("Zone_Fracture_Full");

            int count = 0;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is IForeseeable) count++;

            Debug.Log("===== 예지 대상 " + count + "개 =====");
            Assert.Greater(count, 12, "예지로 볼 수 있는 대상이 너무 적다.");
        }

        [UnityTest]
        public IEnumerator 균열_모든_방에_원본_단일_배경이_연결돼_있다()
        {
            yield return LoadZone("Zone_Fracture_Full");
            foreach (var roomSpec in FractureRooms)
            {
                var room = GameObject.Find(roomSpec.name);
                Assert.IsNotNull(room, roomSpec.name + " 방이 없다.");

                var art = room.transform.Find("Art");
                Assert.IsNotNull(art, roomSpec.name + "에 배경이 없다.");
                Assert.IsNotNull(art.GetComponent<RoomVisualCuller>(),
                    roomSpec.name + " 배경에 현재 방 렌더링 제한이 없다.");

                var background = art.Find("RoomBackground");
                Assert.IsNotNull(background, roomSpec.name + "/RoomBackground가 없다.");
                var renderer = background.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer);
                Assert.IsNotNull(renderer.sprite, roomSpec.name + " 원본 배경이 연결되지 않았다.");
                Assert.That(renderer.sprite.name, Is.EqualTo(roomSpec.name));
                Assert.IsNotNull(background.GetComponent<CameraLockedRoomBackground>());

                foreach (var legacy in new[] { "BG_Far", "BG_Mid", "FG_Overlay" })
                    Assert.IsNull(art.Find(legacy), roomSpec.name + "/" + legacy + "는 빠져야 한다.");
                Assert.IsNull(room.transform.Find("MotionBack"), roomSpec.name + "/MotionBack은 빠져야 한다.");
                Assert.IsNull(room.transform.Find("MotionFront"), roomSpec.name + "/MotionFront는 빠져야 한다.");
            }
        }
    }
}
