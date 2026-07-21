using RookieToCEO.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RookieToCEO.Gameplay.Skills
{
    // GDD 3번 "2층 밤 보상: 업무 떠넘기기". Space로 사용하는 액티브 - 주변 적을 바깥으로 밀어내는
    // 위기 탈출기. 기본 쿨타임 12초(GDD 원문)이며 짬 스탯(StatSystem.CooldownMultiplier)으로 줄어든다.
    public class WorkDumpSkill : MonoBehaviour
    {
        private const float BaseCooldownSeconds = 12f;

        [SerializeField] private float radius = 2.5f;
        [SerializeField] private float knockbackForce = 8f;

        // M9: 배정되면 radius/knockbackForce를 덮어쓴다. 쿨타임 12초는 GDD 고정값이라 SO로 빼지 않았다.
        [SerializeField] private BalanceData balanceData;

        private PlayerController _player;
        private Cooldown _cooldown;

        public bool IsReady => _cooldown.IsReady;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            if (balanceData != null)
            {
                radius = balanceData.workDumpRadius;
                knockbackForce = balanceData.workDumpKnockbackForce;
            }

            _cooldown = new Cooldown(BaseCooldownSeconds);
        }

        private void Update()
        {
            _cooldown.SetDuration(BaseCooldownSeconds * _player.Stats.CooldownMultiplier);
            _cooldown.Tick(Time.deltaTime);

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryUse();
            }
        }

        private void TryUse()
        {
            if (!_cooldown.TryUse()) return;

            var origin = (Vector2)transform.position;
            var registry = EnemyRegistry.Instance;
            var positions = registry.Positions;
            var radiusSqr = radius * radius;

            for (var i = 0; i < positions.Count; i++)
            {
                var toEnemy = positions[i] - origin;
                if (toEnemy.sqrMagnitude > radiusSqr) continue;

                var pushDirection = toEnemy.sqrMagnitude > 0f ? toEnemy.normalized : Vector2.up;
                registry.KnockbackAt(i, pushDirection, knockbackForce);
            }
        }
    }
}
