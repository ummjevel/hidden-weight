using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 날아가는 공격체. 지형에 닿거나 수명이 다하면 사라진다.
    //
    // 설정은 전부 ProjectileSpawner가 발사 시점에 밀어 넣는다. 프리팹을 종류마다 만들지
    // 않는 이유는 ImpactVFX와 같다 — 이 프로젝트는 씬을 코드로 짓고, 프레임 배열만 들고
    // 그때그때 오브젝트를 만드는 편이 빌더와 궁합이 맞는다.
    public class Projectile : MonoBehaviour
    {
        Sprite[] _frames;
        float _fps;
        float _speed;
        float _lifetime;
        float _radius;
        int _damage;
        Vector2 _direction;
        int _playerMask;
        int _obstacleMask;

        SpriteRenderer _renderer;
        float _elapsed;
        float _frameTimer;
        int _frame;

        public void Launch(Sprite[] frames, float fps, float speed, float lifetime, float radius,
                           int damage, Vector2 direction, float displayHeight,
                           int playerMask, int obstacleMask, int sortingOrder)
        {
            _frames = frames;
            _fps = fps <= 0f ? 12f : fps;
            _speed = speed;
            _lifetime = lifetime;
            _radius = radius;
            _damage = damage;
            _direction = direction.normalized;
            _playerMask = playerMask;
            _obstacleMask = obstacleMask;

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = sortingOrder;
            _renderer.flipX = _direction.x < 0f;
            ApplyFrame(0, displayHeight);
        }

        void ApplyFrame(int index, float displayHeight)
        {
            if (_frames == null || _frames.Length == 0) return;

            _frame = index % _frames.Length;
            _renderer.sprite = _frames[_frame];

            if (displayHeight <= 0f || _renderer.sprite == null) return;

            float height = _renderer.sprite.bounds.size.y;
            if (height > 0f)
            {
                float scale = displayHeight / height;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime) { Destroy(gameObject); return; }

            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

            // 프레임 넘김. displayHeight는 첫 프레임에서 이미 정했으므로 다시 재지 않는다.
            _frameTimer += Time.deltaTime;
            float interval = 1f / _fps;
            while (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                if (_frames != null && _frames.Length > 0)
                {
                    _frame = (_frame + 1) % _frames.Length;
                    _renderer.sprite = _frames[_frame];
                }
            }

            // 지형에 박히면 사라진다. 벽 뒤로 날아가 보이지 않는 곳에서 맞히지 않게 한다.
            if (_obstacleMask != 0 && Physics2D.OverlapCircle(transform.position, _radius * 0.5f, _obstacleMask))
            {
                Destroy(gameObject);
                return;
            }

            if (_damage <= 0) return;

            var hit = Physics2D.OverlapCircle(transform.position, _radius, _playerMask);
            if (hit == null) return;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(_damage, transform.position);
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
