using System.Collections;
using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 플레이어 체력, 무적 시간, 피격 넉백·점멸을 담당한다.
    // 리스폰 위치 결정은 Core(GameManager)가 하고, 이 클래스는 GameManager.RespawnRequested를
    // 구독하기만 한다 — Core를 직접 호출해 위치를 요청하지 않는 역방향 훅 구조.
    public class PlayerHealth : MonoBehaviour
    {
        int _maxHealth;
        float _invulnerableTime;
        float _blinkInterval;
        float _knockbackForce;

        SpriteRenderer _sprite;
        Coroutine _blinkRoutine;

        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsInvulnerable { get; private set; }

        public event System.Action<int, int> HealthChanged;

        // 실제로 피해가 들어간 순간(무적으로 흡수된 피격은 제외). 되감기 채널링 캔슬처럼
        // "맞았다"에 반응해야 하는 쪽(Emotions)이 구독한다.
        public event System.Action Damaged;

        void Awake()
        {
            var data = GameManager.Instance.Balance.player;
            _maxHealth = data.maxHealth;
            _invulnerableTime = data.invulnerableTime;
            _blinkInterval = data.blinkInterval;
            _knockbackForce = data.knockbackForce;
            _sprite = GetComponentInChildren<SpriteRenderer>();
            Current = _maxHealth;
        }

        void OnEnable()
        {
            GameManager.Instance.RespawnRequested += HandleRespawn;
        }

        void OnDisable()
        {
            // 종료/파괴 순서에 따라 GameManager가 먼저 사라져 있을 수 있으므로 널 체크 후 해제한다.
            if (GameManager.Instance != null) GameManager.Instance.RespawnRequested -= HandleRespawn;
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (IsInvulnerable) return;

            Current = Mathf.Max(0, Current - amount);
            HealthChanged?.Invoke(Current, _maxHealth);
            Damaged?.Invoke();

            var direction = ((Vector2)transform.position - sourcePosition).normalized;
            PlayerController.Instance.ApplyKnockback(direction, _knockbackForce);

            StartInvulnerability();

            if (Current <= 0)
            {
                // 게임오버 화면은 없다. 마지막 체크포인트로 되돌린다.
                GameManager.Instance.RespawnPlayer();
            }
        }

        public void RestoreFull()
        {
            Current = _maxHealth;
            HealthChanged?.Invoke(Current, _maxHealth);
        }

        void HandleRespawn(Vector3 position)
        {
            PlayerController.Instance.TeleportTo(position);
            RestoreFull();
        }

        // 피격 외의 이유(숨죽이기 해제 등)로 짧은 무적을 걸 때 쓴다. 이미 더 긴 무적이
        // 돌고 있으면 남은 시간을 줄이지 않는다.
        public void GrantInvulnerability(float seconds) => StartInvulnerability(seconds);

        void StartInvulnerability() => StartInvulnerability(_invulnerableTime);

        void StartInvulnerability(float duration)
        {
            _invulnRemaining = Mathf.Max(_invulnRemaining, duration);
            if (_blinkRoutine == null) _blinkRoutine = StartCoroutine(InvulnerabilityRoutine());
        }

        float _invulnRemaining;

        IEnumerator InvulnerabilityRoutine()
        {
            IsInvulnerable = true;

            while (_invulnRemaining > 0f)
            {
                if (_sprite != null) _sprite.enabled = !_sprite.enabled;
                yield return new WaitForSeconds(_blinkInterval);
                _invulnRemaining -= _blinkInterval;
            }

            _invulnRemaining = 0f;
            if (_sprite != null) _sprite.enabled = true;
            IsInvulnerable = false;
            _blinkRoutine = null;
        }
    }
}
