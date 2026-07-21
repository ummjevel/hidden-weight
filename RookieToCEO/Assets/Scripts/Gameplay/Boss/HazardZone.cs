using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Boss
{
    // GDD 13번 "퇴근 취소" 단계의 "바닥에 빨간 업무 구역 생성 - 구역 안에 있으면 지속 피해".
    [RequireComponent(typeof(Collider2D))]
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] private int damagePerTick = 5;
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] private float lifetimeSeconds = 4f;

        private Cooldown _damageCooldown;
        private float _lifeTimer;

        private void Awake()
        {
            _damageCooldown = new Cooldown(tickInterval);
            _lifeTimer = lifetimeSeconds;
        }

        private void Update()
        {
            _damageCooldown.Tick(Time.deltaTime);

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_damageCooldown.IsReady) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            player.Reputation.TakeDamage(damagePerTick);
            _damageCooldown.TryUse();
        }
    }
}
