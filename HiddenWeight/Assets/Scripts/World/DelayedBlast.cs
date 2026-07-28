using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 착지 예정 지점에 놓이는 지연 폭발. 균열의 "가능성 수집자"가 만든다
    // (FRACTURE_LEVEL_DESIGN.md 6.1절 "착지 예정 발판에 지연 폭발 배치").
    //
    // 예지 반응은 "다음 폭발 지점 표시"다. 그래서 IForeseeable을 구현하되, 이미 터질
    // 시각이 지난 표식은 PredictActive를 false로 돌려 고스트를 띄우지 않는다 — 발판이
    // 있어야 할 자리에 고스트가 없는 것이 경고라는 규칙(ForesightSkill 주석)과 같은 방식이다.
    public class DelayedBlast : MonoBehaviour, IForeseeable
    {
        [SerializeField] float fuseSeconds = 2f;
        [SerializeField] float radius = 1.6f;
        [SerializeField] int damage = 1;
        [SerializeField] LayerMask playerMask;
        [SerializeField] SpriteRenderer visual;

        float _timer;

        public Transform Transform => transform;
        public Sprite CurrentSprite => visual != null ? visual.sprite : null;

        // 수집자가 만든 직후 값을 밀어 넣는다. 프리팹을 늘리지 않기 위해 코드로 조립한다.
        public void Configure(float fuse, float blastRadius, int blastDamage, LayerMask mask, SpriteRenderer sprite)
        {
            fuseSeconds = fuse;
            radius = blastRadius;
            damage = blastDamage;
            playerMask = mask;
            visual = sprite;
            _timer = fuse;
        }

        void Awake()
        {
            if (_timer <= 0f) _timer = fuseSeconds;
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            _timer -= Time.deltaTime;

            // 남은 시간이 짧아질수록 빠르게 깜빡인다. 예지 없이도 읽을 수 있는 기본 예고다
            // (7.2절 "예지 쿨타임 중에도 기본 예고만으로 생존 가능한 안전 경로를 유지").
            if (visual != null)
            {
                float progress = fuseSeconds <= 0f ? 1f : 1f - Mathf.Clamp01(_timer / fuseSeconds);
                float blink = Mathf.PingPong(Time.time * (2f + progress * 8f), 1f);
                var color = visual.color;
                color.a = 0.25f + blink * 0.5f;
                visual.color = color;
            }

            if (_timer > 0f) return;

            var hit = Physics2D.OverlapCircle(transform.position, radius, playerMask);
            if (hit != null)
            {
                var health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage, transform.position);
            }
            Destroy(gameObject);
        }

        public Vector3 PredictPosition(float leadSeconds) => transform.position;

        // lead초 뒤에도 아직 안 터졌으면 그때 그 자리에 위험이 있다 = 고스트를 보여 준다.
        public bool PredictActive(float leadSeconds) => _timer > 0f && _timer >= leadSeconds;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
