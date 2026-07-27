using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // "새 잔재 씬에서 스페이스(점프)가 안 먹는다"를 게임 쪽 문제인지 가려내는 확인용.
    public class JumpSanityTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 새_잔재_씬에서_점프가_동작한다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            for (int i = 0; i < 60; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            float groundY = player.transform.position.y;
            bool groundedBefore = player.IsGrounded;
            bool inputEnabled = PlayerInput.Enabled;
            float timeScale = Time.timeScale;

            // 스페이스 1프레임 누르고 계속 홀드.
            PlayerInput.Injected = new PlayerInput.Frame { jumpPressed = true, jumpHeld = true };
            yield return new WaitForFixedUpdate();

            float peak = groundY;
            for (int i = 0; i < 90; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { jumpHeld = true };
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
            }

            Debug.Log("===== 점프 확인 ===== 접지=" + groundedBefore
                + " PlayerInput.Enabled=" + inputEnabled
                + " Time.timeScale=" + timeScale
                + " GameState=" + GameManager.Instance.State
                + " 시작y=" + groundY.ToString("F2") + " 최고y=" + peak.ToString("F2")
                + " 상승=" + (peak - groundY).ToString("F2"));

            Assert.IsTrue(groundedBefore, "스폰 직후 접지 상태가 아니다 — 점프 조건 자체가 성립하지 않는다.");
            Assert.Greater(peak - groundY, 1f, "스페이스를 넣었는데 점프하지 않았다.");
        }
    }
}
