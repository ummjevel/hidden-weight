using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.Enemies
{
    // 적과 접촉한 플레이어에게 피해를 준다. 무적 시간 처리는 PlayerHealth가 알아서 한다.
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] int damage = 1; // 0이면 Enemy.Data.contactDamage를 쓴다

        Enemy _enemy;

        void Awake()
        {
            // 접촉 피해 전용 트리거 자식(Hitbox)에 붙는다 — 본체 콜라이더는 플레이어와 밀지
            // 않도록 레이어 충돌을 꺼 뒀기 때문이다(ProjectSetup.SetupLayerCollisionMatrix).
            _enemy = GetComponentInParent<Enemy>();
        }

        int Damage => damage != 0 ? damage : (_enemy != null ? _enemy.Data.contactDamage : 1);

        void OnCollisionStay2D(Collision2D collision)
        {
            TryDamage(collision.gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other.gameObject);
        }

        void TryDamage(GameObject target)
        {
            var health = target.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(Damage, transform.position);
        }
    }
}
