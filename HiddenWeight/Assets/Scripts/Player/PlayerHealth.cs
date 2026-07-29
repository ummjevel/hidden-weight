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
            // 영구 성장 조각 1개당 최대 체력 +1 (CONTENT_SYSTEM.md 5절). 조각은 ProgressState가
            // 들고 있으므로 지역을 옮겨도 유지된다.
            _maxHealth = data.maxHealth + GameManager.Instance.Progress.HealthShards;
            _invulnerableTime = data.invulnerableTime;
            _blinkInterval = data.blinkInterval;
            _knockbackForce = data.knockbackForce;
            // 점멸시킬 대상도 "보이는 렌더러"여야 한다. 루트의 꺼진 구형 렌더러를 켰다 껐다 하면
            // 점멸이 끝나는 순간 그 그림이 켜진 채로 남아, 정상 캐릭터 옆에 옛 그림이 함께 보인다.
            var animator = GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
            _sprite = animator != null && animator.Renderer != null
                ? animator.Renderer
                : GetComponentInChildren<SpriteRenderer>();
            Current = _maxHealth;
        }

        void Start()
        {
            // 아직 체크포인트를 밟지 않았다면 이 방의 시작 위치를 기본 복귀 지점으로 삼는다.
            // 그러지 않으면 LastCheckpoint가 (0,0)이라, 체크포인트가 없는 방에서 죽는 순간
            // 원점으로 순간이동해 지형 안에 끼거나 허공에서 떨어진다(R03에서 실제로 재현됐다).
            var progress = GameManager.Instance.Progress;
            if (progress.LastCheckpoint == Vector3.zero) progress.LastCheckpoint = transform.position;
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
            AudioManager.Instance?.PlaySfx(SfxCue.Hurt, 0.65f);

            var direction = ((Vector2)transform.position - sourcePosition).normalized;
            PlayerController.Instance.ApplyKnockback(direction, _knockbackForce);

            StartInvulnerability();

            if (Current <= 0)
            {
                Animator?.PlayOnce("PlayerDeath");

                // 게임오버 화면은 없다. 마지막 체크포인트로 되돌린다.
                GameManager.Instance.RespawnPlayer();
                return;
            }

            Animator?.PlayOnce("PlayerHit");
        }

        // 성장 조각을 먹은 즉시 최대 체력을 올린다. 다음 씬까지 기다리면 보상을 받은 체감이 없다.
        public void RefreshMaxHealth()
        {
            int max = GameManager.Instance.Balance.player.maxHealth
                    + GameManager.Instance.Progress.HealthShards;
            if (max == _maxHealth) return;

            int gained = max - _maxHealth;
            _maxHealth = max;
            Current = Mathf.Min(_maxHealth, Current + Mathf.Max(0, gained)); // 늘어난 만큼 채워 준다
            HealthChanged?.Invoke(Current, _maxHealth);
        }

        // 소형 회복물용. 최대치를 넘지 않는다.
        public void Heal(int amount)
        {
            if (amount <= 0 || Current >= _maxHealth) return;

            Current = Mathf.Min(_maxHealth, Current + amount);
            HealthChanged?.Invoke(Current, _maxHealth);
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
            Animator?.PlayOnce("PlayerRespawn");
        }

        // 반응 VFX(피격·사망·리스폰)는 PlayerVFX_v1 시트의 클립이다. 상태가 아니라 사건이라
        // 상태 클립 위에 덮어쓴다 — 자세한 규칙은 PlayerAnimator의 덮어쓰기 계층 주석 참고.
        PlayerAnimator _animator;
        PlayerAnimator Animator
            => _animator != null ? _animator : (_animator = GetComponent<PlayerAnimator>());

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
