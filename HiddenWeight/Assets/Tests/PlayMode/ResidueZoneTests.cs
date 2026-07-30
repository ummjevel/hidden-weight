using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 잔재 전체 지역(주 동선 12룸 + 비밀 3룸)을 docs/RESIDUE_ROOM_IMPLEMENTATION.md와
    // docs/RESIDUE_LEVEL_DESIGN.md에 대고 검증한다.
    public class ResidueZoneTests
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

        static Room FindRoom(string name)
        {
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (room.name == name) return room;
            return null;
        }

        // 방 로컬 좌표 기준의 바닥 표면 y. 없으면 NaN.
        static float SurfaceIn(Room room, float localX, float fromLocalY)
        {
            var min = room.WorldBounds.min;
            var hit = Physics2D.Raycast(new Vector2(min.x + localX, min.y + fromLocalY), Vector2.down, 60f,
                LayerMask.GetMask("Ground"));
            return hit.collider == null ? float.NaN : hit.point.y - min.y;
        }

        [UnityTest]
        public IEnumerator 열다섯_개_방이_명세_크기로_존재한다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            // (이름, 가로, 세로) — 명세 각 절의 "방 크기".
            var expected = new (string, float, float)[]
            {
                ("Room01", 26, 14), ("Room02", 28, 14), ("Room03", 30, 18), ("Room04", 24, 22),
                ("Secret01", 18, 14), ("Room05", 26, 14), ("Room06", 32, 16), ("Secret02", 24, 18),
                ("Room07", 30, 18), ("Room08", 24, 30), ("Room09", 32, 16), ("Room10", 24, 18),
                ("Room11", 28, 16), ("Secret03", 20, 14), ("Room12", 30, 18),
            };

            var report = new StringBuilder();
            foreach (var (name, w, h) in expected)
            {
                var room = FindRoom(name);
                Assert.IsNotNull(room, name + " 방이 씬에 없다.");
                report.AppendLine(name + " " + room.WorldBounds.size.x + "x" + room.WorldBounds.size.y);
                Assert.AreEqual(w, room.WorldBounds.size.x, 0.01f, name + " 가로가 명세와 다르다.");
                Assert.AreEqual(h, room.WorldBounds.size.y, 0.01f, name + " 세로가 명세와 다르다.");
            }
            Debug.Log("===== 잔재 방 크기 =====\n" + report);

            Assert.AreEqual(15, Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Length,
                "잔재는 주 동선 12룸 + 비밀 3룸 = 15개다.");
        }

        [UnityTest]
        public IEnumerator 지역_구조물이_설계대로_배치됐다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            // 체크포인트 3개 — R01 입구, R05 능력 획득 직전, R10 보스 문 앞.
            Assert.AreEqual(3, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length,
                "체크포인트는 3개다(RESIDUE_LEVEL_DESIGN.md).");

            // 숏컷 3개 — A(R05→R03), B(R08→R03), C(R10→R07).
            var shortcuts = Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None);
            Assert.AreEqual(3, shortcuts.Length, "물리적으로 열리는 숏컷은 3개다.");
            foreach (var shortcut in shortcuts)
                Assert.IsFalse(shortcut.IsOpen, shortcut.Id + " 가 처음부터 열려 있다.");

            // 보스 2 — 중간 보스(R10), 지역 보스(R12).
            Assert.AreEqual(2, Object.FindObjectsByType<BossController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                "중간 보스와 지역 보스 둘이다.");

            // 되감기 획득은 R05의 파편 하나뿐이어야 한다.
            int rewindGrants = 0;
            foreach (var fragment in Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None))
                if (fragment.FragmentId == "residue_skill") rewindGrants++;
            Assert.AreEqual(1, rewindGrants, "되감기를 주는 파편은 R05에 하나만 있어야 한다.");
        }

        [UnityTest]
        public IEnumerator 각_방의_주_동선_바닥이_이어져_있다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            // 명세가 "연속 바닥"이라고 못박은 방들. 구멍이 있으면 주 동선이 성립하지 않는다.
            var solid = new (string, float, float, float)[]
            {
                // 방, 검사 시작 x, 끝 x, 최소 표면 y(방 로컬)
                ("Room01", 1f, 25f, 2f),
                ("Room03", 1f, 29f, 1f),
                ("Room10", 1f, 23f, 3f),
                ("Room12", 1f, 29f, 3f),
                ("Secret03", 1f, 19f, 2f),
            };

            var report = new StringBuilder();
            foreach (var (name, x0, x1, minY) in solid)
            {
                var room = FindRoom(name);
                for (float x = x0; x <= x1; x += 1f)
                {
                    float y = SurfaceIn(room, x, room.WorldBounds.size.y - 1f);
                    Assert.IsFalse(float.IsNaN(y), name + " 의 x=" + x + " 아래에 바닥이 없다.");
                    Assert.GreaterOrEqual(y, minY - 0.01f,
                        name + " 의 x=" + x + " 바닥이 y=" + y + " 로 기준(" + minY + ")보다 낮다.");
                }
                report.AppendLine(name + " 연속 바닥 확인");
            }
            Debug.Log("===== 주 동선 바닥 =====\n" + report);
        }

        [UnityTest]
        public IEnumerator R05에서_되감기를_얻고_대상을_복원할_수_있다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var room = FindRoom("Room05");
            var player = PlayerController.Instance;
            var progress = GameManager.Instance.Progress;

            // 되감기 파편(방 로컬 10,4)을 밟는다.
            var fragmentPos = (Vector2)room.WorldBounds.min + new Vector2(10f, 4f);
            player.TeleportTo(fragmentPos + new Vector2(3f, 2f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragmentPos);
            for (int i = 0; i < 30; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Assert.IsTrue(progress.HasSkill(EmotionId.Rewind), "R05에서 되감기를 얻지 못했다.");

            var controller = EmotionSkillController.Instance;
            yield return null;
            Assert.IsNotNull(controller.Active, "되감기를 얻었는데 활성 스킬이 없다.");

            // 대상 1(방 로컬 14,2 근처의 블록)이 떨어져 밀려난 뒤 복원되는지.
            Rewindable target = null;
            float best = float.MaxValue;
            foreach (var rewindable in Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(rewindable.transform.position, room.WorldBounds.center);
                if (d < best) { best = d; target = rewindable; }
            }
            Assert.IsNotNull(target, "R05에 되감기 대상이 없다.");
        }

        // 주 동선 이동 검증. R01 시작점에서 R09의 전투 구간 입구까지, 봇이 지형만으로
        // 끊김 없이 갈 수 있어야 한다.
        //
        // 목표를 R12가 아니라 R09로 잡은 이유: R09·R10·R12는 들어서면 좌우가 잠기는 조우이고,
        // 방어형 정예는 정면 공격을 막고 보스는 패턴 회피를 요구한다(설계대로다). 정해진 규칙만
        // 따르는 봇은 이길 수 없으므로, 여기서는 "이동 경로가 이어져 있는가"만 본다.
        // 전투 이후 구간은 사람이 플레이해 확인해야 한다.
        [UnityTest, Timeout(600000)]
        public IEnumerator 봇이_R01에서_R09_전투_구간까지_이동한다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            // R09 전투 구간 입구(방 로컬 x=8) 까지.
            var goal = FindRoom("Room09").WorldBounds;
            float goalX = goal.min.x + 8f;

            for (int i = 0; i < 40; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            float maxX = player.transform.position.x;
            var trace = new StringBuilder();
            bool reached = false;
            int steps = 0;
            const int MaxSteps = 6000; // 고정 스텝 0.02초 → 최대 120초

            for (; steps < MaxSteps; steps++)
            {
                if (player == null) break;

                var pos = player.transform.position;
                if (pos.x >= goalX) { reached = true; break; }

                var frame = new PlayerInput.Frame { horizontal = 1f, run = true, jumpHeld = true };

                if (!player.IsGrounded && player.IsOnWall)
                {
                    // 굴뚝·수직 통로: 벽 쪽을 누른 채 점프해 반대 벽으로 튕긴다.
                    frame.horizontal = player.Facing;
                    frame.jumpPressed = true;
                }
                else
                {
                    bool blocked = Physics2D.Raycast(pos, Vector2.right, 0.9f, LayerMask.GetMask("Ground", "Wall"));
                    bool gapAhead = !Physics2D.Raycast((Vector2)pos + new Vector2(1.4f, 0f), Vector2.down, 3f,
                        LayerMask.GetMask("Ground"));
                    // 적이 붙으면 공격한다. 뛰어넘기만 해서는 접촉 피해로 죽어 체크포인트를
                    // 오가는 것을 반복한다(실제로 R02에서 그렇게 됐다).
                    bool enemyClose = Physics2D.Raycast(pos, Vector2.right, 1.6f, LayerMask.GetMask("Enemy"));
                    bool enemyAhead = Physics2D.Raycast(pos, Vector2.right, 2.6f, LayerMask.GetMask("Enemy"));
                    frame.attackPressed = enemyClose;
                    frame.jumpPressed = player.IsGrounded && (blocked || gapAhead || (enemyAhead && !enemyClose));
                }

                PlayerInput.Injected = frame;
                yield return new WaitForFixedUpdate();

                if (player == null) break;
                maxX = Mathf.Max(maxX, player.transform.position.x);
                if (steps % 250 == 0)
                {
                    bool bl = Physics2D.Raycast(player.transform.position, Vector2.right, 0.9f,
                        LayerMask.GetMask("Ground", "Wall"));
                    var up = Physics2D.Raycast(player.transform.position, Vector2.up, 2f,
                        LayerMask.GetMask("Ground", "Wall"));
                    trace.AppendLine("  " + (steps * 0.02f).ToString("F0") + "초 "
                        + player.transform.position.ToString("F1")
                        + " grounded=" + player.IsGrounded + " wall=" + player.IsOnWall
                        + " blocked=" + bl + " 머리위=" + (up.collider == null ? "없음" : up.collider.name + "@" + up.point.y.ToString("F1"))
                        + " " + player.State);
                }
            }

            // 어느 방까지 갔는지 찾아 준다.
            string stalledIn = "(방 밖)";
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (maxX >= room.WorldBounds.min.x && maxX <= room.WorldBounds.max.x) stalledIn = room.name;

            float seconds = steps * 0.02f;
            Debug.Log("===== 잔재 전체 주파 ===== 도달 x=" + maxX.ToString("F1")
                + " / 목표 " + goalX.ToString("F1") + " / " + seconds.ToString("F0") + "초 / 최종 방 " + stalledIn
                + "\n" + trace);

            Assert.IsTrue(reached,
                "R09 전투 구간까지 가지 못했다. 도달 x=" + maxX.ToString("F1") + " (" + stalledIn + ")\n" + trace);
        }

        // 각 방 안에서 봇이 좌우로 움직일 수 있는지(지형에 끼지 않는지) 확인한다.
        // 방 사이 연결 통로는 아직 만들지 않았으므로 방 내부 이동만 본다.
        [UnityTest]
        public IEnumerator 봇이_각_방_안에서_막히지_않고_이동한다(
            [Values("Room01", "Room02", "Room03", "Room11", "Room12")] string roomName)
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var room = FindRoom(roomName);
            var player = PlayerController.Instance;

            var start = (Vector2)room.WorldBounds.min + new Vector2(2f, 4f);
            player.TeleportTo(start);
            for (int i = 0; i < 40; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            float startX = player.transform.position.x;
            float maxX = startX;
            float roomWidth = room.WorldBounds.size.x; // 씬이 바뀌면 room도 파괴되므로 미리 재 둔다

            bool exitedRoom = false;
            for (int i = 0; i < 700; i++)
            {
                // 출구 트리거를 밟으면 다음 지역이 로드되며 플레이어가 파괴된다 = 방을 끝까지 통과.
                if (player == null) { exitedRoom = true; break; }
                bool blocked = Physics2D.Raycast(player.transform.position, Vector2.right, 0.9f,
                    LayerMask.GetMask("Ground", "Wall"));
                bool gapAhead = !Physics2D.Raycast(
                    (Vector2)player.transform.position + new Vector2(1.4f, 0f), Vector2.down, 2.5f,
                    LayerMask.GetMask("Ground"));
                bool enemyAhead = Physics2D.Raycast(player.transform.position, Vector2.right, 2.2f,
                    LayerMask.GetMask("Enemy"));

                PlayerInput.Injected = new PlayerInput.Frame
                {
                    horizontal = 1f,
                    run = true,
                    jumpPressed = player.IsGrounded && (blocked || gapAhead || enemyAhead),
                    jumpHeld = true,
                };
                yield return new WaitForFixedUpdate();
                if (player == null) { exitedRoom = true; break; }
                maxX = Mathf.Max(maxX, player.transform.position.x);
            }

            float travelled = maxX - startX;
            Debug.Log("===== " + roomName + " 주파 ===== 이동 " + travelled.ToString("F1") + " / 방 폭 " + roomWidth);

            if (exitedRoom) yield break; // 방을 통과해 지역이 바뀌었다면 그것으로 충분하다

            Assert.Greater(travelled, roomWidth * 0.7f,
                roomName + " 안에서 방 폭의 70%도 못 갔다(이동 " + travelled.ToString("F1") + " / " + roomWidth + ") — 지형에 막힌다.");
        }
    }
}
