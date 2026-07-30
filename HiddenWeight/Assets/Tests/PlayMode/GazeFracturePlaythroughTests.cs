using System.Collections;
using System.Collections.Generic;
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
    // 봇이 응시·균열의 방을 실제로 걸어서 통과해 본다. 구조 검사(GazeFractureZoneTests)는
    // "방이 있고 통로가 이어져 있다"까지만 봤고, 여기서는 "사람이 실제로 지나갈 수 있는가"를 본다.
    //
    // 적·시선·조우 잠금은 끄고 돌린다. 지형이 통과 가능한지와 전투가 이길 만한지는 다른
    // 문제이고, 섞어 놓으면 실패했을 때 원인이 지형인지 전투인지 구분할 수 없기 때문이다.
    // 여기서 실패하면 그 방은 아무리 잘해도 못 지나간다는 뜻이다.
    public class GazeFracturePlaythroughTests
    {
        // 방 하나에 주는 기본 시간(물리 스텝). 0.02초 × 400 = 8초.
        const int DefaultSteps = 400;
        // 승강기 방은 기다리는 시간이 대부분이라 따로 넉넉히 준다.
        const int LiftRoomSteps = 900;

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        // (방 이름, 입구 로컬 x, 목표 로컬 x, 목표 로컬 y — 0이면 높이는 보지 않음)
        struct Leg
        {
            public string Room;
            public float StartX;
            public float GoalX;
            public float GoalY;
            public int Steps;

            public Leg(string room, float startX, float goalX, float goalY = 0f, int steps = DefaultSteps)
            {
                Room = room; StartX = startX; GoalX = goalX; GoalY = goalY; Steps = steps;
            }
        }

        // 각 빌더의 방 로컬 좌표 그대로다(Room의 WorldBounds.min이 곧 빌더의 오프셋이라 일치한다).
        static readonly Leg[] GazeLegs =
        {
            new Leg("GazeRoom01", 3, 26),
            new Leg("GazeRoom02", 0, 28),
            new Leg("GazeRoom03", 0, 28),
            new Leg("GazeRoom04", 2, 22),   // 상층 관찰대에서 시작해 하층으로 내려간다
            new Leg("GazeRoom05", 0, 26),   // 낮은 입구 — 숨죽이기 필요
            new Leg("GazeRoom06", 0, 32),   // 낮은 천장 2곳 — 숨죽이기 필요
            new Leg("GazeRoom07", 0, 34),
            new Leg("GazeRoom08", 6, 22, 25, LiftRoomSteps), // 승강기 위에서 출발해 타고 오른다
            new Leg("GazeRoom09", 0, 32),
            new Leg("GazeRoom10", 0, 24),
            new Leg("GazeRoom11", 0, 28),
            new Leg("GazeRoom12", 0, 28),
        };

        static readonly Leg[] FractureLegs =
        {
            new Leg("FractureRoom01", 3, 26),
            new Leg("FractureRoom02", 0, 28),
            new Leg("FractureRoom03", 0, 28),
            new Leg("FractureRoom04", 2, 22),
            new Leg("FractureRoom05", 0, 26),
            new Leg("FractureRoom06", 0, 32),
            new Leg("FractureRoom07", 0, 34),
            new Leg("FractureRoom08", 6, 22, 25, LiftRoomSteps),
            new Leg("FractureRoom09", 0, 32),
            new Leg("FractureRoom10", 0, 24),
            new Leg("FractureRoom11", 0, 28),
            new Leg("FractureRoom12", 0, 28),
        };

        static Room FindRoom(string name)
        {
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (room.name == name) return room;
            return null;
        }

        // 전투 요소를 전부 재운다. 지형만 남긴다.
        //
        // 지역 출구(ZoneTrigger)도 함께 끈다. 봇이 방 끝까지 걸어가면 출구를 밟아 다음 씬이
        // 로드되고, 그러면 검사 중이던 오브젝트가 통째로 파괴된다 — 실제로 붕괴 발판 검사가
        // MissingReferenceException으로 죽었다.
        static string SilenceCombat()
        {
            int enemies = 0, gazes = 0, hazards = 0, encounters = 0, exits = 0;

            foreach (var exit in Object.FindObjectsByType<ZoneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { exit.gameObject.SetActive(false); exits++; }

            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { enemy.gameObject.SetActive(false); enemies++; }

            foreach (var gaze in Object.FindObjectsByType<GazeHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { gaze.gameObject.SetActive(false); gazes++; }

            foreach (var hazard in Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { hazard.gameObject.SetActive(false); hazards++; }

            foreach (var blast in Object.FindObjectsByType<DelayedBlast>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { blast.gameObject.SetActive(false); }

            // 조우는 오브젝트째 끄면 안 된다 — 잠금 벽이 그 자식이라 같이 사라지는 것이 맞지만,
            // 컴포넌트만 꺼서 "전투가 시작되지 않는" 상태로 둔다.
            foreach (var encounter in Object.FindObjectsByType<Encounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { encounter.enabled = false; encounters++; }

            return $"적 {enemies} / 시선 {gazes} / 위험영역 {hazards} / 조우 {encounters} / 지역출구 {exits} 비활성화";
        }

        // 방 안 startLocalX 위치의 바닥을 찾아 그 위에 세운다.
        static bool PlaceAtFloor(PlayerController player, Room room, float startLocalX, out Vector3 spawn)
        {
            var min = room.WorldBounds.min;
            var max = room.WorldBounds.max;
            float x = min.x + startLocalX;

            var hit = Physics2D.Raycast(new Vector2(x, max.y + 2f), Vector2.down,
                (max.y + 2f) - (min.y - 4f), LayerMask.GetMask("Ground"));
            if (hit.collider == null) { spawn = Vector3.zero; return false; }

            spawn = new Vector3(x, hit.point.y + 1.2f, 0f);
            player.TeleportTo(spawn);
            return true;
        }

        // 오른쪽으로 나아가는 봇. 막히거나 앞이 끊기면 점프하고, 벽에 붙으면 벽점프한다.
        // 진행이 멈추면 숨죽이기를 켜 본다(응시의 낮은 천장 대비) — 사람이 할 법한 순서다.
        static IEnumerator RunBot(PlayerController player, Room room, Leg leg, System.Action<string> report)
        {
            var min = room.WorldBounds.min;
            float goalX = min.x + leg.GoalX;
            float goalY = leg.GoalY > 0f ? min.y + leg.GoalY : float.NegativeInfinity;

            int groundMask = LayerMask.GetMask("Ground");
            int blockMask = LayerMask.GetMask("Ground", "Wall");

            var body = player.GetComponent<Rigidbody2D>();
            var controller = EmotionSkillController.Instance;
            bool canHush = GameManager.Instance.Progress.HasSkill(EmotionId.Hush);

            float bestX = player.transform.position.x;
            float bestY = player.transform.position.y;
            float lastProgressX = bestX;
            int stuckSteps = 0;
            bool hush = false;
            int hushProgress = 0;
            LiftPlatform lift = null;
            int liftMiss = 0;

            for (int step = 0; step < leg.Steps; step++)
            {
                if (player == null) break;

                var pos = player.transform.position;

                // 세계 밖으로 떨어졌으면 그 방은 실패다.
                if (pos.y < min.y - 12f)
                {
                    report($"    ! 방 아래로 추락 (y={pos.y:F1})");
                    break;
                }

                // 승강기 위에 올라섰으면 멈춰서 끝까지 타고 간다. 계속 달리면 바로 내려버린다.
                // 판정이 한 프레임 끊겼다고 바로 내리지 않도록 몇 프레임은 봐준다.
                var under = Physics2D.Raycast(pos + Vector3.up * 0.1f, Vector2.down, 2f, groundMask);
                var underLift = under.collider != null
                    ? under.collider.GetComponentInParent<LiftPlatform>() : null;
                if (underLift != null) { lift = underLift; liftMiss = 0; }
                else if (lift != null && ++liftMiss > 10) lift = null;

                bool ridingLift = lift != null && !lift.IsFinished;

                var frame = new PlayerInput.Frame { jumpHeld = true, skillHeld = hush };

                if (ridingLift)
                {
                    frame.horizontal = 0f;
                    frame.run = false;
                }
                else
                {
                    frame.horizontal = 1f;
                    frame.run = !hush;

                    if (player.IsOnWall && !player.IsGrounded)
                    {
                        frame.horizontal = player.Facing;
                        frame.jumpPressed = true;
                    }
                    else if (player.IsGrounded)
                    {
                        var origin = (Vector2)pos + new Vector2(0f, 0.15f);
                        bool wallAhead = Physics2D.Raycast(origin, Vector2.right, 0.9f, blockMask);
                        bool gapAhead = !Physics2D.Raycast((Vector2)pos + new Vector2(1.4f, 0.2f),
                            Vector2.down, 2.5f, groundMask);
                        if (wallAhead || gapAhead || stuckSteps > 25) frame.jumpPressed = true;
                    }
                }

                PlayerInput.Injected = frame;
                if (controller != null && canHush) controller.enabled = true;
                yield return new WaitForFixedUpdate();

                if (player == null) break;
                pos = player.transform.position;
                bestX = Mathf.Max(bestX, pos.x);
                bestY = Mathf.Max(bestY, pos.y);

                if (pos.x > lastProgressX + 0.25f)
                {
                    lastProgressX = pos.x;
                    stuckSteps = 0;
                    if (hush) hushProgress++;
                }
                else stuckSteps++;

                // 1초 넘게 못 나아가면 숨죽이기를 켜 본다. 그래도 안 되면 다시 끈다.
                if (canHush && stuckSteps == 50) { hush = true; hushProgress = 0; }
                if (canHush && stuckSteps >= 150) { hush = false; stuckSteps = 60; }

                // 낮은 천장을 빠져나온 뒤에는 다시 푼다. 켜 둔 채로 두면 이동이 느려져
                // 방 끝까지 가지 못한다 — 사람도 틈을 지나면 바로 일어선다.
                if (hush && hushProgress > 12) { hush = false; hushProgress = 0; }

                bool reachedX = bestX >= goalX - 1f;
                bool reachedY = leg.GoalY <= 0f || bestY >= goalY - 1f;
                if (reachedX && reachedY) break;
            }

            // 숨죽이기를 켠 채로 다음 방으로 넘어가지 않도록 정리한다.
            PlayerInput.Injected = new PlayerInput.Frame();
            yield return new WaitForFixedUpdate();

            float needX = goalX;
            bool okX = bestX >= needX - 1f;
            bool okY = leg.GoalY <= 0f || bestY >= goalY - 1f;

            string obstruction = "";
            if (!okX || !okY)
            {
                var nearby = Physics2D.OverlapBoxAll(player.transform.position, new Vector2(2.5f, 3f), 0f,
                    blockMask);
                var names = new StringBuilder();
                foreach (var hit in nearby)
                {
                    if (names.Length > 0) names.Append(", ");
                    names.Append(hit.name);
                }
                obstruction = $" 위치={(Vector2)player.transform.position} 주변=[{names}]";
            }

            report($"  {leg.Room,-18} x {bestX - min.x,6:F1} / {leg.GoalX,-4} "
                 + (leg.GoalY > 0f ? $"y {bestY - min.y,5:F1} / {leg.GoalY,-4} " : "")
                 + (okX && okY ? "통과" : "← 막힘" + obstruction));

            BotResult = okX && okY;
        }

        // RunBot이 코루틴이라 반환값을 못 주므로 정적 필드로 받는다.
        static bool BotResult;

        static IEnumerator RunZone(string sceneName, Leg[] legs, bool grantHush)
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            var report = new StringBuilder();
            report.AppendLine("===== " + sceneName + " 봇 주파 =====");
            report.AppendLine("  " + SilenceCombat());

            var player = PlayerController.Instance;
            Assert.IsNotNull(player, sceneName + ": 플레이어가 없다.");

            if (grantHush)
            {
                // 이 지역의 필수 능력을 미리 준다. 능력 획득 자체는 별도 테스트가 본다.
                GameManager.Instance.Progress.UnlockSkill(EmotionId.Hush);
                if (EmotionSkillController.Instance != null) EmotionSkillController.Instance.RefreshActive();
                report.AppendLine("  숨죽이기 보유 상태로 주행");
            }

            var blocked = new List<string>();

            foreach (var leg in legs)
            {
                var room = FindRoom(leg.Room);
                if (room == null) { blocked.Add(leg.Room + " (방 없음)"); continue; }

                if (!PlaceAtFloor(player, room, leg.StartX, out _))
                {
                    report.AppendLine($"  {leg.Room,-18} 입구 아래에 바닥이 없다 ← 막힘");
                    blocked.Add(leg.Room + " (입구 바닥 없음)");
                    continue;
                }

                // 착지·안정화.
                for (int i = 0; i < 25; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

                // 이 픽스처는 전투가 아니라 지형만 검증한다. 비활성화 직전에 생성된 공격체나
                // 지연 판정 때문에 죽어 시작 체크포인트로 돌아가면 지형 막힘으로 오인되므로,
                // 해당 방을 달리는 짧은 시간 동안만 체력을 채우고 피해를 무시한다.
                var health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.RestoreFull();
                    health.GrantInvulnerability(leg.Steps * Time.fixedDeltaTime + 1f);
                }

                BotResult = false;
                yield return RunBot(player, room, leg, line => report.AppendLine(line));
                if (!BotResult) blocked.Add(leg.Room);
            }

            Debug.Log(report.ToString());

            Assert.IsEmpty(blocked,
                blocked.Count + "개 방을 봇이 통과하지 못했다: " + string.Join(", ", blocked)
                + "\n" + report);
        }

        [UnityTest]
        public IEnumerator 응시_봇이_열두_방을_모두_통과한다()
        {
            yield return RunZone("Zone_Gaze_Full", GazeLegs, grantHush: true);
        }

        [UnityTest]
        public IEnumerator 균열_봇이_열두_방을_모두_통과한다()
        {
            yield return RunZone("Zone_Fracture_Full", FractureLegs, grantHush: false);
        }

        // 승강기가 실제로 플레이어를 태우고 꼭대기까지 올라가고, 그 결과로 숏컷이 열리는지.
        // 방 주파 검사와 따로 두는 이유: 저기서 실패하면 원인이 봇의 길찾기인지 승강기인지
        // 구분되지 않는다. 여기서는 승강기 위에 올려놓고 기다리기만 한다.
        [UnityTest]
        public IEnumerator 승강기가_플레이어를_태우고_올라가_숏컷을_연다(
            [Values("Zone_Gaze_Full", "Zone_Fracture_Full")] string sceneName,
            [Values("gaze_shortcut_b", "fracture_shortcut_b")] string shortcutId)
        {
            // [Values] 조합이 교차로 생기므로 짝이 맞는 것만 실행한다.
            if (sceneName.Contains("Gaze") != shortcutId.StartsWith("gaze")) yield break;

            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            SilenceCombat();

            var player = PlayerController.Instance;
            var lift = Object.FindFirstObjectByType<LiftPlatform>();
            Assert.IsNotNull(lift, sceneName + "에 승강기가 없다.");

            Shortcut shortcut = null;
            foreach (var candidate in Object.FindObjectsByType<Shortcut>(FindObjectsSortMode.None))
                if (candidate.Id == shortcutId) shortcut = candidate;
            Assert.IsNotNull(shortcut, shortcutId + " 숏컷이 없다.");
            Assert.IsFalse(shortcut.IsOpen, shortcutId + "이 처음부터 열려 있다.");

            float startY = lift.transform.position.y;
            player.TeleportTo(lift.transform.position + new Vector3(0f, 1.2f, 0f));

            // 올라타기만 하고 아무 조작도 하지 않는다. 승강기가 알아서 데려가야 한다.
            float ridden = 0f;
            for (int i = 0; i < 1200 && !lift.IsFinished; i++)
            {
                PlayerInput.Injected = default;
                yield return new WaitForFixedUpdate();
                ridden = Mathf.Max(ridden, player.transform.position.y - startY);
            }

            float liftRise = lift.transform.position.y - startY;
            Debug.Log("===== 승강기 ===== " + sceneName
                + " 발판 상승=" + liftRise.ToString("F1")
                + " 플레이어 상승=" + ridden.ToString("F1")
                + " 도착=" + lift.IsFinished + " 숏컷=" + shortcut.IsOpen);

            Assert.IsTrue(lift.IsFinished, "승강기가 종점까지 가지 못했다(상승 " + liftRise.ToString("F1") + ").");
            Assert.Greater(ridden, liftRise - 1.5f,
                "승강기는 올라갔는데 플레이어가 함께 올라가지 못했다(발판 " + liftRise.ToString("F1")
                + " / 플레이어 " + ridden.ToString("F1") + ").");
            Assert.IsTrue(shortcut.IsOpen, "승강기가 종점에 닿았는데 숏컷이 열리지 않았다.");
        }

        [UnityTest]
        public IEnumerator 열린_숏컷의_승강기는_재방문하면_종점에서_시작한다(
            [Values("Zone_Gaze_Full", "Zone_Fracture_Full")] string sceneName,
            [Values("gaze_shortcut_b", "fracture_shortcut_b")] string shortcutId)
        {
            // [Values]의 교차 조합 중 같은 지역의 짝만 검사한다.
            if (sceneName.Contains("Gaze") != shortcutId.StartsWith("gaze")) yield break;

            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            var firstLift = Object.FindFirstObjectByType<LiftPlatform>();
            Assert.IsNotNull(firstLift, sceneName + "에 승강기가 없다.");
            Vector3 firstPosition = firstLift.transform.position;

            GameManager.Instance.Progress.MarkShortcutOpen(shortcutId);
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            var restoredLift = Object.FindFirstObjectByType<LiftPlatform>();
            Assert.IsNotNull(restoredLift, sceneName + " 재방문 후 승강기가 없다.");

            float restoredDistance = Vector3.Distance(firstPosition, restoredLift.transform.position);
            Debug.Log("===== 승강기 재방문 ===== " + sceneName
                + " 종점복원=" + restoredLift.IsFinished
                + " 이동거리=" + restoredDistance.ToString("F1"));

            Assert.IsTrue(restoredLift.IsFinished, "열린 숏컷인데 승강기가 다시 출발 대기 상태다.");
            Assert.Greater(restoredDistance, 5f, "열린 숏컷인데 승강기가 시작 위치에 남아 있다.");
        }

        // 능력 파편이 실제로 스킬을 주는지. 준다고 코드에 적혀 있는 것과 실제로 먹히는 것은 다르다.
        [UnityTest]
        public IEnumerator 응시_파편이_숨죽이기를_자각이_해금된다()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Gaze_Full", LoadSceneMode.Single);
            yield return null;
            SilenceCombat();

            var player = PlayerController.Instance;
            var progress = GameManager.Instance.Progress;

            // ZoneMarker가 지역을 선언했는지 먼저 확인한다. 여기가 틀리면 활성 스킬 계산이
            // 통째로 어긋난다(씬 이름이 ZoneData.sceneName과 달라 이름 매칭은 실패한다).
            var loadedZone = GameManager.Instance.CurrentZoneData;
            Assert.IsNotNull(loadedZone, "Zone_Gaze_Full에서 지역 데이터가 잡히지 않았다 — ZoneMarker 확인 필요.");
            Assert.AreEqual(ZoneId.Gaze, loadedZone.id,
                "Zone_Gaze_Full인데 현재 지역이 " + loadedZone.displayName + "로 잡혔다.");

            var fragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                f => f.FragmentId == "gaze_skill");
            Assert.IsNotNull(fragment, "G05에 숨죽이기 파편이 없다.");

            player.TeleportTo(fragment.transform.position + new Vector3(3f, 1f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragment.transform.position);
            for (int i = 0; i < 30; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Assert.IsTrue(progress.HasSkill(EmotionId.Hush), "파편을 먹었는데 숨죽이기가 해금되지 않았다.");

            // 활성 스킬은 지역 데이터(grantedSkill)를 통해 정해지므로, 실패했을 때
            // 어디가 끊겼는지 바로 보이도록 남긴다.
            for (int i = 0; i < 5; i++) yield return null;
            var zoneData = GameManager.Instance.CurrentZoneData;
            var controller = EmotionSkillController.Instance;
            Debug.Log("===== 활성 스킬 진단 ====="
                + " 지역=" + (zoneData == null ? "(null)" : zoneData.displayName)
                + " grantedSkill=" + (zoneData == null ? "-" : zoneData.grantedSkill.ToString())
                + " 컨트롤러=" + (controller == null ? "(null)" : controller.name)
                + " 컨트롤러enabled=" + (controller != null && controller.enabled)
                + " 스킬컴포넌트=" + (controller == null ? 0 : controller.GetComponents<EmotionSkill>().Length)
                + " HasSkill=" + progress.HasSkill(EmotionId.Hush));

            var active = controller.Active;
            Assert.IsNotNull(active, "숨죽이기가 활성 스킬로 잡히지 않았다.");
            Assert.AreEqual(EmotionId.Hush, active.Id, "활성 스킬이 숨죽이기가 아니다.");

            // 자각 해금 지점(G11)에 들어가 본다.
            var unlock = Object.FindFirstObjectByType<AwarenessUnlockMoment>();
            Assert.IsNotNull(unlock, "G11에 자각 해금 지점이 없다.");

            player.TeleportTo(unlock.transform.position + new Vector3(4f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(unlock.transform.position);
            for (int i = 0; i < 320; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Debug.Log("===== 응시 능력 ===== 숨죽이기=" + progress.HasSkill(EmotionId.Hush)
                + " 자각=" + progress.HasAwareness);

            Assert.IsTrue(progress.HasAwareness, "자각 해금 지점을 밟았는데 자각이 열리지 않았다.");
        }

        [UnityTest]
        public IEnumerator 균열_파편이_예지를_해금한다()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Fracture_Full", LoadSceneMode.Single);
            yield return null;
            SilenceCombat();

            var player = PlayerController.Instance;
            var progress = GameManager.Instance.Progress;

            var fragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                f => f.FragmentId == "fracture_skill");
            Assert.IsNotNull(fragment, "F05에 예지 파편이 없다.");

            player.TeleportTo(fragment.transform.position + new Vector3(3f, 1f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragment.transform.position);
            for (int i = 0; i < 30; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Assert.IsTrue(progress.HasSkill(EmotionId.Foresight), "파편을 먹었는데 예지가 해금되지 않았다.");

            yield return null;
            var active = EmotionSkillController.Instance.Active;
            Assert.IsNotNull(active, "예지가 활성 스킬로 잡히지 않았다.");
            Assert.AreEqual(EmotionId.Foresight, active.Id, "활성 스킬이 예지가 아니다.");
        }

        // 균열의 붕괴 발판이 실제로 되살아나는지. respawnDelay 값만 보는 것과
        // 진짜로 콜라이더가 돌아오는 것은 다르다.
        [UnityTest]
        public IEnumerator 균열_붕괴_발판이_밟은_뒤_실제로_되살아난다()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Fracture_Full", LoadSceneMode.Single);
            yield return null;
            SilenceCombat();

            var player = PlayerController.Instance;

            // F02 상부 길의 발판을 쓴다. 이 지역이 처음으로 "안전해 보이는 것이 무너진다"를
            // 가르치는 자리이고, 주변에 다른 지형이 없어 결과가 흔들리지 않는다.
            var room = FindRoom("FractureRoom02");
            Assert.IsNotNull(room, "FractureRoom02가 없다.");

            CrumblingPlatform platform = null;
            foreach (var candidate in Object.FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None))
                if (room.WorldBounds.Contains(new Vector3(candidate.transform.position.x,
                                                          candidate.transform.position.y, 0f)))
                { platform = candidate; break; }
            Assert.IsNotNull(platform, "F02에 붕괴 발판이 없다.");

            // 발판 바로 위에 떨어뜨린다.
            player.TeleportTo(platform.transform.position + new Vector3(0f, 2f, 0f));

            int crumbleStep = -1;
            for (int i = 0; i < 200 && crumbleStep < 0; i++)
            {
                PlayerInput.Injected = default;
                yield return new WaitForFixedUpdate();
                if (platform.HasCrumbled) crumbleStep = i;
            }

            Debug.Log("===== 붕괴 발판 ===== " + platform.name + " @ " + platform.transform.position.ToString("F1")
                + " 플레이어=" + player.transform.position.ToString("F1")
                + " 무너진 스텝=" + crumbleStep + " respawnDelay=" + platform.RespawnDelay);

            Assert.Greater(crumbleStep, -1, "발판을 밟았는데 4초가 지나도 무너지지 않았다.");

            // respawnDelay(3초) 뒤 복구되어야 한다.
            int respawnStep = -1;
            for (int i = 0; i < 300 && respawnStep < 0; i++)
            {
                PlayerInput.Injected = default;
                yield return new WaitForFixedUpdate();
                if (!platform.HasCrumbled) respawnStep = i;
            }

            Debug.Log("===== 붕괴 발판 복구 ===== 복구된 스텝=" + respawnStep
                + " (respawnDelay=" + platform.RespawnDelay + "초)");

            Assert.Greater(respawnStep, -1,
                "붕괴 발판이 " + platform.RespawnDelay + "초가 지나도 되살아나지 않는다 — 되감기가 없는 지역이라 진행 불가가 된다.");
        }
    }
}
