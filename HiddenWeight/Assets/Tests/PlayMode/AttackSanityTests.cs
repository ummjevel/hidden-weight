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

            // 적은 이제 실제로 순찰한다. 그대로 두면 때리기 전에 사거리 밖으로 걸어 나가
            // "공격이 안 먹는다"로 오탐한다. 검사 대상은 세워 두고 공격만 본다.
            var patrol = target.GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
            target.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

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

        // 공격 스윙이 상태 길이(attackActiveTime=0.1초)에 잘려 준비 프레임만 보이던 회귀를
        // 막는다. Attack 상태가 끝난 뒤에도 덮어쓰기 계층이 스윙 클립을 붙들고 있어야 한다.
        [UnityTest]
        public IEnumerator 공격_스윙_클립은_상태가_끝나도_끝까지_재생된다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            var animator = player.GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
            Assert.IsNotNull(animator);
            if (!animator.Has("PlayerAttack"))
                Assert.Ignore("공격 클립이 아직 없다 — 아트 미도입 단계.");

            // 접지 안정화 후 J 한 번.
            for (int i = 0; i < 20; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            PlayerInput.Injected = new PlayerInput.Frame { attackPressed = true };
            yield return new WaitForFixedUpdate();

            // 0.2초 뒤 — Attack 상태(0.1초)는 이미 끝났지만 스윙(0.375초)은 재생 중이어야 한다.
            for (int i = 0; i < 10; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.AreEqual("PlayerAttack", animator.CurrentClip,
                "Attack 상태가 끝나자마자 스윙이 끊겼다 — 참격 프레임이 화면에 나오지 않는다.");
        }
    }
}
