using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Enemies
{
    // 지형 위 왕복 순찰. 낭떠러지·벽을 만나면 방향을 바꾼다.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Enemy))]
    public class EnemyPatrol : MonoBehaviour
    {
        [SerializeField] Transform edgeCheck; // 발끝 앞쪽에 둔 빈 오브젝트
        [SerializeField] LayerMask groundMask;

        Rigidbody2D _rb;
        EnemyData _data;
        Enemy _enemy;
        int _dir = 1;

        // 방향을 바꾼 뒤 최소한 이만큼은 그 방향으로 걷는다. 폭이 좁은 발판 위에서는 양옆이
        // 모두 낭떠러지라 판정이 매 프레임 참이 되고, 쿨다운이 없으면 초당 50번 뒤집혀
        // 제자리에서 떠는 것처럼 보인다(실제로 4초에 200번 뒤집는 개체가 있었다).
        const float FlipCooldown = 0.4f;
        float _flipTimer;

        // 벽 판정은 Ground만 봐서는 안 된다. 방 경계벽·굴뚝·전투 잠금벽은 전부 "Wall" 레이어라,
        // Ground만 보면 적이 벽에 붙어 밀고만 있고 되돌지 못한다(실측: 8마리 중 5마리가
        // 4초 동안 이동폭 0으로 멈춰 있었다).
        int _obstacleMask;

        // 낭떠러지·벽 판정으로 못 잡는 상황(판정이 닿지 않는 지형 모서리, 겹쳐 놓인 오브젝트 등)에
        // 걸려 밀고만 있는 개체가 실제로 있었다. 원인별로 쫓는 대신 "앞으로 못 가면 돌아선다"는
        // 규칙을 둔다 — 어떤 이유로 막히든 스스로 빠져나온다.
        // 한 물리 스텝(0.02초)에 정상 보행은 0.03유닛밖에 못 간다. 스텝 단위로 비교하면
        // 멀쩡히 걷는 적도 전부 "막힘"으로 오판한다. 0.5초 구간을 통째로 보고 판단한다
        // (정상 보행이면 그 사이 0.75유닛은 간다).
        const float StuckSeconds = 0.5f;
        const float StuckWindowDistance = 0.3f;
        float _stuckTimer;
        float _stuckAnchorX;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<Enemy>();
            _data = _enemy.Data;
            _obstacleMask = groundMask.value | (1 << LayerMask.NameToLayer("Wall"));
            _stuckAnchorX = transform.position.x;
        }

        void FixedUpdate()
        {
            if (_flipTimer > 0f) _flipTimer -= Time.fixedDeltaTime;

            bool groundAhead = Physics2D.OverlapCircle(edgeCheck.position, 0.1f, groundMask);
            // 벽 판정은 몸통 옆면에서 쏜다. 중심에서 쏘면 지형에 살짝 파고든 순간 양쪽 다
            // 벽으로 읽혀 역시 매 프레임 뒤집힌다.
            var origin = (Vector2)transform.position + new Vector2(0f, 0.15f);
            bool wallAhead = Physics2D.Raycast(origin, Vector2.right * _dir, 0.6f, _obstacleMask);

            // 진행 정체 감지: 0.5초 구간 동안 거의 못 나아갔으면 막힌 것이다.
            // 물리 스텝 단위로 비교하면 안 된다 — 정상 보행도 한 스텝에 0.03유닛뿐이라
            // 멀쩡한 적까지 전부 "막힘"으로 오판한다.
            bool stuck = false;
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer >= StuckSeconds)
            {
                stuck = Mathf.Abs(transform.position.x - _stuckAnchorX) < StuckWindowDistance;
                _stuckTimer = 0f;
                _stuckAnchorX = transform.position.x;
            }

            if ((!groundAhead || wallAhead || stuck) && _flipTimer <= 0f)
            {
                Flip();
                _flipTimer = FlipCooldown;
                _stuckTimer = 0f;
                _stuckAnchorX = transform.position.x;
            }

            float wobble = _data.wobbleAmplitude <= 0f
                ? 0f
                : Mathf.Sin(Time.time * _data.wobbleFrequency) * _data.wobbleAmplitude;

            _rb.linearVelocity = new Vector2(_dir * _data.moveSpeed, _rb.linearVelocity.y + wobble * Time.fixedDeltaTime);
            if (_enemy != null) _enemy.PlayClip(Mathf.Abs(_data.moveSpeed) > 0.05f ? "Walk" : "Idle");
        }

        void Flip()
        {
            _dir = -_dir;

            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * _dir;
            transform.localScale = scale;

            // edgeCheck.localPosition은 건드리지 않는다. 루트 스케일을 뒤집으면 자식 위치도
            // 함께 미러링되므로, 여기서 또 뒤집으면 두 번 뒤집혀 원래 쪽으로 돌아온다 —
            // 그러면 낭떠러지 판정이 늘 진행 방향 반대편을 보게 된다.
        }
    }
}
