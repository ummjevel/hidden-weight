using System;
using System.Collections;
using UnityEngine;

namespace HanGame.Common
{
    /// <summary>
    /// 플레이어 HP(멘탈)와 평판 관리. 기획서 6장.
    /// HP 0 → 평판 1 감소 → 2초 후 부활 → 전체 회복 → 3초 무적(6.3).
    /// 평판 0 → 해고(3.2). 피격 후 0.5초 무적(4.4).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("HP (멘탈)")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float hitInvulnSeconds = 0.5f;

        [Header("부활")]
        [SerializeField] private float reviveDelay = 2f;
        [SerializeField] private float reviveInvulnSeconds = 3f;

        public float MaxHp => _maxHp;
        public float Hp { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get; private set; }

        // UI 구독용 이벤트.
        public event Action<float, float> HpChanged;      // (current, max)
        public event Action<int> ReputationChanged;       // 남은 평판
        public event Action Revived;
        public event Action Fired;

        private float _maxHp;
        private float _invulnTimer;

        private RunState Run => GameManager.Instance != null ? GameManager.Instance.Run : null;

        private void Awake()
        {
            _maxHp = maxHp;
            Hp = _maxHp;
        }

        private void Start()
        {
            HpChanged?.Invoke(Hp, _maxHp);
            if (Run != null) ReputationChanged?.Invoke(Run.Reputation);
        }

        private void Update()
        {
            if (_invulnTimer > 0f)
            {
                _invulnTimer -= Time.deltaTime;
                if (_invulnTimer <= 0f) IsInvulnerable = false;
            }
        }

        /// <summary>'멘탈 관리' 강화 등에서 최대 HP 증가 + 일부 회복.</summary>
        public void IncreaseMaxHp(float percent, float healPercentOfMax)
        {
            _maxHp *= 1f + percent;
            Heal(_maxHp * healPercentOfMax);
            HpChanged?.Invoke(Hp, _maxHp);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(_maxHp, Hp + amount);
            HpChanged?.Invoke(Hp, _maxHp);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || IsInvulnerable || amount <= 0f) return;

            Hp = Mathf.Max(0f, Hp - amount);
            HpChanged?.Invoke(Hp, _maxHp);

            _invulnTimer = hitInvulnSeconds;
            IsInvulnerable = true;

            if (Hp <= 0f) StartCoroutine(OnHpDepleted());
        }

        private IEnumerator OnHpDepleted()
        {
            IsDead = true;

            // 평판 1 감소.
            if (Run != null)
            {
                Run.Reputation = Mathf.Max(0, Run.Reputation - 1);
                ReputationChanged?.Invoke(Run.Reputation);

                if (Run.Reputation <= 0)
                {
                    Fired?.Invoke();
                    GameManager.Instance.OnFired();
                    yield break;
                }
            }

            // 2초 후 제자리 부활.
            yield return new WaitForSeconds(reviveDelay);

            Hp = _maxHp;
            IsDead = false;
            HpChanged?.Invoke(Hp, _maxHp);

            // 부활 무적.
            _invulnTimer = reviveInvulnSeconds;
            IsInvulnerable = true;
            Revived?.Invoke();
        }
    }
}
