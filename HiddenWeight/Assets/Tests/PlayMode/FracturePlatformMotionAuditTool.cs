using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 움직이는 발판이 **실제로 제자리로 돌아오는지** 방마다 재 본다.
    //
    // 코드만 보면 MovingPlatform은 PingPong이라 반드시 왕복한다. 그런데 화면에서는
    // "한 번 가더니 안 돌아온다"가 나온다 — 그 차이는 돌려 봐야만 보인다. 한 주기보다
    // 길게 돌리며 위치를 모아, 시작점으로 돌아오는지와 실제 왕복 폭을 적는다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.FracturePlatformMotionAuditTool"
    [Explicit]
    public class FracturePlatformMotionAuditTool
    {
        static readonly string[] Rooms =
            { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08", "F09", "F10", "F11", "F12",
              "FS1", "FS2", "FS3" };

        const float Seconds = 20f;

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [UnityTest]
        public IEnumerator 움직이는_발판이_제자리로_돌아오는지_본다()
        {
            var report = new StringBuilder("[균열 발판 왕복 감사]\n");

            foreach (var room in Rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();

                var tracked = new List<(Transform t, string kind, Vector3 start,
                                        Vector3 min, Vector3 max, Vector3 last)>();
                foreach (var mover in Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude))
                    tracked.Add((mover.transform, "왕복", mover.transform.position,
                                 mover.transform.position, mover.transform.position, mover.transform.position));
                foreach (var orbit in Object.FindObjectsByType<OrbitPlatform>(FindObjectsInactive.Exclude))
                    tracked.Add((orbit.transform, "회전", orbit.transform.position,
                                 orbit.transform.position, orbit.transform.position, orbit.transform.position));

                if (tracked.Count == 0) { report.AppendLine($"  ── {room}: 없음"); continue; }

                float returned = 0f;
                var backAt = new float[tracked.Count];
                for (int i = 0; i < backAt.Length; i++) backAt[i] = -1f;

                for (float elapsed = 0f; elapsed < Seconds; elapsed += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    for (int i = 0; i < tracked.Count; i++)
                    {
                        var e = tracked[i];
                        if (e.t == null) continue;
                        var p = e.t.position;
                        tracked[i] = (e.t, e.kind, e.start,
                            Vector3.Min(e.min, p), Vector3.Max(e.max, p), p);

                        // 한 번 벗어난 뒤 시작점 근처로 되돌아온 첫 시각.
                        if (backAt[i] < 0f && (e.max - e.min).magnitude > 0.5f
                            && (p - e.start).magnitude < 0.2f)
                            backAt[i] = elapsed;
                    }
                    returned = elapsed;
                }

                report.AppendLine($"  ── {room}: {tracked.Count}개");
                for (int i = 0; i < tracked.Count; i++)
                {
                    var e = tracked[i];
                    var span = e.max - e.min;
                    string verdict = span.magnitude < 0.2f
                        ? "★ 아예 움직이지 않는다"
                        : backAt[i] < 0f
                            ? $"★ {Seconds:F0}초 안에 시작점으로 돌아오지 않는다 (지금 {e.last - e.start:F2} 떨어져 있음)"
                            : $"{backAt[i]:F1}초에 복귀";
                    report.AppendLine($"    {e.kind} {e.t.name} 시작({e.start.x:F1},{e.start.y:F1}) "
                                      + $"왕복폭 {span.x:F1}x{span.y:F1}  {verdict}");
                }
            }

            Debug.Log(report.ToString());
        }
    }
}
