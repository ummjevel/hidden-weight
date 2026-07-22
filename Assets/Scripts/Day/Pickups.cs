using UnityEngine;
using HanGame.Common;

namespace HanGame.Day
{
    /// <summary>경험치 서류. 플레이어가 닿으면 경험치 지급. 기획서 7.1.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExpPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private float attractRadius = 2.5f;
        [SerializeField] private float attractSpeed = 8f;

        public void SetAmount(int value) => amount = value;

        private void Update()
        {
            // 가까우면 플레이어로 빨려감(수집 편의).
            var p = Player.Local;
            if (p == null) return;
            float d = Vector2.Distance(transform.position, p.Position);
            if (d <= attractRadius)
                transform.position = Vector2.MoveTowards(transform.position, p.Position, attractSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Player>(out _)) return;
            var exp = FindObjectOfType<ExperienceSystem>();
            if (exp != null) exp.AddExp(amount);
            Destroy(gameObject);
        }
    }

    /// <summary>아메리카노. HP 일부 회복. 기획서 6.4. 녹색 아이콘으로 구분(15.2).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class CoffeePickup : MonoBehaviour
    {
        [SerializeField] private float healAmount = 25f;

        public void SetHeal(float amount) => healAmount = amount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Player>(out var player)) return;
            if (player.Health != null) player.Health.Heal(healAmount);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.CoffeeHeal);
            Destroy(gameObject);
        }
    }
}
