using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Weapons
{
    // GDD 3번 "1층 밤 보상: 스테이플러 연사". 좁은 직선 범위로 빠르게 쏘고,
    // 레벨이 오르면 연사속도와 관통 횟수가 늘어난다(GDD 원문 그대로).
    // 무기 자체의 레벨은 GDD에 별도 획득 경로가 정의돼 있지 않아, 우선 LevelUp()을 밖에서
    // 호출할 수 있게만 열어두고 실제 트리거(예: 특정 조건 달성)는 이후 마일스톤에서 연결한다.
    public class StaplerRapidFireWeapon : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 6;
        [SerializeField] private float baseAttackInterval = 0.3f; // 빠른 공격속도
        [SerializeField] private float baseRange = 6f;
        [SerializeField] private float halfAngleDegrees = 8f;     // 좁은 직선형 판정
        [SerializeField] private int basePierceCount = 1;         // 레벨 0: 가장 가까운 적 1명

        private PlayerController _player;
        private Cooldown _attackCooldown;

        public int Level { get; private set; }

        public void LevelUp()
        {
            Level++;
        }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _attackCooldown = new Cooldown(baseAttackInterval);
        }

        private void Update()
        {
            var levelAttackSpeedBonus = 1f + Level * 0.15f;
            var attackSpeed = _player.Stats.AttackSpeedMultiplier * levelAttackSpeedBonus * _player.AttackSpeedDebuffMultiplier;
            var interval = WeaponMath.EffectiveAttackInterval(baseAttackInterval, attackSpeed);
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
            var pierceCount = basePierceCount + Level;

            var hits = ConeTargetingUtility.FindTargetsInCone(
                transform.position, facing, halfAngleDegrees, range, candidates, pierceCount);

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
