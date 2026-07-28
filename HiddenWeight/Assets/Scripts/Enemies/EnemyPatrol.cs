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

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<Enemy>();
            _data = _enemy.Data;
        }

        void FixedUpdate()
        {
            bool groundAhead = Physics2D.OverlapCircle(edgeCheck.position, 0.1f, groundMask);
            bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * _dir, 0.5f, groundMask);
            if (!groundAhead || wallAhead) Flip();

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

            var edgeLocal = edgeCheck.localPosition;
            edgeLocal.x = Mathf.Abs(edgeLocal.x) * _dir;
            edgeCheck.localPosition = edgeLocal;
        }
    }
}
