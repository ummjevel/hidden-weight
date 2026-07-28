using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Enemies;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // "몬스터 순찰 지역색 + 견고성 수정" 배치 검증: 잔재 보행자의 멈칫거림(turnHesitationSeconds)과
    // 애도 운반자(ChargerBehavior)가 돌진 중 EnemyPatrol과 속도를 두고 다투지 않는지 확인한다.
    public class EnemyPersonalitySanityTests
    {
        const string SceneName = "Zone_Residue_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 잔재_보행자가_방향전환_전에_멈칫거린다()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            EnemyPatrol walker = null;
            foreach (var patrol in Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None))
            {
                if (!patrol.isActiveAndEnabled) continue;
                var enemy = patrol.GetComponent<Enemy>();
                if (enemy != null && enemy.Data.turnHesitationSeconds > 0f) { walker = patrol; break; }
            }
            Assert.IsNotNull(walker, "turnHesitationSeconds > 0인 활성 순찰 개체(잔재 보행자)를 찾지 못했다.");

            var body = walker.GetComponent<Rigidbody2D>();
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate(); // 바닥에 안착

            bool inPause = false;
            float pauseTimer = 0f;
            float longestPause = 0f;
            bool flippedRightAfterPause = false;
            float dirBeforePause = Mathf.Sign(walker.transform.localScale.x);

            for (int i = 0; i < 300; i++) // 6초 관찰
            {
                yield return new WaitForFixedUpdate();
                bool nearZero = Mathf.Abs(body.linearVelocity.x) < 0.05f;

                if (nearZero)
                {
                    if (!inPause)
                    {
                        inPause = true;
                        pauseTimer = 0f;
                        dirBeforePause = Mathf.Sign(walker.transform.localScale.x);
                    }
                    pauseTimer += Time.fixedDeltaTime;
                }
                else if (inPause)
                {
                    longestPause = Mathf.Max(longestPause, pauseTimer);
                    float dirAfter = Mathf.Sign(walker.transform.localScale.x);
                    if (!Mathf.Approximately(dirAfter, dirBeforePause)) flippedRightAfterPause = true;
                    inPause = false;
                }
            }

            Debug.Log("===== 보행자 멈칫거림 ===== 최장 정지시간=" + longestPause.ToString("F2")
                + "초 정지후_방향전환=" + flippedRightAfterPause);

            Assert.Greater(longestPause, 0.25f,
                "멈칫거리는 시간이 관측되지 않았다(EnemyData.turnHesitationSeconds=0.35 기대).\n최장 정지=" + longestPause);
            Assert.IsTrue(flippedRightAfterPause, "멈칫거림 뒤에 방향이 바뀌지 않았다 — Flip()이 호출 안 된 것으로 보인다.");
        }

        [UnityTest]
        public IEnumerator 잔재_운반자가_돌진_중에는_순찰속도로_되돌아가지_않는다()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            ChargerBehavior charger = null;
            foreach (var candidate in Object.FindObjectsByType<ChargerBehavior>(FindObjectsSortMode.None))
            {
                charger = candidate;
                break;
            }
            Assert.IsNotNull(charger, "잔재 씬에 애도 운반자(ChargerBehavior)가 없다.");

            var patrol = charger.GetComponent<EnemyPatrol>();
            Assert.IsNotNull(patrol, "운반자에 EnemyPatrol이 없다.");
            var body = charger.GetComponent<Rigidbody2D>();
            var data = charger.GetComponent<Enemy>().Data;

            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate(); // 바닥에 안착

            var player = PlayerController.Instance;
            player.TeleportTo(charger.transform.position + new Vector3(3f, 0f, 0f));

            bool sawPatrolDisabled = false;
            bool sawPatrolReenabled = false;
            float peakChargeSpeed = 0f;

            for (int i = 0; i < 250; i++) // 5초 관찰: 예고 0.8초 + 돌진 최대 1.5초 + 회복 1초 여유
            {
                yield return new WaitForFixedUpdate();
                if (!patrol.enabled) sawPatrolDisabled = true;
                else if (sawPatrolDisabled) sawPatrolReenabled = true;
                peakChargeSpeed = Mathf.Max(peakChargeSpeed, Mathf.Abs(body.linearVelocity.x));
            }

            Debug.Log("===== 운반자 돌진 ===== patrol비활성_관측=" + sawPatrolDisabled
                + " patrol재활성_관측=" + sawPatrolReenabled
                + " 최고속도=" + peakChargeSpeed.ToString("F2") + " (돌진속도 기대=" + data.chargeSpeed + ")");

            Assert.IsTrue(sawPatrolDisabled,
                "돌진 시퀀스 중에도 EnemyPatrol이 한 번도 비활성화되지 않았다 — 순찰 속도와 계속 충돌할 수 있다.");
            Assert.IsTrue(sawPatrolReenabled,
                "돌진이 끝난 뒤 EnemyPatrol이 다시 켜지지 않았다 — 이후 순찰이 멈춘다.");
            Assert.Greater(peakChargeSpeed, data.chargeSpeed * 0.5f,
                "돌진 속도(기대 " + data.chargeSpeed + ")에 근접하지 못했다 — patrol이 계속 속도를 순찰 속도로 덮어썼을 가능성.\n최고속도=" + peakChargeSpeed);
        }
    }
}
