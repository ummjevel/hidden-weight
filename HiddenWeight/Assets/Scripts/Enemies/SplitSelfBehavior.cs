using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 정예 — 균열의 "갈라진 자아"(FRACTURE_LEVEL_DESIGN.md 6.2절). 동일한 외형의 두
    // 실루엣이 좌우 대칭으로 움직이고, 한 번에 한쪽만 실제 본체다.
    //
    // 구현은 본체 하나 + 거울상 하나다. 거울상은 Enemy가 아니라 그림이라 때려도 아무 일이
    // 일어나지 않는다 — 즉 "어느 쪽이 진짜인지"를 판별하는 것 자체가 이 전투다. 본체가
    // 맞으면 두 위치를 맞바꿔 역할이 바뀐다(6.2절).
    //
    // 예지는 본체의 2초 뒤 위치에만 고스트를 만든다. 예지를 쓰지 않아도 그림자 방향으로
    // 구분할 수 있게, 본체 쪽 그림자만 진하게 둔다 — 쿨타임 동안 무조건 기다리게 하지
    // 않는다는 규칙(6.2절)이다.
    [RequireComponent(typeof(Enemy))]
    public class SplitSelfBehavior : EnemyBehavior, IForeseeable
    {
        [SerializeField] Transform mirror;          // 거울상(콜라이더 없음)
        [SerializeField] SpriteRenderer bodyShadow;
        [SerializeField] SpriteRenderer mirrorShadow;
        [SerializeField] float mirrorAxisX;         // 좌우 대칭축
        [SerializeField] float approachSpeedMultiplier = 1.2f;

        int _lastHealth;

        public Transform Transform => transform;
        public Sprite CurrentSprite => Sprite != null ? Sprite.sprite : null;

        protected override void Awake()
        {
            base.Awake();
            _lastHealth = Self.Health;

            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        void Update()
        {
            // 피격 감지. Enemy에 이벤트를 새로 뚫는 대신 체력 변화를 본다 — 이 적 하나
            // 때문에 공용 계약을 넓히지 않는다.
            if (Self.Health < _lastHealth)
            {
                _lastHealth = Self.Health;
                SwapWithMirror();
            }

            if (Player == null) return;

            int direction = DirectionToPlayer;
            FaceTowards(direction);

            if (DistanceToPlayer <= Data.detectRange)
                Body.linearVelocity = new Vector2(
                    direction * Data.moveSpeed * approachSpeedMultiplier, Body.linearVelocity.y);
            else
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

            UpdateMirror();
        }

        void UpdateMirror()
        {
            if (mirror == null) return;

            // 대칭축을 기준으로 반사한 자리에 세운다.
            mirror.position = new Vector3(
                2f * mirrorAxisX - transform.position.x, transform.position.y, transform.position.z);
            mirror.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x) * Mathf.Sign(transform.localScale.x),
                transform.localScale.y, 1f);

            // 그림자 농도로 진짜를 구분할 수 있게 둔다.
            if (bodyShadow != null) bodyShadow.color = new Color(0f, 0f, 0f, 0.55f);
            if (mirrorShadow != null) mirrorShadow.color = new Color(0f, 0f, 0f, 0.18f);
        }

        // 맞으면 역할이 바뀐다 = 두 위치를 맞바꾼다. 플레이어는 방금 잡은 각을 다시 잡아야 한다.
        void SwapWithMirror()
        {
            if (mirror == null) return;

            var here = transform.position;
            transform.position = mirror.position;
            mirror.position = here;
        }

        public Vector3 PredictPosition(float leadSeconds)
        {
            float vx = Body != null ? Body.linearVelocity.x : 0f;
            return transform.position + new Vector3(vx * leadSeconds, 0f, 0f);
        }

        public bool PredictActive(float leadSeconds) => Self.IsAlive;
    }
}
