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

        // 각도 판정에서 세로로 봐주는 양. 내 몸 절반 높이다.
        float _bodyHalfHeight = 0.7f;

        public bool CanAttack { get; set; } = true;

        public event System.Action Attacked;

        void Awake()
        {
            _data = GameManager.Instance.Balance.player;
            _controller = GetComponent<PlayerController>();

            var body = GetComponent<Collider2D>();
            if (body != null) _bodyHalfHeight = Mathf.Max(0.1f, body.bounds.extents.y);
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

            // **휘두르는 것이 먼저다.**
            //
            // 예전에는 이 줄이 맨 아래, 판정 루프 다음에 있었다. Attacked를 구독하는 쪽이
            // 플레이어를 Attack 상태로 바꾸고 스윙 그림을 재생하므로, 판정 도중 무엇 하나라도
            // 어긋나면 **공격 자체가 화면에 나타나지 않는다** — 눌렀는데 아무 일도 없는 것으로
            // 보인다. 휘두르는 것은 플레이어가 한 일이고, 무엇이 맞았는지는 그 다음 문제다.
            Attacked?.Invoke();

            var hits = Physics2D.OverlapCircleAll(transform.position, _data.attackRadius, enemyLayer);
            var facingVec = new Vector2(_controller.Facing, 0f);
            bool hitAny = false;

            foreach (var hit in hits)
            {
                if (hit == null || !WithinArc(hit, facingVec)) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                damageable.TakeDamage(_data.attackDamage, transform.position);
                hitAny = true;

                // 맞은 자리에 타격 연출을 띄운다. 헛스윙과 적중을 눈으로 구분하게 하는
                // 것이 목적이라, 판정이 실제로 통한 대상에만 띄운다.
                ImpactVFX.Play("ImpactMelee", hit.transform.position, _controller.Facing);
            }

            if (hitAny) AudioManager.Instance?.PlaySfx(SfxCue.AttackHit, 0.58f);
        }

        // 이 적이 휘두른 범위 안에 있는가.
        //
        // 예전에는 두 **중심**을 잇는 벡터의 각도만 봤다(정면 ±45도). 그런데 새싹 같은 낮은
        // 적은 중심이 내 발목 높이에 있어서, 바짝 붙으면 방향 벡터가 거의 "아래쪽 90도"가
        // 된다 — 45도 밖으로 밀려나 **몸이 닿아 피해를 주는 적을 정작 때릴 수 없었다**.
        // 플레이어가 겪는 최악의 상황이 정확히 그 상황이다.
        //
        // 두 가지를 고친다.
        //  - 거리는 콜라이더의 **가장 가까운 점**으로 잰다. 겹쳐 있으면 그 점이 내 위치가
        //    되어 offset이 0이 되고, 방향을 따질 것도 없이 맞는다.
        //  - 세로 어긋남은 **내 몸 절반 높이만큼 봐준다**. 발밑이나 머리 위에 붙은 적을
        //    못 때리는 것은 적을 점으로 보기 때문이지 플레이어가 잘못 겨눈 것이 아니다.
        //
        // 좌우 방향성은 그대로 남는다 — 등 뒤의 적은 여전히 맞지 않는다.
        bool WithinArc(Collider2D hit, Vector2 facingVec)
        {
            var offset = hit.ClosestPoint(transform.position) - (Vector2)transform.position;

            float slackY = Mathf.Sign(offset.y)
                         * Mathf.Max(0f, Mathf.Abs(offset.y) - _bodyHalfHeight);
            var flattened = new Vector2(offset.x, slackY);

            if (flattened.sqrMagnitude <= 0.0001f) return true;   // 몸에 겹쳐 있다
            return Vector2.Angle(facingVec, flattened.normalized) <= _data.attackAngle * 0.5f;
        }
    }
}
