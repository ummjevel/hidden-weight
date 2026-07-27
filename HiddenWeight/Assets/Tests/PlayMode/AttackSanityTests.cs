using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // "공격 키가 없는 것 같다"를 가려낸다. J 키가 실제로 적 체력을 깎는지, 그리고 공격했다는
    // 사실이 화면에 드러나는지(상태 전환) 확인한다.
    public class AttackSanityTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator J키_공격이_적_체력을_깎는다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;

            // R02의 잔재 보행자를 데려와 바로 앞에 세운다.
            Enemy target = null;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (enemy.GetComponent<EnemyPatrol>() != null && enemy.isActiveAndEnabled) { target = enemy; break; }
            Assert.IsNotNull(target, "씬에 순찰형 적이 없다.");

            player.TeleportTo(target.transform.position + new Vector3(-1f, 0f, 0f));
            for (int i = 0; i < 20; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            int before = target.Health;
            var attack = player.GetComponent<PlayerAttack>();
            Assert.IsNotNull(attack, "PlayerAttack 컴포넌트가 없다.");
            Assert.IsTrue(attack.CanAttack, "공격이 막혀 있다(CanAttack=false).");

            bool sawAttackState = false;
            for (int i = 0; i < 60 && target != null; i++)
            {
                // 오른쪽을 보며 J를 누른다.
                PlayerInput.Injected = new PlayerInput.Frame { horizontal = 0.2f, attackPressed = i % 30 == 0 };
                yield return new WaitForFixedUpdate();
                if (player.State == PlayerState.Attack) sawAttackState = true;
            }

            int after = target == null ? 0 : target.Health;
            Debug.Log("===== 공격 확인 ===== 공격 전 체력=" + before + " 후=" + after
                + " 적 파괴=" + (target == null) + " Attack 상태 진입=" + sawAttackState
                + " 공격 반경=" + GameManager.Instance.Balance.player.attackRadius
                + " 각도=" + GameManager.Instance.Balance.player.attackAngle
                + " 쿨타임=" + GameManager.Instance.Balance.player.attackCooldown);

            Assert.IsTrue(target == null || after < before,
                "J를 눌렀는데 적 체력이 그대로다(전=" + before + ", 후=" + after + ").");
            Assert.IsTrue(sawAttackState, "공격했는데 PlayerState가 Attack으로 바뀌지 않았다.");
        }
    }
}
