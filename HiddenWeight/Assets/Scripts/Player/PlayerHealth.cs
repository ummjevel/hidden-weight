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

        void StartInvulnerability()
        {
            if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
            _blinkRoutine = StartCoroutine(InvulnerabilityRoutine());
        }

        IEnumerator InvulnerabilityRoutine()
        {
            IsInvulnerable = true;
            float elapsed = 0f;

            while (elapsed < _invulnerableTime)
            {
                if (_sprite != null) _sprite.enabled = !_sprite.enabled;
                yield return new WaitForSeconds(_blinkInterval);
                elapsed += _blinkInterval;
            }

            if (_sprite != null) _sprite.enabled = true;
            IsInvulnerable = false;
            _blinkRoutine = null;
        }
    }
}
