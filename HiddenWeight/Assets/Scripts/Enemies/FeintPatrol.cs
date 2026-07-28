using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 순찰형 — 균열의 "불안 새싹"(FRACTURE_LEVEL_DESIGN.md 6.1절). 짧은 왕복 중 방향을
    // 한 번 가짜로 튼다.
    //
    // EnemyPatrol을 쓰지 않는 이유: 저쪽은 낭떠러지·벽 판정으로 방향을 정하는 반응형이라
    // 2초 뒤 위치를 계산할 수 없다. 균열의 공정성 규칙은 "예지 고스트가 보여준 위치와 실제
    // 2초 뒤 위치는 항상 일치한다"(7.2절)이므로, 이 적의 궤도는 시간의 순수 함수여야 한다.
    //
    // 가짜 방향 전환은 궤도가 아니라 몸의 방향만 뒤집는다. 예지가 거짓말을 하지 않으면서도
    // 눈으로 보면 속는다 — 정보를 왜곡하는 대신 관찰을 어렵게 하는 쪽이다.
    [RequireComponent(typeof(Enemy))]
    public class FeintPatrol : EnemyBehavior, IForeseeable
    {
        Vector3 _origin;

        public Transform Transform => transform;
        public Sprite CurrentSprite => Sprite != null ? Sprite.sprite : null;

        protected override void Awake()
        {
            base.Awake();
            _origin = transform.position;

            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        // 왕복 궤도. 위상만으로 x가 정해진다.
        float PathXAt(float time)
        {
            float period = Mathf.Max(0.1f, Data.patrolPeriod);
            float t = Mathf.PingPong(time / (period * 0.5f), 1f);
            return _origin.x + Mathf.SmoothStep(-Data.patrolWidth * 0.5f, Data.patrolWidth * 0.5f, t);
        }

        // 반환점 직전마다 짧게 몸을 반대로 튼다. 진행은 그대로다.
        bool IsFeinting(float time)
        {
            float period = Mathf.Max(0.1f, Data.patrolPeriod);
            float phase = Mathf.Repeat(time, period * 0.5f);
            return phase >= period * 0.5f - Data.feintSeconds;
        }

        void FixedUpdate()
        {
            float now = Time.time;
            float targetX = PathXAt(now);
            float aheadX = PathXAt(now + Time.fixedDeltaTime);

            // 속도로 따라간다. 위치를 직접 밀어 넣으면 지형 충돌과 접촉 피해가 깨진다.
            float drift = (targetX - transform.position.x) / Time.fixedDeltaTime;
            float travel = (aheadX - targetX) / Time.fixedDeltaTime;
            Body.linearVelocity = new Vector2(
                Mathf.Clamp(travel + drift, -Data.moveSpeed * 4f, Data.moveSpeed * 4f),
                Body.linearVelocity.y);

            int heading = aheadX >= targetX ? 1 : -1;
            FaceTowards(IsFeinting(now) ? -heading : heading);
            Self.PlayClip("Walk");
        }

        public Vector3 PredictPosition(float leadSeconds)
            => new Vector3(PathXAt(Time.time + leadSeconds), transform.position.y, transform.position.z);

        public bool PredictActive(float leadSeconds) => Self.IsAlive;
    }
}
