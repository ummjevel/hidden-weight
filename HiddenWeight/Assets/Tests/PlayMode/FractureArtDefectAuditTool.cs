using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HiddenWeight.Tests
{
    // 화면에서 "이상하게 보이는 것"의 정체를 이름으로 특정한다.
    //
    // 스크린샷으로는 "세로로 반복되는 띠가 있다"까지만 알 수 있고, 그게 어느 오브젝트인지
    // 무슨 스프라이트인지는 알 수 없다. 늘어난 정도(원본 대비 배율)를 재면 눈으로 보이는
    // 뭉개짐·반복이 어디서 오는지 바로 나온다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.FractureArtDefectAuditTool"
    [Explicit]
    public class FractureArtDefectAuditTool
    {
        static readonly string[] Rooms =
            { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08", "F09", "F10", "F11", "F12",
              "FS1", "FS2", "FS3" };

        // 가로·세로 배율이 이만큼 벌어지면 그림이 눌리거나 늘어난 것으로 읽힌다.
        const float StretchRatio = 2.5f;

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [UnityTest]
        public IEnumerator 균열_늘어난_그림을_찾는다()
        {
            var report = new StringBuilder("[균열 스프라이트 왜곡 감사]\n");
            var totals = new Dictionary<string, int>();

            foreach (var room in Rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();
                for (int frame = 0; frame < 6; frame++) yield return null;

                var found = new List<string>();
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
                {
                    if (!sr.enabled || sr.sprite == null) continue;

                    var native = sr.sprite.bounds.size;
                    if (native.x <= 0f || native.y <= 0f) continue;

                    var world = sr.bounds.size;
                    float scaleX = world.x / native.x;
                    float scaleY = world.y / native.y;
                    if (scaleX <= 0f || scaleY <= 0f) continue;

                    float ratio = Mathf.Max(scaleX / scaleY, scaleY / scaleX);
                    if (ratio < StretchRatio) continue;

                    found.Add($"    {Path(sr.transform)}  [{sr.sprite.name}]  "
                              + $"배율 {scaleX:F2}x{scaleY:F2} (비 {ratio:F1}) "
                              + $"화면크기 {world.x:F1}x{world.y:F1} 정렬{sr.sortingOrder}");

                    totals.TryGetValue(sr.sprite.name, out int count);
                    totals[sr.sprite.name] = count + 1;
                }

                report.AppendLine($"  ── {room}: 왜곡 {found.Count}개");
                foreach (var line in found) report.AppendLine(line);
            }

            report.AppendLine("  ── 스프라이트별 합계");
            foreach (var pair in totals)
                report.AppendLine($"    {pair.Key}: {pair.Value}회");

            Debug.Log(report.ToString());
        }

        // 한 방의 화면 구성을 통째로 적는다. 왜곡 감사는 "비율이 어긋난 것"만 잡으므로,
        // 비율은 멀쩡한데 자리나 크기가 이상한 것(예: 화면 전체 높이로 서 있는 기둥)은
        // 걸리지 않는다. 그런 건 목록을 눈으로 훑는 편이 빠르다.
        [UnityTest]
        public IEnumerator 한_방의_화면_구성을_적는다()
        {
            string room = System.Environment.GetEnvironmentVariable("HW_ROOM") ?? "F02";
            yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();
            for (int frame = 0; frame < 6; frame++) yield return null;

            var rows = new List<(float x, string line)>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
            {
                if (!sr.enabled || sr.sprite == null) continue;
                var b = sr.bounds;
                rows.Add((b.min.x,
                    $"    x[{b.min.x,7:F1}~{b.max.x,7:F1}] y[{b.min.y,7:F1}~{b.max.y,7:F1}] "
                    + $"크기 {b.size.x,6:F1}x{b.size.y,6:F1} 정렬{sr.sortingOrder,4}  "
                    + $"{sr.sprite.name}  ←  {Path(sr.transform)}"));
            }
            rows.Sort((a, b) => a.x.CompareTo(b.x));

            var report = new StringBuilder($"[{room} 화면 구성 {rows.Count}개]\n");
            foreach (var row in rows) report.AppendLine(row.line);
            Debug.Log(report.ToString());
        }

        static string Path(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
