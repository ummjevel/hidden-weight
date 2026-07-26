using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 근접 공격 판정. Enemies 모듈은 Task 9에서 만들어지므로 여기서는 참조하지 않는다.
    // 부채꼴 판정에 명중한 대상에게 실제 피해를 적용하는 코드는 Task 9가
    // IDamageable 연동과 함께 이 파일에 추가한다. 이번 태스크는 Attacked 이벤트 발행까지만 한다.
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
            var hits = Physics2D.OverlapCircleAll(transform.position, _data.attackRadius, enemyLayer);
            var facingVec = new Vector2(_controller.Facing, 0f);

            foreach (var hit in hits)
            {
                var toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                if (Vector2.Angle(facingVec, toTarget) <= _data.attackAngle * 0.5f)
                {
                    // 명중 판정만 한다. 피해 적용은 Task 9에서 추가된다.
                }
            }

            Attacked?.Invoke();
        }
    }
}
