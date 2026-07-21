using System.Collections.Generic;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Weapons
{
    // GDD 3번 "시작 무기: 키보드 샷건". 부채꼴 범위 안의 여러 적을 동시에 공격한다.
    // 공격속도는 느리지만(baseAttackInterval이 큼) 범위가 넓다(wideHalfAngle, 다중 타겟).
    public class KeyboardShotgunWeapon : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 12;
        [SerializeField] private float baseAttackInterval = 1.2f; // 느린 공격속도(GDD 기준)
        [SerializeField] private float baseRange = 3f;
        [SerializeField] private float halfAngleDegrees = 60f;   // 부채꼴 절반각(총 120도)
        [SerializeField] private int maxTargets = 5;

        // M9: 배정되면 이 값들로 위 기본값을 덮어써서 코드 재컴파일 없이 밸런스를 조정할 수 있다.
        [SerializeField] private BalanceData balanceData;

        private PlayerController _player;
        private Cooldown _attackCooldown;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            if (balanceData != null)
            {
                baseDamage = balanceData.keyboardShotgun.baseDamage;
                baseAttackInterval = balanceData.keyboardShotgun.baseAttackInterval;
                baseRange = balanceData.keyboardShotgun.baseRange;
            }

            _attackCooldown = new Cooldown(baseAttackInterval);
        }

        private void Update()
        {
            var attackSpeed = _player.Stats.AttackSpeedMultiplier * _player.AttackSpeedDebuffMultiplier;
            var interval = WeaponMath.EffectiveAttackInterval(baseAttackInterval, attackSpeed);
            _attackCooldown.SetDuration(interval);
            _attackCooldown.Tick(Time.deltaTime);

            // GDD 9번: 상사의 시선에 걸려 "일하는 척" 중에는 자동 공격이 멈춘다.
            if (_attackCooldown.IsReady && !_player.IsPretendingToWork)
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
