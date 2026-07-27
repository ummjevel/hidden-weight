using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // 봇이 실제로 게임을 플레이한다. PlayerInput.Injected로 키 입력을 흘려넣어
    // PlayerController를 진짜로 돌리기 때문에, 기획 수치가 아니라 실제 물리 결과를 잰다.
    //
    // 여기서 나오는 숫자(점프 높이·점프 도달 거리·대시 거리)가 레벨의 구멍 폭·벽 높이보다
    // 작으면 그 구간은 사람이 아무리 잘해도 못 넘는다.
    public class PlaythroughTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        // 한 FixedUpdate 동안 주입할 입력.
        static IEnumerator Hold(PlayerInput.Frame frame, int fixedSteps)
        {
            for (int i = 0; i < fixedSteps; i++)
            {
                PlayerInput.Injected = frame;
                yield return new WaitForFixedUpdate();
                // jumpPressed/dashPressed는 GetKeyDown 흉내라 첫 스텝에서만 참이어야 한다.
                frame.jumpPressed = false;
                frame.dashPressed = false;
            }
        }

        [UnityTest]
        public IEnumerator 플레이어_기동력을_실제로_측정한다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            // 평지(Room1) 위에 세우고 안정될 때까지 기다린다.
            player.TeleportTo(new Vector3(5f, 1f, 0f));
            for (int i = 0; i < 60; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            var report = new StringBuilder();
            report.AppendLine("===== 플레이어 기동력 실측 =====");
            float groundY = player.transform.position.y;
            report.AppendLine("바닥 위 기준 y: " + groundY.ToString("F3"));

            // --- 제자리 최대 점프 높이(점프 유지) ---
            float peak = groundY;
            yield return Run(Hold(new PlayerInput.Frame { jumpPressed = true, jumpHeld = true }, 1));
            for (int i = 0; i < 120; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { jumpHeld = true };
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
                if (i > 10 && player.IsGrounded) break;
            }
            float jumpHeight = peak - groundY;
            report.AppendLine("최대 점프 높이: " + jumpHeight.ToString("F2") + " 유닛");

            // --- 달리며 점프했을 때 수평 도달 거리 ---
            player.TeleportTo(new Vector3(5f, 1f, 0f));
            for (int i = 0; i < 40; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            // 먼저 최고 속도까지 달린다.
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true }, 40));
            float takeoffX = player.transform.position.x;
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true, jumpPressed = true, jumpHeld = true }, 1));
            for (int i = 0; i < 160; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { horizontal = 1f, run = true, jumpHeld = true };
                yield return new WaitForFixedUpdate();
                if (i > 10 && player.IsGrounded) break;
            }
            float runJumpDistance = player.transform.position.x - takeoffX;
            report.AppendLine("달리기 점프 수평 도달: " + runJumpDistance.ToString("F2") + " 유닛");

            // --- 달리며 점프 + 공중 대시 ---
            player.TeleportTo(new Vector3(5f, 1f, 0f));
            for (int i = 0; i < 40; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true }, 40));
            float takeoffX2 = player.transform.position.x;
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true, jumpPressed = true, jumpHeld = true }, 1));
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true, jumpHeld = true }, 8));
            yield return Run(Hold(new PlayerInput.Frame { horizontal = 1f, run = true, jumpHeld = true, dashPressed = true }, 1));
            for (int i = 0; i < 160; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { horizontal = 1f, run = true, jumpHeld = true };
                yield return new WaitForFixedUpdate();
                if (i > 20 && player.IsGrounded) break;
            }
            float jumpDashDistance = player.transform.position.x - takeoffX2;
            report.AppendLine("달리기 점프 + 공중 대시 수평 도달: " + jumpDashDistance.ToString("F2") + " 유닛");

            report.AppendLine();
            report.AppendLine("--- 레벨 요구치와 비교 ---");
            report.AppendLine("프롤로그 Room2 구덩이 폭: 4 유닛 (x 36~40)");
            report.AppendLine("프롤로그 Room3 굴뚝: 벽 하단 y=2.2, 꼭대기 출구 y=9.5");
            report.AppendLine("잔재 Room2 무너진 다리 폭: 3 유닛 (x 35~38)");

            Debug.Log(report.ToString());

            Assert.Greater(jumpHeight, 0.5f, "점프가 사실상 안 된다.\n" + report);
            Assert.Greater(runJumpDistance, 1f, "달리기 점프로 앞으로 나아가지 못한다.\n" + report);
        }

        // 프롤로그 Room2는 x40~44 구간의 바닥이 양옆(y=3)보다 3 낮은 y=0이다. 브리핑상
        // "착지 실패 시 낮은 통로로 떨어져 안전하게 재시도"하는 구덩이인데, 실제로 떨어졌을 때
        // 다시 올라올 수 있는지를 봇으로 확인한다. 못 올라오면 재시도가 아니라 진행 불가다.
        [UnityTest]
        public IEnumerator 프롤로그_구덩이에_빠져도_다시_올라올_수_있다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            player.TeleportTo(new Vector3(42f, 1f, 0f)); // 구덩이 한가운데
            for (int i = 0; i < 60; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            var report = new StringBuilder();
            report.AppendLine("===== 프롤로그 구덩이 탈출 =====");
            report.AppendLine("구덩이 바닥에 선 위치: " + player.transform.position.ToString("F2"));

            int groundMask = LayerMask.GetMask("Ground");
            var pitFloor = Physics2D.Raycast(new Vector2(42f, 8f), Vector2.down, 20f, groundMask);
            var ledge = Physics2D.Raycast(new Vector2(46f, 8f), Vector2.down, 20f, groundMask);
            report.AppendLine("구덩이 바닥 표면 y: " + (pitFloor.collider == null ? "없음" : pitFloor.point.y.ToString("F2"))
                + " / 나갈 발판(x=46) 표면 y: " + (ledge.collider == null ? "없음" : ledge.point.y.ToString("F2")));

            float bestY = player.transform.position.y;
            float bestX = player.transform.position.x;

            // 좌우로 달리며 계속 점프해 본다(벽에 붙으면 벽점프도 시도).
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float dir = attempt % 2 == 0 ? 1f : -1f;
                for (int i = 0; i < 90; i++)
                {
                    bool wantJump = player.IsGrounded || player.IsOnWall;
                    PlayerInput.Injected = new PlayerInput.Frame
                    {
                        horizontal = dir,
                        run = true,
                        jumpPressed = wantJump,
                        jumpHeld = true,
                    };
                    yield return new WaitForFixedUpdate();
                    bestY = Mathf.Max(bestY, player.transform.position.y);
                    bestX = Mathf.Max(bestX, player.transform.position.x);
                }
            }

            report.AppendLine("도달한 최고 y: " + bestY.ToString("F2") + " (탈출하려면 약 3.7 필요)");
            report.AppendLine("도달한 최대 x: " + bestX.ToString("F2") + " (구덩이는 x 40~44)");
            report.AppendLine("최종 위치: " + player.transform.position.ToString("F2"));
            Debug.Log(report.ToString());

            // 양옆 바닥 표면이 y=3이므로 캡슐 반높이 0.7을 더해 y≈3.7까지 올라야 나갈 수 있다.
            // "지금 어디 있나"가 아니라 "구덩이를 벗어난 적이 있나"로 판정한다 — 봇은 탈출한 뒤에도
            // 계속 달리다가 다른 낮은 지대에 있을 수 있다.
            Assert.Greater(bestY, 3.7f,
                "구덩이에 빠지면 다시 올라올 수 없다 — 되돌릴 방법이 없어 진행 불가 상태가 된다.\n" + report);
            Assert.Greater(bestX, 44f,
                "구덩이 밖으로 나가지 못했다.\n" + report);
        }

        // 잔재·균열의 "안전 바닥"은 본 바닥보다 6 낮아 점프로는 절대 못 올라온다.
        // 떨어졌을 때 체크포인트로 되돌려 보내는지 확인한다.
        [UnityTest]
        public IEnumerator 깊은_구덩이에_떨어지면_체크포인트로_복귀한다(
            [Values("Zone_Residue", "Zone_Fracture")] string zone,
            [Values(36.5f, 38f)] float pitX)
        {
            yield return SceneManager.LoadSceneAsync(zone, LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;

            // 먼저 체크포인트(4,1)를 밟아 복귀 지점을 등록한다 — 실제 플레이 순서 그대로.
            player.TeleportTo(new Vector3(6f, 1f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(new Vector3(4f, 1f, 0f));
            for (int i = 0; i < 20; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            var checkpoint = GameManager.Instance.Progress.LastCheckpoint;
            Assert.AreNotEqual(Vector3.zero, checkpoint,
                zone + ": 체크포인트를 밟았는데 복귀 지점이 등록되지 않았다.");

            // 구덩이 위에서 떨어뜨린다.
            player.TeleportTo(new Vector3(pitX, 2f, 0f));
            for (int i = 0; i < 200; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            var final = player.transform.position;
            Assert.Greater(final.y, -4f,
                zone + ": 깊은 안전 바닥(" + pitX + ")에 떨어진 뒤 그대로 갇혔다. 최종 위치=" + final.ToString("F2")
                + " (점프 높이 2.72로는 6을 못 올라온다)");
        }

        // 프롤로그 Room3은 좌우 벽(x=58, x=61) 사이 굴뚝을 벽점프 핑퐁으로 올라 꼭대기
        // 출구 트리거(59.5, 9.5)에 닿아야 클리어된다. 예선 영상이 지나갈 경로라 실제로
        // 오를 수 있는지 봇으로 확인한다.
        [UnityTest]
        public IEnumerator 프롤로그_굴뚝을_벽점프로_오를_수_있다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            var body = player.GetComponent<Rigidbody2D>();
            player.TeleportTo(new Vector3(59.5f, 1f, 0f)); // 굴뚝 아래
            for (int i = 0; i < 60; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            var report = new StringBuilder();
            report.AppendLine("===== 프롤로그 굴뚝 등반 =====");
            report.AppendLine("굴뚝 바닥 시작: " + player.transform.position.ToString("F2"));

            float bestY = player.transform.position.y;
            float dir = 1f;

            bool clearedZone = false;

            for (int i = 0; i < 900; i++)
            {
                // 출구 트리거에 닿으면 다음 지역이 로드되면서 이 플레이어는 파괴된다 = 클리어.
                if (player == null)
                {
                    clearedZone = true;
                    break;
                }

                var frame = new PlayerInput.Frame { jumpHeld = true };

                if (player.IsGrounded)
                {
                    // 바닥에서는 한쪽 벽으로 달려가며 점프해 벽에 붙는다.
                    frame.horizontal = dir;
                    frame.jumpPressed = true;
                }
                else if (player.IsOnWall)
                {
                    // 벽에 붙어 있으면 벽 쪽을 계속 누른 채(매달리기 조건) 점프한다.
                    frame.horizontal = player.Facing;
                    frame.jumpPressed = true;
                    dir = -player.Facing; // 다음엔 반대쪽 벽으로
                }
                else
                {
                    // 공중에서는 날아가는 방향을 계속 눌러 반대쪽 벽에 붙는다.
                    frame.horizontal = Mathf.Abs(body.linearVelocity.x) > 0.1f
                        ? Mathf.Sign(body.linearVelocity.x) : dir;
                }

                PlayerInput.Injected = frame;
                yield return new WaitForFixedUpdate();

                // 대기 중에 지역이 바뀌면(=클리어) 플레이어가 파괴돼 있다.
                if (player == null) { clearedZone = true; break; }
                bestY = Mathf.Max(bestY, player.transform.position.y);
            }

            report.AppendLine("출구 트리거 발동(다음 지역 로드): " + clearedZone);
            report.AppendLine("도달한 최고 y: " + bestY.ToString("F2") + " (출구 트리거는 y 8~11)");
            report.AppendLine("최종 위치: " + (player == null ? "(다음 지역으로 전환됨)"
                : player.transform.position.ToString("F2")));
            report.AppendLine("현재 지역: " + GameManager.Instance.Progress.CurrentZone);
            Debug.Log(report.ToString());

            Assert.IsTrue(clearedZone || bestY > 8f,
                "굴뚝 꼭대기(출구 트리거 y8~11)까지 오르지 못했다 — 프롤로그를 클리어할 수 없다.\n" + report);
        }

        // 중첩 코루틴 실행 헬퍼.
        static IEnumerator Run(IEnumerator routine)
        {
            while (routine.MoveNext()) yield return routine.Current;
        }
    }
}
