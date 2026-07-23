using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Weapons
{
    // GDD 3번 "1층 밤 보상: 스테이플러 연사". 좁은 직선 범위로 빠르게 쏘고,
    // 레벨이 오르면 연사속도와 관통 횟수가 늘어난다(GDD 원문 그대로).
    // 무기 자체의 레벨을 언제 올릴지는 GDD에 명시가 없어서, 플레이어 전체 레벨업(3택1 선택)에
    // 맞춰 함께 올라가는 것으로 정했다 - 별도 UI/선택지를 새로 만들지 않고도 GDD가 말한
    // "레벨이 오르면 강화된다"를 자연스럽게 충족한다.
    public class StaplerRapidFireWeapon : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 6;
        [SerializeField] private float baseAttackInterval = 0.3f; // 빠른 공격속도
        [SerializeField] private float baseRange = 6f;
        [SerializeField] private float halfAngleDegrees = 8f;     // 좁은 직선형 판정
        [SerializeField] private int basePierceCount = 1;         // 레벨 0: 가장 가까운 적 1명

        // M9: 배정되면 이 값들로 위 기본값을 덮어써서 코드 재컴파일 없이 밸런스를 조정할 수 있다.
        [SerializeField] private BalanceData balanceData;

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

            if (balanceData != null)
            {
                baseDamage = balanceData.staplerRapidFire.baseDamage;
                baseAttackInterval = balanceData.staplerRapidFire.baseAttackInterval;
                baseRange = balanceData.staplerRapidFire.baseRange;
            }

            _attackCooldown = new Cooldown(baseAttackInterval);

            // OnEnable이 아니라 Awake에서 구독한다: 이 무기는 밤 조사를 마치기 전까지
            // 비활성(enabled=false) 상태로 대기하는데, OnEnable에서 구독하면 비활성 기간 동안의
            // 레벨업을 놓쳐서 실제로 활성화됐을 때 스테이플러 레벨이 뒤처진다. Awake는 활성화
            // 여부와 무관하게 항상 한 번 실행되므로, 비활성 상태에서도 계속 레벨을 추적하다가
            // 활성화되는 순간 이미 쌓인 레벨이 바로 반영되게 만든다.
            _player.Level.OnLevelUp += HandlePlayerLevelUp;
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Level.OnLevelUp -= HandlePlayerLevelUp;
        }

        private void HandlePlayerLevelUp()
        {
            LevelUp();
        }

        private void Update()
        {
            var levelAttackSpeedBonus = 1f + Level * 0.15f;
            var attackSpeed = _player.Stats.AttackSpeedMultiplier * levelAttackSpeedBonus * _player.AttackSpeedDebuffMultiplier;
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
            // 지속되는(DontDestroyOnLoad) Player는 EnemyRegistry가 아직 없는 씬(예: Bootstrap)에서도
            // Update가 계속 돌기 때문에, EnemyRegistry.Instance가 비어 있을 수 있다.
            if (EnemyRegistry.Instance == null) return;

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
