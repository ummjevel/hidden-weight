using System.Collections;
using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.Enemies
{
    // 보스 전장 관리자(RESIDUE_ROOM_IMPLEMENTATION.md 2.2절). R10 '손목의 감시자'와
    // R12 '기억의 교수자'가 같은 컴포넌트를 쓰고 단계 구성만 다르게 한다.
    //
    // 설계 원칙(R10·R12 명세 공통): 난도는 공격 속도가 아니라 조합 순서로 올린다.
    // 그래서 단계가 올라가도 telegraphSeconds는 절대 줄이지 않고, 대신 연속 사용 횟수만 늘린다.
    // "첫 도전에서도 각 공격을 최소 한 번은 보고 살아남을 수 있어야 한다"가 검증 항목이기 때문이다.
    [RequireComponent(typeof(Enemy))]
    public class BossController : MonoBehaviour
    {
        public enum Move { GroundSweep, Charge, Slam }

        [SerializeField] Move[] moves = { Move.GroundSweep, Move.Charge, Move.Slam };
        [SerializeField] float telegraphSeconds = 0.9f;
        [SerializeField] float recoverSeconds = 1.0f;
        [SerializeField] float sweepRange = 5f;
        [SerializeField] float chargeSpeed = 12f;
        [SerializeField] float slamHeight = 6f;
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask obstacleMask;

        // 체력 비율이 이 값 아래로 내려가면 다음 단계. 명세의 R12 3단계(1.0 → 0.6 → 0.3)를 기본값으로.
        [SerializeField] float[] phaseThresholds = { 0.6f, 0.3f };

        Enemy _self;
        Rigidbody2D _body;
        SpriteRenderer _sprite;
        int _moveIndex;

        public int Phase { get; private set; }

        void Awake()
        {
            _self = GetComponent<Enemy>();
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable() => StartCoroutine(FightRoutine());

        IEnumerator FightRoutine()
        {
            // 입장 직후 한 박자 쉰다 — 명세의 "입장 후 2초간 보스를 관찰"과 짝을 이룬다.
            yield return new WaitForSeconds(1f);

            while (_self.IsAlive)
            {
                UpdatePhase();

                // 단계가 오를수록 한 번에 잇는 공격 수만 늘린다(1 → 2 → 2).
                int combo = Phase == 0 ? 1 : 2;
                for (int i = 0; i < combo && _self.IsAlive; i++)
                {
                    yield return PerformMove(moves[_moveIndex % moves.Length]);
                    _moveIndex++;
                }

                yield return new WaitForSeconds(recoverSeconds);
            }
        }

        void UpdatePhase()
        {
            float ratio = _self.Data.maxHealth <= 0 ? 1f : (float)_self.Health / _self.Data.maxHealth;
            int phase = 0;
            for (int i = 0; i < phaseThresholds.Length; i++)
                if (ratio <= phaseThresholds[i]) phase = i + 1;
            Phase = phase;
        }

        IEnumerator PerformMove(Move move)
        {
            var player = PlayerController.Instance;
            if (player == null) yield break;

            // 예고는 어떤 단계에서도 같은 길이다.
            Telegraph(true);
            yield return new WaitForSeconds(telegraphSeconds);
            Telegraph(false);

            switch (move)
            {
                case Move.GroundSweep:
                    // 지상 쓸기 — 점프나 벽점프로 넘는다.
                    HitPlayersInCircle(transform.position, sweepRange);
                    break;

                case Move.Charge:
                {
                    // 감시탑 돌진 — 복원벽 뒤로 피하면 벽에 박고 큰 빈틈이 생긴다.
                    int dir = player.transform.position.x >= transform.position.x ? 1 : -1;
                    float elapsed = 0f;
                    while (elapsed < 1.2f)
                    {
                        _body.linearVelocity = new Vector2(dir * chargeSpeed, _body.linearVelocity.y);
                        if (Physics2D.Raycast(transform.position, Vector2.right * dir, 1f, obstacleMask))
                        {
                            _body.linearVelocity = Vector2.zero;
                            Telegraph(true);          // 경직도 눈에 보이게
                            yield return new WaitForSeconds(1.8f);
                            Telegraph(false);
                            yield break;
                        }
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                    break;
                }

                case Move.Slam:
                {
                    // 상부 낙하 — 그림자를 보고 좌우로 비킨다. 예고 동안 위치가 고정이라 확실히 피할 수 있다.
                    var target = new Vector3(player.transform.position.x, transform.position.y + slamHeight, 0f);
                    transform.position = target;
                    yield return new WaitForSeconds(0.6f);
                    _body.linearVelocity = new Vector2(0f, -18f);
                    yield return new WaitForSeconds(0.5f);
                    HitPlayersInCircle(transform.position, sweepRange * 0.8f);
                    break;
                }
            }
        }

        void HitPlayersInCircle(Vector2 center, float radius)
        {
            var hit = Physics2D.OverlapCircle(center, radius, playerMask);
            if (hit == null) return;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(_self.Data.contactDamage, center);
        }

        void Telegraph(bool on)
        {
            if (_sprite == null) return;
            _sprite.color = on ? Color.Lerp(_self.Data.tint, Color.white, 0.7f) : _self.Data.tint;
        }
    }
}
