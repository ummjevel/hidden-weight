using UnityEngine;
using HanGame.Day;

namespace HanGame.Weapons
{
    /// <summary>
    /// 플레이어 무기 발사체. 가구를 통과(기획서 4.4)하므로 적 레이어만 검사.
    /// pierce=false면 첫 적 적중 시 소멸(스테이플러, 기획서 8.3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        private Vector2 _dir;
        private float _speed;
        private float _damage;
        private bool _pierce;
        private float _life;

        public void Launch(Vector2 dir, float speed, float damage, bool pierce, float life = 3f)
        {
            _dir = dir.normalized;
            _speed = speed;
            _damage = damage;
            _pierce = pierce;
            _life = life;
        }

        private void Update()
        {
            transform.position += (Vector3)(_dir * (_speed * Time.deltaTime));
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead) return;

            enemy.TakeDamage(_damage);
            if (!_pierce) Destroy(gameObject);
        }
    }
}
