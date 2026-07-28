using System.Collections;
using UnityEngine;

namespace HiddenWeight.Enemies
{
    // 돌진형 — "애도 운반자"(CONTENT_SYSTEM.md 4절). 긴 다리·직선 통로에 놓는다.
    //
    // 명세(R07): "돌진하기 전 0.8초 몸을 뒤로 젖히고 소리를 낸다", "복원 가능한 낮은 벽에
    // 충돌시키면 1.5초 경직". 즉 이 적의 공략은 회피가 아니라 유도다 — 벽에 박게 만들면
    // 큰 빈틈이 생긴다. 그래서 돌진 중 지형 충돌을 직접 검사해 경직에 넣는다.
    public class ChargerBehavior : EnemyBehavior
    {
        [SerializeField] LayerMask obstacleMask;

        enum Phase { Idle, Telegraph, Charge, Stun, Recover }

        Phase _phase = Phase.Idle;
        int _chargeDirection = 1;
        EnemyPatrol _patrol;

        public bool IsStunned => _phase == Phase.Stun;

        protected override void Awake()
        {
            base.Awake();
            _patrol = GetComponent<EnemyPatrol>();
        }

        void Update()
        {
            if (_phase != Phase.Idle) return;
            if (DistanceToPlayer > Data.detectRange) return;

            // 높이가 크게 어긋나면 돌진해도 닿지 않는다. 같은 층일 때만 시작한다.
            if (Player != null && Mathf.Abs(Player.position.y - transform.position.y) > 1.5f) return;

            StartCoroutine(ChargeRoutine());
        }

        IEnumerator ChargeRoutine()
        {
            // EnemyPatrol은 FixedUpdate에서 매 스텝 속도를 순찰 속도로 되돌리므로, 켜 둔 채로
            // 이 코루틴이 속도를 건드리면 서로 덮어써서 떤다(StalkerBehavior.cs의 경고와 동일 상황).
            if (_patrol != null) _patrol.enabled = false;

            _phase = Phase.Telegraph;
            _chargeDirection = DirectionToPlayer;
            FaceTowards(_chargeDirection);

            // 예고: 몸을 뒤로 젖힌다(반대 방향으로 살짝 물러난다).
            ShowTelegraph(true);
            Body.linearVelocity = new Vector2(-_chargeDirection * Data.moveSpeed * 0.5f, Body.linearVelocity.y);
            yield return new WaitForSeconds(Data.telegraphSeconds);
            ShowTelegraph(false);

            _phase = Phase.Charge;
            float elapsed = 0f;
            while (elapsed < Data.chargeMaxSeconds)
            {
                Body.linearVelocity = new Vector2(_chargeDirection * Data.chargeSpeed, Body.linearVelocity.y);

                // 벽에 박으면 큰 빈틈. 플레이어가 만들어 낸 기회다.
                if (Physics2D.Raycast(transform.position, Vector2.right * _chargeDirection, 0.6f, obstacleMask))
                {
                    yield return StunRoutine();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _phase = Phase.Recover;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
            yield return new WaitForSeconds(Data.recoverSeconds);
            ReturnToIdle();
        }

        IEnumerator StunRoutine()
        {
            _phase = Phase.Stun;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
            ShowTelegraph(true); // 경직도 눈에 보여야 공격 기회로 읽힌다
            yield return new WaitForSeconds(Data.stunSeconds);
            ShowTelegraph(false);
            ReturnToIdle();
        }

        void ReturnToIdle()
        {
            _phase = Phase.Idle;

            // 돌진 중 FaceTowards()가 스케일(보이는 방향)만 바꿔 놨으므로, 순찰을 다시 켜기 전에
            // 내부 방향(_dir)도 맞춰 준다 — 안 그러면 재개 첫 프레임에 반대 방향으로 미끄러진다.
            if (_patrol != null)
            {
                _patrol.SyncDirection(_chargeDirection);
                _patrol.enabled = true;
            }
        }
    }
}
