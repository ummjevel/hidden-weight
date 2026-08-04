using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 설계 10절: "발판 위상은 사망 후 항상 같은 시작값으로 돌아가 실패에서 학습할 수 있게 한다."
    //
    // 이게 없으면 죽고 다시 시도할 때마다 발판이 제각각의 위상에 있어, 같은 점프가 매번
    // 다른 타이밍을 요구한다 — 실패가 학습으로 이어지지 않고 운으로 넘어간다.
    //
    // 예전 구현은 Time.time을 그대로 썼다(주석에는 "그러면 두 조건이 동시에 만족된다"고
    // 적혀 있었으나 사실이 아니었다). 지금은 ZoneClock을 공유하고 사망·방 진입에서 되돌린다.
    public class FracturePlatformPhaseTests
    {
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
        public IEnumerator 사망하면_발판이_처음_위상으로_돌아온다()
        {
            // F06 시차 온실 — 서로 다른 주기의 이동 발판이 모여 있다(설계 4.6).
            yield return RoomTestHarness.EnterRoom("Fracture", "F06");
            Time.timeScale = 1f;
            yield return new WaitForFixedUpdate();

            var platforms = Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude);
            Assert.IsNotEmpty(platforms, "F06에 이동 발판이 없다.");

            // 방에 들어선 직후(위상 0)의 자리를 기억한다.
            var start = new Vector3[platforms.Length];
            for (int i = 0; i < platforms.Length; i++) start[i] = platforms[i].transform.position;

            // 한참 움직이게 두어 위상이 확실히 달라지게 한다.
            float moved = 0f;
            while (moved < 1.5f)
            {
                moved += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            bool anyMoved = false;
            for (int i = 0; i < platforms.Length; i++)
                if (Vector3.Distance(platforms[i].transform.position, start[i]) > 0.2f) anyMoved = true;
            Assert.IsTrue(anyMoved, "발판이 아예 움직이지 않아 이 검사가 의미가 없다.");

            // 죽는다.
            GameManager.Instance.RespawnPlayer();
            yield return null;
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i] == null) continue;
                float back = Vector3.Distance(platforms[i].transform.position, start[i]);
                Assert.Less(back, 0.25f,
                    $"{platforms[i].name}: 사망 후 처음 위상으로 돌아오지 않았다 (차이 {back:F2}). "
                    + "같은 점프가 시도마다 다른 타이밍을 요구하게 된다.");
            }
        }

        // 위상을 되돌려도 예지는 여전히 정확해야 한다 — 둘은 같은 시계를 읽는다.
        [UnityTest]
        public IEnumerator 위상을_되돌려도_예지는_정확하다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F06");
            Time.timeScale = 1f;
            GameManager.Instance.RespawnPlayer();     // 시계를 막 되돌린 직후에도
            yield return new WaitForFixedUpdate();

            const float lead = 2f;
            var platforms = Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude);
            var predicted = new Vector3[platforms.Length];
            for (int i = 0; i < platforms.Length; i++)
                predicted[i] = ((IForeseeable)platforms[i]).PredictPosition(lead);

            float waited = 0f;
            while (waited < lead)
            {
                waited += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i] == null) continue;
                float error = Vector3.Distance(platforms[i].transform.position, predicted[i]);
                Assert.Less(error, 0.3f,
                    $"{platforms[i].name}: 예지가 보여준 자리와 실제 2초 뒤가 다르다 (차이 {error:F2}).");
            }
        }
    }
}
