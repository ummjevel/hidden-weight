using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 웨이포인트를 따라 한 번 올라가는 승강기. 응시 G08 "시선 승강정"과 균열 F08 "역행 승강축"이
    // 같은 컴포넌트를 쓰고 경로만 다르게 준다 — 균열 쪽은 첫 웨이포인트를 아래로 두면 그대로
    // "먼저 내려갔다가 올라가는" 역행 승강기가 된다(FRACTURE_LEVEL_DESIGN.md 4.8절).
    //
    // MovingPlatform과 나누는 이유: 저쪽은 시간만으로 위치가 정해지는 무한 왕복이고, 이쪽은
    // 플레이어가 올라타야 시작해서 끝에서 멈추고 숏컷을 여는 1회성 장치다. 두 성질을 한
    // 컴포넌트에 넣으면 예지 예측식이 상태에 의존하게 되어 고스트가 어긋난다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class LiftPlatform : MonoBehaviour, IForeseeable
    {
        [Tooltip("시작 위치 기준 상대 좌표. 순서대로 이동한다.")]
        [SerializeField] Vector2[] waypoints = { new Vector2(0f, 10f) };
        [SerializeField] float speed = 3f;
        [SerializeField] float startDelay = 0.6f;   // 올라탄 뒤 출발까지. 예지를 쓸 시간을 준다
        [SerializeField] Shortcut linkedShortcut;   // 종점에 닿으면 열린다(숏컷 B)

        Rigidbody2D _rb;
        SpriteRenderer _sprite;
        BoxCollider2D _box;
        Vector3 _origin;
        int _riderMask;

        int _leg = -1;      // -1이면 아직 출발 전
        float _delayTimer;
        bool _finished;

        public bool IsRunning => _leg >= 0 && !_finished;
        public bool IsFinished => _finished;

        public Transform Transform => transform;
        public Sprite CurrentSprite => _sprite != null ? _sprite.sprite : null;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            // 지역 아트는 루트를 끄고 자식에 그린다(ApplyPlatformArt). 예지 고스트가
            // 실제 보이는 그림을 복사하도록 보이는 렌더러를 잡는다.
            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite == null || !_sprite.enabled)
                foreach (var candidate in GetComponentsInChildren<SpriteRenderer>())
                    if (candidate.enabled) { _sprite = candidate; break; }
            _box = GetComponent<BoxCollider2D>();
            _origin = transform.position;
            _riderMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");
        }

        // 발판 바로 위를 매 스텝 훑어 탑승자를 옮긴다.
        //
        // MovingPlatform처럼 OnCollisionEnter2D/Exit2D로 탑승자를 기억하는 방식은 수평 왕복에는
        // 통하지만 수직 승강에는 통하지 않는다. 위로 올라가는 동안 발판이 플레이어를 아래에서
        // 밀어 올리느라 접촉이 끊겼다 붙었다를 반복하고, Exit가 한 번 뜨는 순간 탑승자를 잊어
        // 발판만 혼자 올라가 버린다 — 봇이 승강기 중간에서 계속 떨어졌던 원인이다.
        void CarryRiders(Vector3 delta)
        {
            if (delta.sqrMagnitude <= 0f) return;

            var size = _box != null ? _box.size : new Vector2(3f, 0.5f);
            var center = (Vector2)transform.position + new Vector2(0f, size.y * 0.5f + 0.45f);
            var area = new Vector2(size.x * 0.95f, 1f);

            foreach (var hit in Physics2D.OverlapBoxAll(center, area, 0f, _riderMask))
                hit.transform.position += delta;
        }

        Vector3 Target(int leg)
            => _origin + (Vector3)waypoints[Mathf.Clamp(leg, 0, waypoints.Length - 1)];

        void FixedUpdate()
        {
            if (_finished || _leg < 0) return;

            if (_delayTimer > 0f)
            {
                _delayTimer -= Time.fixedDeltaTime;
                return;
            }

            var target = Target(_leg);
            var next = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);
            var delta = next - transform.position;

            _rb.MovePosition(next);
            CarryRiders(delta);

            if ((next - target).sqrMagnitude > 0.0001f) return;

            _leg++;
            if (_leg < waypoints.Length) return;

            // 종점 도착. 여기서만 숏컷이 열린다 — 도중에 내려도 열리지 않아야
            // "승강기를 상층까지 작동시키면"이라는 조건이 성립한다.
            _finished = true;
            _leg = waypoints.Length - 1;
            if (linkedShortcut != null) linkedShortcut.Open();
        }

        // 예지: 남은 경로를 그대로 따라가 lead초 뒤 위치를 계산한다. 아직 출발 전이면
        // 제자리 — "타면 어디로 갈지"가 아니라 "곧 어디에 있을지"를 보여주는 것이 규칙이다.
        public Vector3 PredictPosition(float leadSeconds)
        {
            if (_finished || _leg < 0) return transform.position;

            var position = transform.position;
            float remaining = leadSeconds - Mathf.Max(0f, _delayTimer);
            for (int leg = _leg; leg < waypoints.Length && remaining > 0f; leg++)
            {
                var target = Target(leg);
                float distance = Vector3.Distance(position, target);
                float travel = speed * remaining;
                if (travel < distance) return Vector3.MoveTowards(position, target, travel);

                position = target;
                remaining -= distance / speed;
            }
            return position;
        }

        public bool PredictActive(float leadSeconds) => true;

        // 출발은 "위에서 밟았을 때" 한 번만 판단하면 되므로 충돌 이벤트를 그대로 쓴다.
        // 탑승자 운반만 CarryRiders가 따로 맡는다.
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_leg >= 0 || _finished) return;
            if (!PlayerLayers.SteppedOnFromAbove(collision, transform)) return;

            _leg = 0;
            _delayTimer = startDelay;
        }
    }
}
