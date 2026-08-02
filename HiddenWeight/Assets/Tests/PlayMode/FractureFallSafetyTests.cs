using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 방 밖으로 떨어질 수 있는 자리를 찾는다.
    //
    // 인수인계 문서(ROOM_PORTAL_STATUS.md)가 남긴 미해결 부채다: "방에 동쪽 경계벽이
    // 없다. 방 끝의 문(콜라이더 폭 1.2)에 의존하므로 대시·낙하로 스쳐 지나가면 허공으로
    // 떨어질 수 있다." 방을 씬으로 쪼갠 뒤로는 방마다 좌우가 열려 있어 더 쉬워졌다.
    //
    // VoidRespawn이 최후에 잡아 주기는 하지만, 설계 10절은 "일반 추락은 체력 1 피해와
    // **방별 안전지점 복귀**"를 말한다 — 30유닛을 떨어진 뒤 체크포인트로 튕기는 것은
    // 그 규칙이 아니다. 떨어질 수 있는 자리를 목록으로 만들어 둔다.
    public class FractureFallSafetyTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 방_좌우_끝에서_허공으로_나가지_못한다()
        {
            string[] rooms = { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08",
                               "F09", "F10", "F11", "F12" };
            var report = new StringBuilder("[균열 방 좌우 끝 점검]\n");
            var open = new System.Collections.Generic.List<string>();

            int ground = LayerMask.GetMask("Ground");
            int wall = LayerMask.GetMask("Wall");

            foreach (var name in rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + name, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();

                var room = Object.FindAnyObjectByType<Room>();
                if (room == null) continue;
                var bounds = room.WorldBounds;

                // 방의 양 끝 바로 바깥. 여기 벽도 바닥도 없으면 걸어 나갈 수 있다.
                foreach (var (side, x) in new[] { ("서", bounds.min.x - 0.4f), ("동", bounds.max.x + 0.4f) })
                {
                    bool blocked = false;
                    // 방 높이 전체를 훑어 벽이 하나라도 있으면 막힌 것으로 본다.
                    for (float y = bounds.min.y + 0.5f; y <= bounds.max.y && !blocked; y += 1f)
                        if (Physics2D.OverlapCircle(new Vector2(x, y), 0.35f, wall) != null) blocked = true;

                    // 벽이 없어도 그 자리에 바닥이 이어져 있으면 떨어지지 않는다.
                    if (!blocked)
                        for (float y = bounds.min.y; y <= bounds.max.y && !blocked; y += 1f)
                            if (Physics2D.OverlapCircle(new Vector2(x, y), 0.35f, ground) != null) blocked = true;

                    if (!blocked)
                    {
                        open.Add($"{name}:{side}");
                        report.AppendLine($"  {name} {side}쪽 끝(x={x:F1})이 열려 있다 — 벽도 바닥도 없다.");
                    }
                }
            }

            Debug.Log(report.ToString());
            Assert.IsEmpty(open,
                "방 밖으로 걸어 나갈 수 있는 자리가 있다: " + string.Join(", ", open) + "\n" + report);
        }
    }
}
