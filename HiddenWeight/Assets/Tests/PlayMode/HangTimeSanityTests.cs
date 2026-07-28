using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // 점프 정점에서 중력이 줄어들어 "붕 뜨는" hang time 구간이 실제로 생기는지 확인.
    public class HangTimeSanityTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 점프_정점에서_중력이_줄어든다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            var data = GameManager.Instance.Balance.player;
            var rb = player.GetComponent<Rigidbody2D>();

            for (int i = 0; i < 60; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            // 스페이스 1프레임 누르고 계속 홀드(가변 점프 컷이 끼어들지 않게).
            PlayerInput.Injected = new PlayerInput.Frame { jumpPressed = true, jumpHeld = true };
            yield return new WaitForFixedUpdate();

            int apexFrames = 0;
            for (int i = 0; i < 120; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { jumpHeld = true };
                yield return new WaitForFixedUpdate();
                if (Mathf.Abs(rb.linearVelocity.y) < data.jumpApexThreshold) apexFrames++;
            }

            Debug.Log("===== hang time 확인 ===== apex 임계값=" + data.jumpApexThreshold
                + " apex 중력배수=" + data.jumpApexGravityMultiplier
                + " apex 구간 프레임수=" + apexFrames);

            Assert.Greater(apexFrames, 5, "정점 부근(|vy| < jumpApexThreshold)에 머무는 시간이 너무 짧다 — hang time이 동작하지 않는다.");
        }
    }
}
