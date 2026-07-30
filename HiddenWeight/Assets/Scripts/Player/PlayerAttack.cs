using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Core;
using HiddenWeight.World;

namespace HiddenWeight.Player
{
    // 근접 공격 판정. 피해 적용은 IDamageable(World/Interactions.cs)만 참조한다 — 적 모듈의
    // 구현 타입은 절대 참조하지 않는다. Interactions.cs는 어떤 모듈에도 의존하지 않는 순수
    // 계약 파일이라 Player가 참조해도 World → Player 의존 방향(설계 문서 3.1절)이 깨지지 않는다.
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] LayerMask enemyLayer;

        PlayerData _data;
        PlayerController _controller;
        float _cooldownTimer;

        public bool CanAttack { get; set; } = true;

        public event System.Action Attacked;

        void Awake()
        {
            _data = GameManager.Instance.Balance.player;
            _controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            if (!CanAttack || _cooldownTimer > 0f || !PlayerInput.AttackPressed) return;

            _cooldownTimer = _data.attackCooldown;
            PerformAttack();
        }

        void PerformAttack()
        {
            AudioManager.Instance?.PlaySfx(SfxCue.Attack, 0.55f);
            var hits = Physics2D.OverlapCircleAll(transform.position, _data.attackRadius, enemyLayer);
            var facingVec = new Vector2(_controller.Facing, 0f);

            foreach (var hit in hits)
            {
                var toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                if (Vector2.Angle(facingVec, toTarget) <= _data.attackAngle * 0.5f)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null && damageable.IsAlive)
                    {
                        damageable.TakeDamage(_data.attackDamage, transform.position);

                        // 맞은 자리에 타격 연출을 띄운다. 헛스윙과 적중을 눈으로 구분하게 하는
                        // 것이 목적이라, 판정이 실제로 통한 대상에만 띄운다.
                        ImpactVFX.Play("ImpactMelee", hit.transform.position, _controller.Facing);
                    }
                }
            }

            Attacked?.Invoke();
        }
    }
}
