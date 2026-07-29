using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 응시 지역의 "시선" 기믹. 원뿔 시야 안에 플레이어가 들어오면 경보(눈 확대) 후
    // alarmDelay가 지나야 피해가 시작된다 (기획서 EMOTION_SYSTEM 2.3절 — 감지 시
    // 경보 이펙트 → 0.5초 후 위협 활성화. 즉사 대신 데미지 방식은 2.6절 권장안).
    //
    // GAZE_LEVEL_DESIGN.md가 요구하는 세 가지를 같은 컴포넌트로 덮는다 — 시선 종류를
    // 늘리는 대신 켜짐 조건과 복귀 방식만 바꾼다.
    //   1) 점멸 시선(onSeconds/offSeconds): G09 정예가 "시선이 꺼지는 순간 등을 보인다"를
    //      성립시키려면 시선이 주기적으로 꺼져야 한다. IsGazeOn을 적 행동 모듈이 읽는다.
    //   2) 휴면 시선(dormant): 평소 꺼져 있고 "밀고하는 입"(ScreamerBehavior)이 Activate로 켠다.
    //   3) 포착 복귀(retreatPoint): 4.2절 "포착은 즉사가 아니라 체력 1 피해와 직전 엄폐물
    //      뒤로의 넉백". 복귀 직후 0.8초 안전 시간도 여기서 준다(10절).
    public class GazeHazard : MonoBehaviour
    {
        // HUD가 특정 시선 구현을 탐색하지 않고도 현재 노출도를 표현할 수 있게 하는 역방향 훅.
        public static event System.Action<GazeHazard, float, bool> ExposureChanged;
        [SerializeField] float viewRadius = 6f;
        [SerializeField] float viewAngle = 60f;
        [SerializeField] float damageInterval = 1f;
        [SerializeField] float alarmDelay = 0.5f;
        [SerializeField] float alarmScale = 1.4f; // 경보 시 눈 확대 배율

        [Header("점멸 — offSeconds가 0이면 항상 켜져 있다")]
        [SerializeField] float onSeconds = 0f;
        [SerializeField] float offSeconds = 0f;
        [SerializeField] float phaseOffset = 0f;  // 여러 시선의 위상을 어긋나게 한다

        [Header("휴면 — true면 Activate()로만 켜진다")]
        [SerializeField] bool dormant = false;

        [Header("포착 복귀 — 비면 그 자리에서 피해만 준다")]
        [SerializeField] Transform retreatPoint;
        [SerializeField] float retreatInvulnSeconds = 0.8f;

        [Header("전환 애니메이션 — 비면 크기·색 변화로만 알린다")]
        // 눈이 뜨고 감기고, 빔을 예고하고, 실제로 쏘는 네 단계(EyeHazardTransitions_v1).
        // 애니메이터가 없으면 지금까지처럼 스케일과 색으로만 경보를 표현한다.
        [SerializeField] SpriteAnimator transitions;
        [SerializeField] string openClip = "GazeEyeOpen";
        [SerializeField] string closeClip = "GazeEyeClose";
        [SerializeField] string telegraphClip = "GazeBeamTelegraph";
        [SerializeField] string dischargeClip = "GazeBeamDischarge";

        bool _wasOn;
        bool _wasTelegraphing;
        float _lastReportedExposure = -1f;
        bool _lastReportedAlarm;

        void PlayTransition(string clip)
        {
            if (transitions == null || string.IsNullOrEmpty(clip)) return;
            if (!transitions.Has(clip)) return;
            transitions.Play(clip, true);
        }

        // 인스펙터에서 Player 레이어만 지정한다. PlayerHushed는 넣지 않는다 —
        // 숨죽이기 중에는 플레이어 레이어가 바뀌어 시선에서 자동으로 무해화된다 (기획서 4.2절).
        // World가 Emotions를 참조하지 않고도 숨죽이기 기믹과 맞물리는 이유다.
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask groundMask;

        float _damageTimer;
        float _seenTime;
        float _externalTimer;
        Vector3 _baseScale;
        SpriteRenderer _sprite;

        public bool IsPlayerSeen { get; private set; }
        public bool IsAlarmed => _seenTime >= alarmDelay;

        // 지금 이 시선이 켜져 있는가. 점멸·휴면을 한 곳에서 판단한다.
        public bool IsGazeOn
        {
            get
            {
                if (dormant) return _externalTimer > 0f;
                if (offSeconds <= 0f) return true;

                float cycle = onSeconds + offSeconds;
                float t = Mathf.Repeat(Time.time + phaseOffset, cycle);
                return t < onSeconds;
            }
        }

        // 밀고하는 입이 부른다. 이미 켜져 있으면 시간을 늘린다.
        public void Activate(float seconds)
        {
            _externalTimer = Mathf.Max(_externalTimer, seconds);
        }

        void Awake()
        {
            _baseScale = transform.localScale;
            _sprite = GetComponent<SpriteRenderer>();
        }

        void OnDisable()
        {
            ExposureChanged?.Invoke(this, 0f, false);
            _lastReportedExposure = -1f;
            _lastReportedAlarm = false;
        }

        void ReportExposure()
        {
            float exposure = IsPlayerSeen
                ? (alarmDelay <= 0f ? 1f : Mathf.Clamp01(_seenTime / alarmDelay))
                : 0f;
            bool alarmed = IsAlarmed && IsPlayerSeen;
            if (Mathf.Abs(exposure - _lastReportedExposure) < 0.05f && alarmed == _lastReportedAlarm) return;
            _lastReportedExposure = exposure;
            _lastReportedAlarm = alarmed;
            ExposureChanged?.Invoke(this, exposure, alarmed);
        }

        void Update()
        {
            if (_externalTimer > 0f) _externalTimer -= Time.deltaTime;

            // 꺼져 있는 동안은 감지도 경보도 하지 않는다. 눈을 반투명으로 낮춰
            // "지금은 안 보고 있다"를 텍스트 없이 알린다.
            bool on = IsGazeOn;
            if (on != _wasOn)
            {
                // 눈이 뜨고 감기는 순간만 재생한다. 매 프레임 다시 걸면 첫 프레임에서 멈춘다.
                _wasOn = on;
                PlayTransition(on ? openClip : closeClip);
            }

            if (!on)
            {
                IsPlayerSeen = false;
                _damageTimer = 0f;
                _seenTime = 0f;
                transform.localScale = _baseScale;
                if (_sprite != null) _sprite.color = new Color(1f, 1f, 1f, 0.25f);
                ReportExposure();
                return;
            }

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
                _wasTelegraphing = false;
                UpdateAlarmVisual();
                ReportExposure();
                return;
            }

            _seenTime += Time.deltaTime;
            UpdateAlarmVisual();
            ReportExposure();

            // 포착됐지만 아직 피해가 없는 단계 = 빔 예고. 여기서 벗어나면 맞지 않는다.
            if (!_wasTelegraphing)
            {
                _wasTelegraphing = true;
                PlayTransition(telegraphClip);
            }

            if (!IsAlarmed) return; // 경보 단계 — 아직 피해 없음, 벗어날 기회

            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0f)
            {
                var health = target.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(1, transform.position);
                _damageTimer = damageInterval;
                PlayTransition(dischargeClip);
                Retreat(health);
            }
        }

        // 포착당하면 직전 엄폐물 뒤로 되돌린다. 같은 자리에서 연속으로 다시 맞지 않도록
        // 짧은 무적을 함께 준다(GAZE_LEVEL_DESIGN.md 10절).
        void Retreat(PlayerHealth health)
        {
            if (retreatPoint == null || PlayerController.Instance == null) return;
            if (health != null && health.Current <= 0) return; // 이미 체크포인트로 돌아갔다

            PlayerController.Instance.TeleportTo(retreatPoint.position);
            if (health != null) health.GrantInvulnerability(retreatInvulnSeconds);
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
