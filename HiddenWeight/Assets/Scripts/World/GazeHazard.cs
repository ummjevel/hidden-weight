using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 응시 지역의 "시선" 기믹. 원뿔 시야 안에 플레이어가 들어오면 경보(눈 확대) 후
    // alarmDelay가 지나야 피해가 시작된다 (기획서 EMOTION_SYSTEM 2.3절 — 감지 시
    // 경보 이펙트 → 0.5초 후 위협 활성화. 즉사 대신 데미지 방식은 2.6절 권장안).
    public class GazeHazard : MonoBehaviour
    {
        [SerializeField] float viewRadius = 6f;
        [SerializeField] float viewAngle = 60f;
        [SerializeField] float damageInterval = 1f;
        [SerializeField] float alarmDelay = 0.5f;
        [SerializeField] float alarmScale = 1.4f; // 경보 시 눈 확대 배율

        // 인스펙터에서 Player 레이어만 지정한다. PlayerHushed는 넣지 않는다 —
        // 숨죽이기 중에는 플레이어 레이어가 바뀌어 시선에서 자동으로 무해화된다 (기획서 4.2절).
        // World가 Emotions를 참조하지 않고도 숨죽이기 기믹과 맞물리는 이유다.
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask groundMask;

        float _damageTimer;
        float _seenTime;
        Vector3 _baseScale;
        SpriteRenderer _sprite;

        public bool IsPlayerSeen { get; private set; }
        public bool IsAlarmed => _seenTime >= alarmDelay;

        void Awake()
        {
            _baseScale = transform.localScale;
            _sprite = GetComponent<SpriteRenderer>();
        }

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
                _seenTime = 0f;
                UpdateAlarmVisual();
                return;
            }

            _seenTime += Time.deltaTime;
            UpdateAlarmVisual();
            if (!IsAlarmed) return; // 경보 단계 — 아직 피해 없음, 벗어날 기회

            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0f)
            {
                var health = target.GetComponent<PlayerHealth>();
                if (health != null) health.TakeDamage(1, transform.position);
                _damageTimer = damageInterval;
            }
        }

        // 경보 진행도에 따라 눈이 커지고 붉어진다 — 별도 UI 없이 "들켰다"를 몸으로 알린다.
        void UpdateAlarmVisual()
        {
            float t = alarmDelay <= 0f ? 1f : Mathf.Clamp01(_seenTime / alarmDelay);
            transform.localScale = Vector3.Lerp(_baseScale, _baseScale * alarmScale, t);
            if (_sprite != null) _sprite.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), t);
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
