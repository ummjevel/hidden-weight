using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번 "클레임 전화기": 원거리형, 일정 거리에서 전화 공격. 너무 가까우면 물러나고
    // 너무 멀면 다가와서 선호 사거리(preferredRange)를 유지하려 한다.
    public class ClaimPhoneEnemy : EnemyBase
    {
        [SerializeField] private float preferredRange = 4f;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private int attackDamage = 8;
        [SerializeField] private float attackInterval = 2f;

        private Cooldown _attackCooldown;

        protected override void Awake()
        {
            moveSpeed = 1f;
            base.Awake();
            _attackCooldown = new Cooldown(attackInterval);
        }

        protected override void Update()
        {
            base.Update();
            _attackCooldown.Tick(Time.deltaTime);
            TryRangedAttack();
        }

        protected override Vector2 GetMoveDirection()
        {
            if (PlayerTransform == null) return Vector2.zero;

            var toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
            var distance = toPlayer.magnitude;

            if (distance > preferredRange + 0.5f) return toPlayer.normalized;
            if (distance < preferredRange - 0.5f) return -toPlayer.normalized;
            return Vector2.zero;
        }

        private void TryRangedAttack()
        {
            if (PlayerTransform == null || !_attackCooldown.IsReady) return;

            var distance = Vector2.Distance(transform.position, PlayerTransform.position);
            if (distance > attackRange) return;

            var player = PlayerTransform.GetComponent<PlayerController>();
            if (player == null) return;

            player.Reputation.TakeDamage(attackDamage);
            _attackCooldown.TryUse();
        }
    }
}
