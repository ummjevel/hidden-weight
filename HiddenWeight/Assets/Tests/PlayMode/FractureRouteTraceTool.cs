using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 봇이 한 방에서 실제로 어디를 밟고 지나가는지 좌표로 적는다.
    //
    // "F04에서 막힌다"까지는 경로 검사가 알려 주지만, 어느 발판을 놓쳐서 그렇게 됐는지는
    // 알려 주지 않는다. 0.1초마다 위치와 접지 여부를 남기면 바로 보인다.
    //
    // 실행:
    //   HW_ROOM=F04 Unity -batchmode -runTests -testPlatform PlayMode \
    //     -testFilter "HiddenWeight.Tests.FractureRouteTraceTool"
    [Explicit]
    public class FractureRouteTraceTool
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 한_방을_걸어_본_자취를_적는다()
        {
            string room = System.Environment.GetEnvironmentVariable("HW_ROOM") ?? "F04";
            string door = System.Environment.GetEnvironmentVariable("HW_DOOR");

            yield return RoomTestHarness.EnterRoom("Fracture", "F01");
            Time.timeScale = 1f;

            yield return RoomLoader.Instance.LoadRoom(room, door);
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            var report = new StringBuilder($"[{room} 자취]\n");
            float sample = 0f;
            for (float elapsed = 0f; elapsed < 14f; elapsed += Time.fixedDeltaTime)
            {
                var player = PlayerController.Instance;
                if (player == null) { report.AppendLine("    플레이어가 사라졌다"); break; }

                PlayerInput.Injected = new PlayerInput.Frame { horizontal = 1f, run = true };

                sample += Time.fixedDeltaTime;
                if (sample >= 0.1f)
                {
                    sample = 0f;
                    var p = player.transform.position;
                    report.AppendLine($"    t={elapsed,5:F1}  ({p.x,6:F2},{p.y,6:F2})  "
                                      + $"방={RoomLoader.Instance.CurrentRoom} "
                                      + (RoomLoader.Instance.IsTransitioning ? "전환중" : ""));
                }
                yield return new WaitForFixedUpdate();
            }
            PlayerInput.Injected = null;
            Debug.Log(report.ToString());
        }
    }
}
