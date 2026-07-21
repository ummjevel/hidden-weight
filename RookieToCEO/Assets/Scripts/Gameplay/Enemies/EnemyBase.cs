using RookieToCEO.Core;
using RookieToCEO.Gameplay.Skills;
using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번(적 구성)의 공통 동작: HP/데미지, 넉백·공포·슬로우 반응, 접촉 데미지,
    // 사망 시 레지스트리 해제 + 퇴사 통보 게이지 충전. 각 적 타입은 이 클래스를 상속해
    // GetMoveDirection()만 다르게 구현하면 된다.
    // 정확한 HP/이동속도/접촉 데미지 수치는 GDD에서도 TBD(M9 밸런싱)라, 기본값은 상대적 감각에
    // 맞춘 임시값이다.
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable, ICrowdControllable
    {
        [SerializeField] protected int maxHp = 20;
        [SerializeField] protected float moveSpeed = 1.5f;
        [SerializeField] protected EnemyCategory category = EnemyCategory.Normal;
        [SerializeField] protected int contactDamage = 10;
        [SerializeField] private float contactDamageInterval = 1f; // 접촉 중 데미지가 매 프레임 들어가지 않도록

        protected int CurrentHp;
        protected Rigidbody2D Rigidbody;
        protected Transform PlayerTransform;

        private Cooldown _contactCooldown;
        private float _fearTimer;
        private float _slowTimer;
        private float _slowMultiplier = 1f;
        private Vector2 _knockbackVelocity;

        public bool IsDead { get; private set; }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            CurrentHp = maxHp;
            _contactCooldown = new Cooldown(contactDamageInterval);
        }

        protected virtual void Start()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) PlayerTransform = player.transform;

            EnemyRegistry.Instance?.Register(transform, this, category);
        }

        protected virtual void OnDestroy()
        {
            EnemyRegistry.Instance?.Unregister(transform);
        }

        protected virtual void Update()
        {
            _contactCooldown.Tick(Time.deltaTime);
            TickCrowdControl(Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead) return;

            var moveVector = ComputeMoveVector() * (moveSpeed * _slowMultiplier) + _knockbackVelocity;
            Rigidbody.MovePosition(Rigidbody.position + moveVector * Time.fixedDeltaTime);
            _knockbackVelocity = Vector2.Lerp(_knockbackVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
        }

        // 공포 상태면 플레이어 반대 방향으로 도망(GDD 3번 퇴사 통보), 아니면 각 적 타입의 이동 방식을 따른다.
        private Vector2 ComputeMoveVector()
        {
            if (_fearTimer > 0f && PlayerTransform != null)
            {
                var awayFromPlayer = (Vector2)transform.position - (Vector2)PlayerTransform.position;
                return awayFromPlayer.sqrMagnitude > 0f ? awayFromPlayer.normalized : Vector2.zero;
            }

            return GetMoveDirection();
        }

        // 공포 상태가 아닐 때의 이동 방향(정규화 여부는 하위 클래스 재량 - 돌진형은 배율을 실어 반환한다).
        protected abstract Vector2 GetMoveDirection();

        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            CurrentHp -= amount;
            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // 적을 처치하면 퇴사 통보 게이지가 충전되고(GDD 3번) 경험치를 얻는다(GDD 4번).
            // "경험치 서류를 드롭 -> 가까이 가서 획득"하는 물리적 줍기 단계는 M9 폴리싱에서 추가할 수
            // 있고, 지금은 코어 루프(레벨업 -> 3택1)를 검증하기 위해 즉시 지급하는 방식으로 단순화했다.
            // 충전량/경험치량 모두 TBD(M9)라 임시값 사용.
            var player = PlayerTransform != null ? PlayerTransform.GetComponent<PlayerController>() : null;
            player?.GetComponent<ResignationUltimate>()?.AddGaugeOnKill(10f);
            player?.Level.AddXp(10f);

            Destroy(gameObject);
        }

        public void ApplyFear(float duration)
        {
            _fearTimer = Mathf.Max(_fearTimer, duration);
        }

        public void ApplySlow(float duration, float slowMultiplier)
        {
            _slowTimer = Mathf.Max(_slowTimer, duration);
            _slowMultiplier = slowMultiplier;
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            _knockbackVelocity = direction * force;
        }

        private void TickCrowdControl(float deltaTime)
        {
            if (_fearTimer > 0f)
            {
                _fearTimer -= deltaTime;
            }

            if (_slowTimer > 0f)
            {
                _slowTimer -= deltaTime;
                if (_slowTimer <= 0f) _slowMultiplier = 1f;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!_contactCooldown.IsReady) return;

            var player = collision.collider.GetComponent<PlayerController>();
            if (player == null) return;

            player.Reputation.TakeDamage(contactDamage);
            _contactCooldown.TryUse();
        }
    }
}
