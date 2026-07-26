using UnityEngine;

namespace HiddenWeight.World
{
    // 왕복 이동 발판. 위치를 시간 기반 순수 함수로 계산해, 예지(균열)가 미래 위치를
    // 정확히 예측할 수 있게 한다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour, IForeseeable
    {
        [SerializeField] Vector2 offset = new Vector2(6, 0); // 시작점 기준 왕복 끝점
        [SerializeField] float period = 4f;                  // 왕복 1회 주기

        Rigidbody2D _rb;
        SpriteRenderer _sprite;
        Vector3 _origin;

        // 발판 위 플레이어. transform.SetParent 대신 이동량을 더해 옮긴다 (부모 변경은
        // 스케일 오염을 일으킨다). OnCollisionEnter2D/Exit2D로 갱신한다.
        Transform _riderOnTop;

        public Transform Transform => transform;
        public Sprite CurrentSprite => _sprite != null ? _sprite.sprite : null;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _sprite = GetComponent<SpriteRenderer>();
            _origin = transform.position;
        }

        Vector3 PositionAt(float time)
        {
            float t = Mathf.PingPong(time / (period * 0.5f), 1f);
            return _origin + (Vector3)offset * Mathf.SmoothStep(0f, 1f, t);
        }

        void FixedUpdate()
        {
            var next = PositionAt(Time.time);
            var delta = next - transform.position;
            _rb.MovePosition(next);
            if (_riderOnTop != null) _riderOnTop.position += delta;
        }

        public Vector3 PredictPosition(float lead) => PositionAt(Time.time + lead);
        public bool PredictActive(float lead) => true;

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            bool fromAbove = false;
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) { fromAbove = true; break; }
            }
            if (fromAbove) _riderOnTop = collision.transform;
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.transform == _riderOnTop) _riderOnTop = null;
        }
    }
}
