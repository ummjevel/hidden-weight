using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 제작·회귀 검사용으로 남겨 둔 균열의 분리 방 씬(포탈 셸용)을 봇이 걸어 본다.
    //
    // 기존 `GazeFracturePlaythroughTests.균열_봇이_열두_방을_모두_통과한다`는 통로판
    // 정식 배포 경로는 다시 `Zone_Fracture_Full` 연속 맵을 쓴다. 분리 방 씬은 정식 동선은
    // 아니지만 제작 도구가 계속 생성하므로, 포탈 링크가 조용히 깨지지 않았는지만 확인한다.
    //
    // 손으로 F12까지 몰고 가는 것은 이동 발판 타이밍·정예·중간 보스 때문에 사실상 불가능하다.
    // 봇은 그 대신 "각 방에서 출구까지 갈 수 있는가"만 본다 — 진행 불가(소프트락)를 잡는 것이
    // 목적이고, 전투 실력을 재는 것이 아니다.
    public class FracturePortalRouteTests
    {
        // 방 하나에 허용하는 시간. 실측 이동 속도로 30유닛 방을 건너기에 충분하다.
        const float RoomSeconds = 22f;

        // 이만큼 나아가지 못하고 이 시간이 지나면 막힌 것으로 보고 점프를 시도한다.
        const float StuckSeconds = 0.45f;
        const float StuckDistance = 0.25f;

        // 걷기만으로는 넘을 수 없는, 설계가 의도한 관문. 여기서 멈추는 것은 결함이 아니다.
        //   F08 역행 승강축 — 출구가 (22,26) 승강기 선반 위라 승강기를 타야 한다(설계 4.8)
        //   F09 거울 가능성실 — 조우 잠금벽(fracture_f09_main)이 적 처치를 요구한다(설계 4.9)
        // 이 둘을 제외한 나머지 구간이 걸어서 통과되는지가 이 검사의 판정 대상이다.
        static readonly HashSet<string> IntendedGates = new HashSet<string> { "F08", "F09" };

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator 봇이_포탈_경로로_F01에서_보스방까지_간다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F01");
            Time.timeScale = 1f;

            // 정식 진입 경로는 연속형 _Full 씬이어야 한다. 아래 포탈 문 검사는 제작용으로
            // 남은 분리 방 씬의 링크가 유효한지만 보는 별도 회귀 검사다.
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            Assert.IsNotNull(zone, "균열 지역 데이터가 잡히지 않았다.");
            Assert.AreEqual("Zone_Fracture_Full", zone.sceneName,
                "균열의 정식 씬이 연속형 전체 맵이 아니다.");
            Assert.IsNotEmpty(Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Exclude),
                "첫 방에 포탈 문이 없다.");

            var blocked = new List<string>();
            var report = new StringBuilder("===== 균열 포탈 경로 봇 주파 =====\n");

            for (int i = 1; i < 12; i++)
            {
                string from = $"F{i:00}";
                string to = $"F{i + 1:00}";
                string doorId = $"fracture_{from}_{to}:E";

                // 이 방에서 실제로 걸어 본다. 문에 닿으면 RoomDoor가 스스로 전환을 건다.
                float startX = PlayerController.Instance.transform.position.x;
                float bestX = startX;
                bool arrived = false;

                float elapsed = 0f, stuckTimer = 0f, anchorX = startX;
                // 점프는 한 물리 스텝만 눌러서는 턱을 못 넘는다(가변 점프라 누른 시간이
                // 곧 높이다). 막혔다고 판단하면 몇 스텝 동안 눌러 준다.
                int jumpFrames = 0;
                while (elapsed < RoomSeconds)
                {
                    if (RoomLoader.Instance.IsTransitioning)
                    {
                        while (RoomLoader.Instance.IsTransitioning) yield return null;
                        arrived = RoomLoader.Instance.CurrentRoom == to;
                        break;
                    }

                    var player = PlayerController.Instance;
                    if (player == null) break;

                    float x = player.transform.position.x;
                    bestX = Mathf.Max(bestX, x);

                    stuckTimer += Time.fixedDeltaTime;
                    if (stuckTimer >= StuckSeconds)
                    {
                        if (Mathf.Abs(x - anchorX) < StuckDistance) jumpFrames = 9;
                        stuckTimer = 0f;
                        anchorX = x;
                    }
                    bool jumping = jumpFrames > 0;
                    if (jumping) jumpFrames--;

                    // 막히면 뛰어넘어 본다 — 턱과 낮은 발판은 그것으로 대부분 넘어간다.
                    PlayerInput.Injected = new PlayerInput.Frame
                    {
                        horizontal = 1f,
                        run = true,
                        jumpPressed = jumping && jumpFrames == 8,
                        jumpHeld = jumping,
                    };

                    elapsed += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }
                PlayerInput.Injected = null;

                if (arrived)
                {
                    report.AppendLine($"  {from} → {to}   통과 (x {startX:F1} → 문)");
                    continue;
                }

                if (IntendedGates.Contains(from))
                    report.AppendLine($"  {from} → {to}   관문(승강기·전투) — 걷기로는 통과 불가, 의도됨");
                else
                {
                    blocked.Add($"{from}(x {bestX:F1})");
                    report.AppendLine($"  {from} → {to}   ← 막힘. 최대 도달 x={bestX:F1}, "
                                      + $"현재 방={RoomLoader.Instance.CurrentRoom}");
                }

                // 비밀방 입구(FS1 등)로 떨어지면 다른 방에 서 있게 된다. 그건 결함이 아니라
                // 설계된 갈림길이므로, 원래 방으로 되돌린 뒤 나머지를 계속 감사한다.
                if (RoomLoader.Instance.CurrentRoom != from)
                {
                    report.AppendLine($"    (봇이 {RoomLoader.Instance.CurrentRoom}로 빠져 {from}으로 되돌린다)");
                    yield return RoomLoader.Instance.LoadRoom(from, null);
                    while (RoomLoader.Instance.IsTransitioning) yield return null;
                }

                // 막혀도 나머지 방을 계속 감사한다 — 한 곳에서 멈추면 뒤쪽 문제를 못 본다.
                var door = FindDoor(doorId);
                if (door == null)
                {
                    report.AppendLine($"    (문 {doorId} 자체가 없어 이후 감사를 멈춘다)");
                    break;
                }
                RoomLoader.Instance.RequestTransition(door);
                while (RoomLoader.Instance.IsTransitioning) yield return null;
            }

            report.AppendLine($"  최종 방: {RoomLoader.Instance.CurrentRoom}");
            Debug.Log(report.ToString());

            Assert.IsEmpty(blocked,
                "포탈 경로에서 봇이 통과하지 못한 방이 있다: " + string.Join(", ", blocked)
                + "\n" + report);
        }

        static RoomDoor FindDoor(string doorId)
        {
            foreach (var door in Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include))
                if (door.DoorId == doorId) return door;
            return null;
        }
    }
}
