using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 응시 지역의 "시선" 기믹. 원뿔 시야 안에 플레이어가 있으면 피해를 준다.
    public class GazeHazard : MonoBehaviour
    {
        [SerializeField] float viewRadius = 6f;
        [SerializeField] float viewAngle = 60f;
        [SerializeField] float damageInterval = 1f;

        // 인스펙터에서 Player 레이어만 지정한다. PlayerHushed는 넣지 않는다 —
        // 숨죽이기 중에는 플레이어 레이어가 바뀌어 시선에서 자동으로 무해화된다 (기획서 4.2절).
        // World가 Emotions를 참조하지 않고도 숨죽이기 기믹과 맞물리는 이유다.
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask groundMask;

        float _damageTimer;

        public bool IsPlayerSeen { get; private set; }

        void Update()
        {
            IsPlayerSeen = false;
            var target = Physics2D.OverlapCircle(transform.position, viewRadius, playerMask);

            if (target != null)
            {
                Vector2 toPlayer = (Vector2)target.transform.position - (Vector2)transform.position;
                float angle = Vector2.Angle(transform.right, toPlayer);

                if (angle <= viewAngle * 0.5f)
                {
                    bool blocked = Physics2D.Linecast(transform.position, target.transform.position, groundMask);
                    IsPlayerSeen = !blocked;
                }
            }

            if (!IsPlayerSeen)
            {
                _damageTimer = 0f;
                return;
            }

            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0f)
            {
                var health = target.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(1, transform.position);
                _damageTimer = damageInterval;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 forward = transform.right;
            var leftRot = Quaternion.Euler(0f, 0f, viewAngle * 0.5f);
            var rightRot = Quaternion.Euler(0f, 0f, -viewAngle * 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + leftRot * forward * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightRot * forward * viewRadius);
        }
    }
}
