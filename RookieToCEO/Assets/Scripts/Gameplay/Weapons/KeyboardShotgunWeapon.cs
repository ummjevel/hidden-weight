using System.Collections.Generic;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Weapons
{
    // GDD 3번 "시작 무기: 키보드 샷건". 부채꼴 범위 안의 여러 적을 동시에 공격한다.
    // 공격속도는 느리지만(baseAttackInterval이 큼) 범위가 넓다(wideHalfAngle, 다중 타겟).
    public class KeyboardShotgunWeapon : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float baseAttackInterval = 1.2f; // 느린 공격속도(GDD 기준)
        [SerializeField] private float baseRange = 3f;
        [SerializeField] private float halfAngleDegrees = 60f;   // 부채꼴 절반각(총 120도)
        [SerializeField] private int maxTargets = 5;

        private PlayerController _player;
        private Cooldown _attackCooldown;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _attackCooldown = new Cooldown(baseAttackInterval);
        }

        private void Update()
        {
            var interval = WeaponMath.EffectiveAttackInterval(baseAttackInterval, _player.Stats.AttackSpeedMultiplier);
            _attackCooldown.SetDuration(interval);
            _attackCooldown.Tick(Time.deltaTime);

            if (_attackCooldown.IsReady)
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            var facing = _player.FacingDirection;
            var range = WeaponMath.EffectiveRange(baseRange, _player.Stats.RangeMultiplier);
            var candidates = EnemyRegistry.Instance.Positions;

            var hits = ConeTargetingUtility.FindTargetsInCone(
                transform.position, facing, halfAngleDegrees, range, candidates, maxTargets);

            if (hits.Count == 0) return;

            var damage = WeaponMath.EffectiveDamage(baseDamage, _player.Stats.DamageMultiplier);
            foreach (var index in hits)
            {
                EnemyRegistry.Instance.DamageAt(index, damage);
            }

            _attackCooldown.TryUse();
        }
    }
}
