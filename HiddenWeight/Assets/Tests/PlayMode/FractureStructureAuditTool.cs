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
    // 균열의 다리·바닥·발판 같은 **구조물**을 한 방씩 훑는다.
    //
    // 배경은 그림 한 장이라 눈으로 보면 되지만, 구조물은 "그림"과 "밟히는 판정"이 서로 다른
    // 물건이다. 둘이 어긋나면 화면에서는 멀쩡한데 발이 빠지거나, 아무것도 없는 곳에서 막힌다.
    // 그림만 있고 판정이 없는 것, 판정만 있고 그림이 없는 것, 서로 크기가 다른 것,
    // 겹쳐서 두 번 놓인 것, 방 밖으로 나간 것을 전부 이름으로 뽑는다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.FractureStructureAuditTool"
    [Explicit]
    public class FractureStructureAuditTool
    {
        static readonly string[] Rooms =
            { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08", "F09", "F10", "F11", "F12",
              "FS1", "FS2", "FS3" };

        // 실측치(PlaythroughTests): 최대 점프 2.72, 달리기 점프 도달 7.56.
        const float JumpHeight = 2.72f;
        const float JumpReach = 7.6f;

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [UnityTest]
        public IEnumerator 균열_구조물을_전부_점검한다()
        {
            var report = new StringBuilder("[균열 구조물 감사]\n");
            var totals = new Dictionary<string, int>();
            int groundLayer = LayerMask.NameToLayer("Ground");
            int wallLayer = LayerMask.NameToLayer("Wall");

            foreach (var room in Rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();
                for (int frame = 0; frame < 6; frame++) yield return null;

                var anchor = Object.FindAnyObjectByType<Room>();
                var findings = new List<string>();

                // ── 밟히는 면을 먼저 모은다. 닿을 수 있는지 판단할 기준이 된다.
                var surfaces = new List<Bounds>();
                foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
                {
                    if (col.isTrigger || col.gameObject.layer != groundLayer) continue;
                    if (col.name.Contains("Boundary") || col.name.Contains("RoomEdge")) continue;
                    surfaces.Add(col.bounds);
                }

                // ── 1. 그림은 있는데 밟히지 않는 발판 ────────────────────────────
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
                {
                    if (!sr.enabled || sr.sprite == null) continue;
                    if (sr.sortingOrder < 0) continue;              // 배경·장식은 대상이 아니다
                    // 예지로 드러나는 미래의 물건은 평소 알파 0이 정상이다.
                    if (sr.name.Contains("FutureVisual") || sr.name.Contains("Preview")) continue;
                    if (sr.color.a < 0.08f)
                    {
                        Add(findings, totals, "거의 투명한 그림",
                            $"{Path(sr.transform)} [{sr.sprite.name}] a={sr.color.a:F2}");
                        continue;
                    }

                    var b = sr.bounds;
                    if (b.size.x <= 0.01f || b.size.y <= 0.01f)
                    {
                        Add(findings, totals, "크기 0인 그림", $"{Path(sr.transform)} {b.size:F2}");
                        continue;
                    }

                    // 넓고 납작하면 발판으로 읽힌다. 그런데 판정이 없으면 밟으려다 빠진다.
                    bool looksWalkable = b.size.x >= 1.6f && b.size.y <= 1.2f
                                         && b.size.x / Mathf.Max(0.01f, b.size.y) >= 2.5f;
                    if (!looksWalkable) continue;

                    var owner = sr.GetComponentInParent<Collider2D>();
                    if (owner == null)
                        Add(findings, totals, "발판처럼 보이는데 판정 없음",
                            $"{Path(sr.transform)} [{sr.sprite.name}] 크기 {b.size.x:F1}x{b.size.y:F1} @({b.center.x:F1},{b.center.y:F1})");
                }

                // ── 2·3·4. 콜라이더 쪽에서 본 문제 ──────────────────────────────
                var solids = new List<Collider2D>();
                foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
                {
                    if (col.isTrigger) continue;
                    if (col.gameObject.layer != groundLayer && col.gameObject.layer != wallLayer) continue;
                    if (col.name.Contains("Boundary") || col.name.Contains("RoomEdge")) continue;
                    if (col.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() == null
                        && !(col is CompositeCollider2D))
                        solids.Add(col);

                    var cb = col.bounds;

                    // 방 밖으로 나간 구조물.
                    if (anchor != null)
                    {
                        var rb = anchor.WorldBounds;
                        if (cb.max.x < rb.min.x - 1f || cb.min.x > rb.max.x + 1f
                            || cb.max.y < rb.min.y - 2f || cb.min.y > rb.max.y + 2f)
                            Add(findings, totals, "방 밖에 있는 구조물",
                                $"{Path(col.transform)} @({cb.center.x:F1},{cb.center.y:F1}) 방 {rb.min.x:F0}~{rb.max.x:F0}");
                    }

                    // 타일맵은 방 전체를 덮는 한 덩어리라 어떤 비교에도 의미가 없다.
                    if (col.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null
                        || col is CompositeCollider2D) continue;

                    // 막는데 아무것도 안 보인다.
                    if (!TryVisibleArtBounds(col, out var ab))
                    {
                        Add(findings, totals, "안 보이는데 막는 구조물",
                            $"{Path(col.transform)} 크기 {cb.size.x:F1}x{cb.size.y:F1} @({cb.center.x:F1},{cb.center.y:F1})");
                        continue;
                    }

                    // 그림이 판정을 못 덮는다 — 벽인데 일부만 보이거나, 발판인데 끝이 허공이다.
                    // 그림이 판정보다 **큰** 것은 문제로 보지 않는다. 발판 아래 장식(고드름·
                    // 보석 술)이 판정 밖으로 내려오는 것은 이 게임의 정상적인 생김새다.
                    float missingX = (cb.size.x - ab.size.x) / Mathf.Max(0.01f, cb.size.x);
                    float missingY = (cb.size.y - ab.size.y) / Mathf.Max(0.01f, cb.size.y);
                    if (missingX > 0.35f || missingY > 0.35f)
                        Add(findings, totals, "그림이 판정을 못 덮음",
                            $"{Path(col.transform)} 그림 {ab.size.x:F1}x{ab.size.y:F1} vs 판정 {cb.size.x:F1}x{cb.size.y:F1}");

                    // 밟는 면의 높이가 어긋나면 발이 그림 속에 파묻히거나 위에 뜬다.
                    if (col.gameObject.layer == groundLayer
                        && Mathf.Abs(ab.max.y - cb.max.y) > 0.25f)
                        Add(findings, totals, "밟는 면과 그림 윗면이 어긋남",
                            $"{Path(col.transform)} 그림 윗면 {ab.max.y:F2} vs 밟는 면 {cb.max.y:F2}");
                }

                // ── 5. 같은 자리에 두 번 놓인 것 ────────────────────────────────
                for (int i = 0; i < solids.Count; i++)
                    for (int j = i + 1; j < solids.Count; j++)
                    {
                        var a = solids[i].bounds;
                        var b = solids[j].bounds;
                        if (!a.Intersects(b)) continue;
                        float overlap = OverlapRatio(a, b);
                        if (overlap < 0.8f) continue;
                        Add(findings, totals, "같은 자리에 겹쳐 놓임",
                            $"{Path(solids[i].transform)} ↔ {Path(solids[j].transform)} 겹침 {overlap:P0}");
                    }

                // ── 6. 닿을 수 없는 높이에 뜬 발판 ──────────────────────────────
                foreach (var col in solids)
                {
                    if (col.gameObject.layer != groundLayer) continue;
                    // 지름길 차단막은 밟으라고 둔 것이 아니고, 움직이는 발판은 한 시점의
                    // 위치로 닿는지 판정할 수 없다.
                    if (col.name.Contains("Blocker")) continue;
                    if (col.GetComponentInParent<MovingPlatform>() != null) continue;
                    var cb = col.bounds;

                    bool reachable = false;
                    foreach (var other in surfaces)
                    {
                        if (other.center == cb.center) continue;
                        float gap = cb.max.y - other.max.y;
                        if (gap > JumpHeight + 0.4f) continue;              // 너무 높다
                        float horizontal = Mathf.Max(0f,
                            Mathf.Max(other.min.x - cb.max.x, cb.min.x - other.max.x));
                        if (horizontal > JumpReach) continue;              // 너무 멀다
                        reachable = true;
                        break;
                    }
                    if (!reachable && surfaces.Count > 1)
                        Add(findings, totals, "닿을 수 없는 발판",
                            $"{Path(col.transform)} 윗면 y={cb.max.y:F1} @x {cb.min.x:F1}~{cb.max.x:F1}");
                }

                report.AppendLine($"  ── {room}: {findings.Count}건");
                foreach (var line in findings) report.AppendLine(line);
            }

            report.AppendLine("  ── 종류별 합계");
            foreach (var pair in totals) report.AppendLine($"    {pair.Key}: {pair.Value}건");
            Debug.Log(report.ToString());
        }

        // 방 안의 모든 발 디딜 면을 모아, 어느 면에서도 올라갈 수 없는 면을 뽑는다.
        //
        // 위의 구조물 감사는 **독립 콜라이더**만 본다. 그런데 균열의 층·선반·관찰대는
        // 대부분 타일맵으로 깔려 있어서 그 목록에 들어오지 않는다 — 방 위쪽에 넓은 층이
        // 있는데 거기까지 닿는 길이 없어도 감사는 0건이었다. 화면에는 분명히 올라갈 곳이
        // 보이는데 올라가는 방법이 없다는 보고가 여기서 나온다.
        //
        // 타일맵은 "위가 빈 타일"의 가로 연속 구간을 한 면으로 센다. 런타임 지형 아트가
        // 바닥 윗면을 그리는 규칙과 같으므로, 여기서 세는 면은 화면에서 실제로 밟는 면으로
        // 보이는 것과 정확히 일치한다.
        [UnityTest]
        public IEnumerator 올라갈_수_없는_층을_찾는다()
        {
            var report = new StringBuilder("[균열 수직 동선 감사]\n");
            int total = 0;

            foreach (var room in Rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();
                for (int frame = 0; frame < 6; frame++) yield return null;

                var ledges = CollectLedges();

                // 시작 지점이 서 있는 면에서 **퍼져 나가며** 센다.
                //
                // 처음에는 면마다 "이웃 중에 올라올 수 있는 것이 하나라도 있는가"만 봤다.
                // 그러면 서로만 닿는 면 두 개가 서로를 근거로 통과한다 — 아무 데서도
                // 올라갈 수 없는 공중 발판 한 쌍이 "이상 없음"으로 나온다. 실제로 F06이
                // 그랬다. 시작 면에서 도달 가능한 집합을 넓혀 가며 판정해야 한다.
                var start = Object.FindAnyObjectByType<RoomStart>();
                Vector2 from0 = start != null
                    ? (Vector2)start.transform.position
                    : new Vector2(3f, 4f);

                var reached = new HashSet<Ledge>();
                var frontier = new Queue<Ledge>();
                foreach (var ledge in ledges)
                {
                    // 시작 지점 아래로 떨어지면 닿는 면들이 출발점이다.
                    if (ledge.top > from0.y + 0.6f) continue;
                    if (from0.x < ledge.xMin - 1f || from0.x > ledge.xMax + 1f) continue;
                    Reach(ledge, ledges, reached, frontier);
                }
                // 시작 지점 바로 아래에 아무것도 없으면 전체에서 가장 낮은 면을 출발점으로 둔다.
                if (reached.Count == 0 && ledges.Count > 0)
                {
                    var lowest = ledges[0];
                    foreach (var ledge in ledges) if (ledge.top < lowest.top) lowest = ledge;
                    Reach(lowest, ledges, reached, frontier);
                }

                while (frontier.Count > 0)
                {
                    var here = frontier.Dequeue();
                    foreach (var next in ledges)
                    {
                        if (reached.Contains(next)) continue;
                        float rise = next.top - here.top;
                        if (rise > JumpHeight + 0.4f) continue;      // 너무 높다
                        float across = Mathf.Max(0f,
                            Mathf.Max(here.xMin - next.xMax, next.xMin - here.xMax));
                        // 아래로 내려가는 것은 떨어지면 되므로 가로 거리만 본다.
                        if (across > JumpReach) continue;
                        Reach(next, ledges, reached, frontier);
                    }
                }

                // 움직이는 발판은 자리 하나하나가 아니라 한 덩어리로 셈한다. 한 자리라도
                // 올라탈 수 있으면 그 발판이 지나는 모든 자리를 쓸 수 있기 때문이다.
                var unreachable = new List<string>();
                var reportedOwners = new HashSet<string>();
                foreach (var ledge in ledges)
                {
                    if (reached.Contains(ledge)) continue;
                    if (ledge.owner != null && !reportedOwners.Add(ledge.owner)) continue;
                    unreachable.Add($"    [올라갈 길 없음] {ledge.name} 윗면 y={ledge.top:F1} "
                                    + $"@x {ledge.xMin:F1}~{ledge.xMax:F1}");
                }

                total += unreachable.Count;
                report.AppendLine($"  ── {room}: 면 {ledges.Count}개 중 {unreachable.Count}건");
                foreach (var line in unreachable) report.AppendLine(line);
            }

            report.AppendLine($"  ── 합계 {total}건");
            Debug.Log(report.ToString());
        }

        sealed class Ledge
        {
            public string name;
            public float top, xMin, xMax;

            // 움직이는 발판이 지나는 자리들을 하나로 묶는 이름. 고정 지형은 null이다.
            public string owner;
        }

        // 면 하나에 닿았다고 표시한다. 움직이는 발판의 한 자리에 올라탔다면 그 발판이
        // 지나는 나머지 자리도 함께 쓸 수 있게 된다 — 발판이 데려다주기 때문이다.
        static void Reach(Ledge ledge, List<Ledge> all, HashSet<Ledge> reached, Queue<Ledge> frontier)
        {
            if (!reached.Add(ledge)) return;
            frontier.Enqueue(ledge);
            if (ledge.owner == null) return;

            foreach (var sibling in all)
                if (sibling.owner == ledge.owner && reached.Add(sibling))
                    frontier.Enqueue(sibling);
        }

        static List<Ledge> CollectLedges()
        {
            var ledges = new List<Ledge>();
            int ground = LayerMask.NameToLayer("Ground");

            foreach (var tilemap in Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var bounds = tilemap.cellBounds;
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    int runStart = int.MinValue;
                    for (int x = bounds.xMin; x <= bounds.xMax; x++)
                    {
                        bool surface = x < bounds.xMax
                            && tilemap.HasTile(new Vector3Int(x, y, 0))
                            && !tilemap.HasTile(new Vector3Int(x, y + 1, 0));

                        if (surface && runStart == int.MinValue) runStart = x;
                        else if (!surface && runStart != int.MinValue)
                        {
                            var left = tilemap.CellToWorld(new Vector3Int(runStart, y + 1, 0));
                            var right = tilemap.CellToWorld(new Vector3Int(x, y + 1, 0));
                            ledges.Add(new Ledge
                            {
                                name = $"지형 {runStart}~{x}",
                                top = left.y, xMin = left.x, xMax = right.x,
                            });
                            runStart = int.MinValue;
                        }
                    }
                }
            }

            foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
            {
                if (col.isTrigger || col.gameObject.layer != ground) continue;
                if (col.GetComponentInParent<UnityEngine.Tilemaps.Tilemap>() != null) continue;
                if (col.name.Contains("Boundary") || col.name.Contains("RoomEdge")) continue;
                if (col.name.Contains("Blocker") || col.name.Contains("Lock_")) continue;

                var b = col.bounds;

                // 승강기는 올라탄 뒤에만 움직이므로 미래 위치를 물어봐도 제자리라고 답한다.
                // 경로(웨이포인트)를 직접 훑어 지나갈 자리를 면으로 센다.
                var lift = col.GetComponentInParent<LiftPlatform>();
                if (lift != null)
                {
                    string liftOwner = col.name + col.transform.position;
                    Vector2 leg = Vector2.zero;
                    ledges.Add(new Ledge { name = col.name + " (승강 시작)", top = b.max.y,
                                           xMin = b.min.x, xMax = b.max.x, owner = liftOwner });
                    foreach (var waypoint in lift.Waypoints)
                    {
                        // 다리 하나를 잘게 나눠 중간에 내리는 층도 셈에 넣는다.
                        for (float t = 0f; t <= 1f; t += 0.05f)
                        {
                            Vector2 at = Vector2.Lerp(leg, waypoint, t);
                            ledges.Add(new Ledge
                            {
                                name = $"{col.name} (승강 {at.y:F1})",
                                top = b.max.y + at.y,
                                xMin = b.min.x + at.x, xMax = b.max.x + at.x,
                                owner = liftOwner,
                            });
                        }
                        leg = waypoint;
                    }
                    continue;
                }

                // 움직이는 발판은 한 시점의 자리로 판단할 수 없다. 미래 위치를 물어보는
                // 인터페이스가 이미 있으므로(예지가 쓰는 것과 같은 함수) 한 주기 넘게
                // 훑어 지나가는 자리를 전부 면으로 센다.
                IForeseeable mover = col.GetComponentInParent<MovingPlatform>();
                if (mover == null) mover = col.GetComponentInParent<OrbitPlatform>();
                if (mover != null)
                {
                    var here = col.transform.position;
                    for (float lead = 0f; lead <= 15f; lead += 0.25f)
                    {
                        var at = mover.PredictPosition(lead) - here;
                        ledges.Add(new Ledge
                        {
                            name = $"{col.name} (+{lead:F1}초)",
                            top = b.max.y + at.y,
                            xMin = b.min.x + at.x, xMax = b.max.x + at.x,
                            owner = col.name + col.transform.position,
                        });
                    }
                    continue;
                }

                ledges.Add(new Ledge { name = col.name, top = b.max.y, xMin = b.min.x, xMax = b.max.x });
            }

            return ledges;
        }

        // 이 콜라이더가 화면에 보여 주는 그림의 **전체** 범위.
        //
        // 처음에는 자식 중 가장 큰 렌더러 하나만 봤다. 그런데 이 프로젝트의 벽·발판 아트는
        // 조각을 여러 장 이어 붙여 만든다(ApplyBlockArt·TiledPiece). 조각 하나만 재면
        // 10유닛짜리 벽이 "그림은 1.3뿐"으로 잡혀, 멀쩡한 벽 수십 개가 결함으로 나온다.
        static bool TryVisibleArtBounds(Collider2D col, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var sr in col.GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (!sr.enabled || sr.sprite == null || sr.color.a < 0.08f) continue;
                if (!any) { bounds = sr.bounds; any = true; }
                else bounds.Encapsulate(sr.bounds);
            }
            return any;
        }

        static float OverlapRatio(Bounds a, Bounds b)
        {
            float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float y = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
            if (x <= 0f || y <= 0f) return 0f;
            float smaller = Mathf.Min(a.size.x * a.size.y, b.size.x * b.size.y);
            return smaller <= 0f ? 0f : (x * y) / smaller;
        }

        static void Add(List<string> findings, Dictionary<string, int> totals,
                        string kind, string detail)
        {
            findings.Add($"    [{kind}] {detail}");
            totals.TryGetValue(kind, out int count);
            totals[kind] = count + 1;
        }

        static string Path(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
