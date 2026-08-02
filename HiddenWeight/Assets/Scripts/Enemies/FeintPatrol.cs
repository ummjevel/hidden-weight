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

            // 이 적은 속도를 매 물리 스텝 외부에서 지정받아 움직인다. 궤도가 SmoothStep이라
            // 반환점에서 속도가 0에 수렴하는데, 그 순간 리지드바디가 잠들면 이후 지정한
            // 속도가 먹지 않아 **궤도 끝에 영구히 고정된다**(F07에서 실제로 그랬다).
            // 그러면 예지는 계속 앞을 계산하고 적은 오지 않는다 — 공정성 규칙이 깨진다.
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            // 낭떠러지 클램프는 걸어 다니는 적을 위한 것이다. 궤도형이 그걸 맞으면 속도가
            // 0으로 눌린 채 시간만 흘러 궤도에서 영영 뒤처진다(Enemy.SuppressLedgeGuard 주석).
            Self.SuppressLedgeGuard = true;
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
            // 궤도 원점(_origin)은 절대 옮기지 않는다.
            //
            // 막혀 있을 때 원점을 현재 위치로 다시 잡는 복구를 넣었다가 이 지역의 약속을
            // 깨뜨렸다 — 예지는 예측 시점의 원점으로 계산한 위치를 보여주는데, 그 뒤 원점이
            // 옮겨지면 2초 뒤 실제는 다른 곳에 있다(실측 오차 1.5, 정확히 궤도 폭의 절반).
            // 플레이어는 정확한 정보라 믿고 움직였다가 맞는다.
            //
            // 궤도는 시간의 순수 함수여야 한다(설계 7.2). 막혀서 뒤처지면 보정항이 따라잡게
            // 두고, 궤도 자체는 건드리지 않는다.
            float error = targetX - transform.position.x;

            // 궤도 속도(travel)는 절대 깎지 않는다.
            //
            // 예전에는 travel+drift를 통째로 moveSpeed의 배수로 잘랐는데, 궤도가
            // SmoothStep이라 중간 구간의 순간 속도가 평균의 1.5배까지 오른다 — 상한에
            // 걸리면 적이 **자기 예측 궤도를 따라가지 못한다**. 그러면 예지가 보여준
            // 2초 뒤 위치와 실제가 어긋나고(실측 1.5유닛), 그 순간 이 지역의 약속인
            // "고스트는 언제나 정확하다"(설계 7.2)가 깨진다. 어려운 게 아니라 부당해진다.
            //
            // 튀는 것을 막아야 할 대상은 궤도가 아니라 **보정항(drift)**이다. 그쪽만 조인다.
            float drift = Mathf.Clamp(error / Time.fixedDeltaTime,
                                      -Data.moveSpeed, Data.moveSpeed);
            float travel = (aheadX - targetX) / Time.fixedDeltaTime;
            Body.linearVelocity = new Vector2(travel + drift, Body.linearVelocity.y);
            float speedX = travel + drift;

            // 궤도가 권위다. 속도만으로 따라가면 물리적으로 방해받는 순간 뒤처지는데,
            // 시간은 계속 흐르므로 한 번 벌어진 차이는 스스로 좁혀지지 않는다 — F07의
            // 새싹이 반환점에서 되돌아오지 못한 채 궤도와 1.5유닛 벌어졌고, 그만큼
            // 예지 고스트가 오지 않을 자리를 가리켰다.
            //
            // 이 지역이 요구하는 것은 "적이 걸어서 거기 도달한다"가 아니라 "2초 뒤 거기
            // 있다"이다(설계 7.2). 차이가 커지면 위치를 직접 끌어당겨 그 약속을 지킨다.
            // 한 스텝에 옮기는 양을 묶어 두므로 순간이동으로 보이지 않는다.
            if (Mathf.Abs(error) > TrackTolerance)
            {
                float step = Mathf.Min(Mathf.Abs(error), Data.moveSpeed * 3f * Time.fixedDeltaTime);
                Body.position = new Vector2(
                    Body.position.x + Mathf.Sign(error) * step, Body.position.y);
            }

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

        // 궤도에서 이만큼 벗어나면 위치를 직접 맞춘다.
        const float TrackTolerance = 0.12f;

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
        {
            // 이 컴포넌트가 돌지 않으면(조우 잠금으로 아직 깨어나지 않았거나 꺼진 상태)
            // 적은 움직이지 않는다. 그런데 궤도는 시간의 함수라 예측만 혼자 앞으로 간다 —
            // 고스트가 실제로는 오지 않을 자리를 가리키게 되고, 그 순간 "고스트는 언제나
            // 정확하다"는 약속이 깨진다. 멈춰 있는 것의 미래는 제자리다.
            if (!enabled || !isActiveAndEnabled) return transform.position;
            return new Vector3(PathXAt(Time.time + leadSeconds),
                               transform.position.y, transform.position.z);
        }

        public bool PredictActive(float leadSeconds) => Self.IsAlive;
    }
}
