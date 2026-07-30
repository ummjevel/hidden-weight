using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 중심점을 도는 발판. 균열 F10 "시계바늘 발판"과 F07의 수직 순환 발판이 쓴다
    // (FRACTURE_LEVEL_DESIGN.md 4.10절 "일정한 주기로 회전해 학습 가능해야 한다").
    //
    // 위치를 시간의 순수 함수로 둔다. MovingPlatform과 같은 이유다 — 예지 고스트가
    // "정확히 2초 뒤"를 가리켜야 하고(7.2절 공정성 규칙), 사망 후에는 항상 같은 위상으로
    // 돌아와야 실패에서 배울 수 있다(10절). Time.time만 쓰면 두 조건이 동시에 만족된다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class OrbitPlatform : MonoBehaviour, IForeseeable
    {
        [SerializeField] Vector2 pivot;             // 시작 위치 기준 상대 중심
        [SerializeField] float degreesPerSecond = 45f;
        [SerializeField] float phaseOffset = 0f;    // 여러 바늘의 시작 각도를 어긋나게 한다

        Rigidbody2D _rb;
        SpriteRenderer _sprite;
        BoxCollider2D _box;
        Vector3 _center;
        float _radius;
        float _startAngle;
        int _riderMask;

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
            _riderMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");

            _center = transform.position + (Vector3)pivot;
            var arm = transform.position - _center;
            _radius = arm.magnitude;
            _startAngle = Mathf.Atan2(arm.y, arm.x) * Mathf.Rad2Deg;
        }

        Vector3 PositionAt(float time)
        {
            float angle = (_startAngle + phaseOffset + degreesPerSecond * time) * Mathf.Deg2Rad;
            return _center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _radius;
        }

        void FixedUpdate()
        {
            var next = PositionAt(Time.time);
            var delta = next - transform.position;
            _rb.MovePosition(next);
            CarryRiders(delta);
        }

        // 발판 바로 위를 매 스텝 훑어 탑승자를 옮긴다. 이 발판은 위아래로도 움직이므로
        // LiftPlatform과 같은 이유로 충돌 이벤트에 기대지 않는다(주석은 그쪽 참고).
        void CarryRiders(Vector3 delta)
        {
            if (delta.sqrMagnitude <= 0f) return;

            var size = _box != null ? _box.size : new Vector2(3f, 0.5f);
            var center = (Vector2)transform.position + new Vector2(0f, size.y * 0.5f + 0.45f);
            var area = new Vector2(size.x * 0.95f, 1f);

            foreach (var hit in Physics2D.OverlapBoxAll(center, area, 0f, _riderMask))
                hit.transform.position += delta;
        }

        public Vector3 PredictPosition(float leadSeconds) => PositionAt(Time.time + leadSeconds);
        public bool PredictActive(float leadSeconds) => true;

    }
}
