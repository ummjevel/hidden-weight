using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 균열의 적이 실제로 어떻게 서 있고 어떻게 움직이는지 방마다 적어 둔다.
    //
    // 씬 YAML로는 "적이 몇 마리 있다"까지만 알 수 있다. 그 적이 바닥 위에 서 있는지,
    // 실제로 움직이는지, 걷는 그림이 붙어 있는지는 몇 초를 돌려 봐야 안다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.FractureEnemyAuditTool"
    [Explicit]
    public class FractureEnemyAuditTool
    {
        const float SimulateSeconds = 6f;

        static readonly string[] Rooms =
            { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08", "F09", "F10", "F11", "F12",
              "FS1", "FS2", "FS3" };

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            Time.timeScale = 1f;
        }

        sealed class Track
        {
            public Vector3 Spawn;
            public float SpawnGap = -1f;       // 스폰 순간 발밑까지의 빈 거리
            public float MinX, MaxX;
            public int Flips;                  // 좌우 방향이 뒤집힌 횟수
            public int MovingFrames, TotalFrames;
            public float MaxStep;              // 한 프레임에 튄 최대 거리
            public int LastDir;
            public string Clip = "없음";
        }

        [UnityTest]
        public IEnumerator 균열_적_배치와_행동을_적는다()
        {
            var report = new StringBuilder("[균열 적 감사]\n");
            int groundMask = LayerMask.GetMask("Ground");

            foreach (var room in Rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;

                var enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
                report.AppendLine($"  ── {room}: 적 {enemies.Length}마리");
                if (enemies.Length == 0) continue;

                // 물리가 돌기 전에 스폰 자리의 발밑을 잰다. 이걸 나중에 재면 이미 떨어진
                // 뒤라 "제자리에 서 있었다"로 보인다.
                Physics2D.SyncTransforms();
                var tracks = new Dictionary<Enemy, Track>();
                foreach (var enemy in enemies)
                {
                    if (enemy == null) continue;
                    var p = enemy.transform.position;
                    var track = new Track { Spawn = p, MinX = p.x, MaxX = p.x };

                    var body = enemy.GetComponent<Collider2D>();
                    float bottom = body != null ? body.bounds.min.y : p.y;
                    var hit = Physics2D.Raycast(new Vector2(p.x, bottom + 0.02f),
                                                Vector2.down, 60f, groundMask);
                    if (hit.collider != null) track.SpawnGap = bottom - hit.point.y;

                    tracks[enemy] = track;
                }

                float until = Time.time + SimulateSeconds;
                var lastX = new Dictionary<Enemy, float>();
                foreach (var pair in tracks) lastX[pair.Key] = pair.Value.Spawn.x;

                while (Time.time < until)
                {
                    yield return null;
                    foreach (var pair in tracks)
                    {
                        var enemy = pair.Key;
                        var track = pair.Value;
                        if (enemy == null) continue;

                        float x = enemy.transform.position.x;
                        float step = x - lastX[enemy];
                        lastX[enemy] = x;

                        track.TotalFrames++;
                        track.MinX = Mathf.Min(track.MinX, x);
                        track.MaxX = Mathf.Max(track.MaxX, x);
                        track.MaxStep = Mathf.Max(track.MaxStep, Mathf.Abs(step));

                        if (Mathf.Abs(step) > 0.0015f)
                        {
                            track.MovingFrames++;
                            int dir = step > 0f ? 1 : -1;
                            if (track.LastDir != 0 && dir != track.LastDir) track.Flips++;
                            track.LastDir = dir;
                        }

                        var animator = enemy.GetComponentInChildren<SpriteAnimator>();
                        if (animator != null && !string.IsNullOrEmpty(animator.CurrentClip))
                            track.Clip = animator.CurrentClip;
                    }
                }

                foreach (var pair in tracks)
                {
                    var enemy = pair.Key;
                    var track = pair.Value;
                    if (enemy == null) { report.AppendLine("    (감사 중 사라짐)"); continue; }

                    var problems = new List<string>();
                    if (enemy.Data == null) problems.Add("EnemyData 미할당");
                    if (track.SpawnGap > 0.15f)
                        problems.Add($"스폰이 {track.SpawnGap:F2} 떠 있음(놓자마자 떨어짐)");
                    if (track.SpawnGap < 0f) problems.Add("발밑에 지형 없음");

                    float span = track.MaxX - track.MinX;
                    var patrol = enemy.GetComponent<EnemyPatrol>();
                    if (patrol != null && span < 0.15f) problems.Add("순찰 컴포넌트가 있는데 안 움직임");

                    float movingRatio = track.TotalFrames > 0
                        ? (float)track.MovingFrames / track.TotalFrames : 0f;
                    // 움직이는 프레임이 드문드문하면 미끄러지듯 끊겨 보인다.
                    if (span > 0.15f && movingRatio < 0.85f)
                        problems.Add($"움직임이 끊김(이동 프레임 {movingRatio:P0})");
                    if (track.Flips > 12) problems.Add($"방향 뒤집힘 {track.Flips}회(떨림)");
                    if (track.Clip == "없음" && span > 0.15f) problems.Add("걷는데 애니메이션 클립 없음");

                    if (enemy.GetComponentInChildren<ContactDamage>(true) == null)
                        problems.Add("접촉 피해 없음");

                    string dataName = enemy.Data != null ? enemy.Data.name : "데이터 없음";
                    report.AppendLine(
                        $"    {enemy.name} [{dataName}] 체력{enemy.Health} "
                        + $"스폰({track.Spawn.x:F1},{track.Spawn.y:F1}) 발밑틈 {track.SpawnGap:F2} "
                        + $"순찰폭 {span:F1} 방향전환 {track.Flips} 클립 {track.Clip} "
                        + $"최대한프레임 {track.MaxStep:F3} | "
                        + (problems.Count == 0 ? "정상" : string.Join(", ", problems)));
                }
            }

            Debug.Log(report.ToString());
        }
    }
}
