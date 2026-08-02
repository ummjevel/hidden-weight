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
            //
            // 보정(drift)은 궤도에서 밀려난 만큼을 되돌리는 항이다. 지형에 막혀 있으면 오차가
            // 계속 쌓이고, 풀리는 순간 최대 속도로 튀어 순간이동처럼 보인다. 보정은 한 걸음
            // 크기로 묶고, 오래 막혀 있으면 궤도 자체를 지금 위치로 다시 잡는다.
            float error = targetX - transform.position.x;
            if (Mathf.Abs(error) > ReanchorDistance)
            {
                _blockedTimer += Time.fixedDeltaTime;
                if (_blockedTimer >= ReanchorSeconds)
                {
                    _origin.x += error;
                    _blockedTimer = 0f;
                    error = 0f;
                }
            }
            else
            {
                _blockedTimer = 0f;
            }

            float drift = Mathf.Clamp(error / Time.fixedDeltaTime,
                                      -Data.moveSpeed, Data.moveSpeed);
            float travel = (aheadX - targetX) / Time.fixedDeltaTime;
            float speedX = Mathf.Clamp(travel + drift,
                                       -Data.moveSpeed * 2f, Data.moveSpeed * 2f);
            Body.linearVelocity = new Vector2(speedX, Body.linearVelocity.y);

            // 궤도가 SmoothStep이라 반환점에서 속도가 0에 수렴한다. 그때도 Walk를 틀면
            // 제자리에서 걷는 그림이 미끄러지는 것처럼 보인다.
            UpdateLocomotionClip(speedX);

            // 페인트는 몸의 방향을 뒤집지 않는다. 뒤집으면 진행 방향과 실루엣이 어긋난 채
            // 미끄러져 연출이 아니라 버그로 읽힌다(원래 의도는 "속인다"였다). 대신 상체를
            // 반대쪽으로 기울여 곧 돌아설 것처럼 보이게 한다 — 궤도는 그대로이므로
            // 예지 고스트는 여전히 정확하다.
            int heading = aheadX >= targetX ? 1 : -1;
            FaceTowards(heading);
            ApplyFeintLean(IsFeinting(now) ? -heading : 0);
        }

        // 궤도에서 이만큼 벗어난 채 이만큼 오래 버티면 막힌 것으로 본다.
        const float ReanchorDistance = 0.6f;
        const float ReanchorSeconds = 0.5f;
        float _blockedTimer;

        const float FeintLeanDegrees = 14f;
        Transform _art;

        void ApplyFeintLean(int direction)
        {
            // 겉모습만 기울인다. 루트를 돌리면 콜라이더가 함께 돌아 판정이 어긋난다.
            if (_art == null)
            {
                var animator = GetComponentInChildren<World.SpriteAnimator>();
                _art = animator != null ? animator.transform
                     : Sprite != null ? Sprite.transform : null;
                if (_art == null || _art == transform) return;
            }

            float target = direction * FeintLeanDegrees;
            float current = _art.localEulerAngles.z;
            if (current > 180f) current -= 360f;
            _art.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.MoveTowards(current, target, 90f * Time.fixedDeltaTime));
        }

        public Vector3 PredictPosition(float leadSeconds)
            => new Vector3(PathXAt(Time.time + leadSeconds), transform.position.y, transform.position.z);

        public bool PredictActive(float leadSeconds) => Self.IsAlive;
    }
}
