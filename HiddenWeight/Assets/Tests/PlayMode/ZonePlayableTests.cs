using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // 지역 씬이 "사람이 눌러보지 않아도" 플레이 가능한 상태로 로드되는지 자동 검증한다.
    // 판정 항목: (1) 바닥 콜라이더가 실제로 생성되는가 (2) 플레이어가 그 위에 착지해 멈추는가
    // (3) 플레이어 스프라이트가 카메라 시야 안에 있는가 (4) 로드 중 에러/예외가 없는가.
    //
    // "시작하면 그냥 아래로 떨어진다"류의 증상은 (1)(2)에서 바로 드러난다 —
    // 낙하가 멈추지 않으면 settledY가 스폰보다 한참 아래로 찍히고 grounded=false로 남는다.
    public class ZonePlayableTests
    {
        static readonly string[] Zones = { "Zone_Prologue", "Zone_Residue", "Zone_Gaze", "Zone_Fracture" };

        // 착지 판정 허용 오차. 콜라이더 접촉 오프셋(0.01)과 조금의 파고듦을 감안한 값.
        const float LandingTolerance = 0.2f;

        readonly List<string> _logged = new List<string>();

        [SetUp]
        public void Hook()
        {
            // 에러를 만나면 즉시 실패시키지 않고 전부 모아 리포트에 싣는다 —
            // 첫 에러에서 끊기면 "어디까지 진행됐는지"를 볼 수 없다.
            LogAssert.ignoreFailingMessages = true;
            _logged.Clear();
            Application.logMessageReceived += Capture;
        }

        [TearDown]
        public void Unhook()
        {
            Application.logMessageReceived -= Capture;
        }

        void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _logged.Add(type + ": " + message);
        }

        [UnityTest]
        public IEnumerator 지역_씬이_플레이_가능한_상태로_로드된다([ValueSource(nameof(Zones))] string zone)
        {
            yield return SceneManager.LoadSceneAsync(zone, LoadSceneMode.Single);
            yield return null; // Awake/Start 한 바퀴

            var report = new StringBuilder();
            report.AppendLine("===== [" + zone + "] 플레이 가능성 진단 =====");

            var player = Object.FindFirstObjectByType<PlayerController>();
            var camera = Camera.main;
            var tilemap = Object.FindFirstObjectByType<Tilemap>();

            report.AppendLine("Player 존재: " + (player != null));
            report.AppendLine("MainCamera 존재: " + (camera != null));
            report.AppendLine("Tilemap 존재: " + (tilemap != null));

            Assert.IsNotNull(player, zone + ": 씬에 PlayerController가 없다.\n" + report);
            Assert.IsNotNull(camera, zone + ": 씬에 MainCamera(tag=MainCamera)가 없다.\n" + report);
            Assert.IsNotNull(tilemap, zone + ": 씬에 Tilemap이 없다.\n" + report);

            var spawn = player.transform.position;
            var body = player.GetComponent<Rigidbody2D>();
            // 루트 렌더러는 제거됐다. 실제로 그리는 것은 애니메이터가 가리키는 렌더러다.
            var animator = player.GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
            var sprite = animator != null && animator.Renderer != null
                ? animator.Renderer
                : player.GetComponentInChildren<SpriteRenderer>();
            report.AppendLine("스폰 위치: " + spawn.ToString("F3"));
            report.AppendLine("Rigidbody2D: bodyType=" + body.bodyType + " gravityScale=" + body.gravityScale);

            // --- 바닥 지오메트리: 타일맵 콜라이더가 런타임에 실제로 생성됐는지 ---
            var tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            var composite = tilemap.GetComponent<CompositeCollider2D>();
            report.AppendLine("타일 수: " + CountTiles(tilemap) + " / cellBounds=" + tilemap.cellBounds);
            report.AppendLine("TilemapCollider2D: " + (tilemapCollider == null ? "없음"
                : "enabled=" + tilemapCollider.enabled + " compositeOperation=" + tilemapCollider.compositeOperation));
            report.AppendLine("CompositeCollider2D: " + (composite == null ? "없음"
                : "enabled=" + composite.enabled + " pathCount=" + composite.pathCount
                  + " shapeCount=" + composite.shapeCount + " bounds=" + composite.bounds));

            // 스폰 지점에서 아래로 레이캐스트 — 밟을 바닥이 정말 있는지.
            int groundMask = LayerMask.GetMask("Ground");
            var down = Physics2D.Raycast(spawn, Vector2.down, 100f, groundMask);
            report.AppendLine("스폰에서 아래 레이캐스트(Ground): "
                + (down.collider == null ? "충돌 없음(바닥 없음!)"
                    : "hit=" + down.collider.name + " point=" + down.point.ToString("F3")
                      + " distance=" + down.distance.ToString("F3")));

            // --- 실제로 낙하가 멈추는지 2초간 시뮬레이션 ---
            float startY = player.transform.position.y;
            float minY = startY;
            for (float t = 0f; t < 2f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                minY = Mathf.Min(minY, player.transform.position.y);
            }

            var settled = player.transform.position;
            report.AppendLine("2초 후 위치: " + settled.ToString("F3")
                + " (낙하 최저점 y=" + minY.ToString("F3") + ")");
            report.AppendLine("속도: " + body.linearVelocity.ToString("F3"));
            report.AppendLine("IsGrounded: " + player.IsGrounded + " / 상태: " + player.State);

            // --- 스프라이트가 카메라 시야에 들어와 있는지(렌더 없이 기하로 판정) ---
            report.AppendLine("SpriteRenderer: " + (sprite == null ? "없음"
                : "enabled=" + sprite.enabled + " sprite=" + (sprite.sprite == null ? "null" : sprite.sprite.name)
                  + " sortingOrder=" + sprite.sortingOrder + " bounds=" + sprite.bounds));
            report.AppendLine("카메라 위치: " + camera.transform.position.ToString("F3")
                + " orthographicSize=" + camera.orthographicSize);

            bool inView = sprite != null
                && GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), sprite.bounds);
            report.AppendLine("스프라이트가 카메라 시야 안: " + inView);

            report.AppendLine("로드 중 에러/예외 " + _logged.Count + "건"
                + (_logged.Count == 0 ? "" : ":\n  " + string.Join("\n  ", _logged)));
            Debug.Log(report.ToString());

            // ----- 여기부터 판정 -----
            Assert.IsNotNull(down.collider,
                zone + ": 스폰 지점 아래 100유닛 안에 Ground 콜라이더가 없다 — 밟을 바닥이 없어 무한 낙하한다.\n" + report);

            float expectedY = down.point.y + PlayerHalfHeight(player);
            Assert.That(settled.y, Is.EqualTo(expectedY).Within(LandingTolerance),
                zone + ": 플레이어가 바닥 위에 착지하지 못했다(무한 낙하/파묻힘).\n" + report);

            Assert.IsTrue(player.IsGrounded,
                zone + ": 착지했는데도 IsGrounded가 false — 접지 판정(groundCheck/groundLayer)이 깨졌다.\n" + report);

            Assert.IsTrue(inView,
                zone + ": 플레이어 스프라이트가 카메라 시야 밖이다 — 화면에 아무것도 안 보인다.\n" + report);

            Assert.IsEmpty(_logged, zone + ": 씬 로드/플레이 중 에러가 발생했다.\n" + report);
        }

        static float PlayerHalfHeight(PlayerController player)
        {
            var capsule = player.GetComponent<CapsuleCollider2D>();
            return capsule == null ? 0f : capsule.size.y * 0.5f - capsule.offset.y;
        }

        static int CountTiles(Tilemap tilemap)
        {
            int count = 0;
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(cell)) count++;
            return count;
        }
    }
}
