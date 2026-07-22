using UnityEngine;
using HanGame.Data;

namespace HanGame.Night
{
    /// <summary>
    /// 경비 순찰. 정해진 경로 반복, 앞쪽 부채꼴 시야. 기획서 11.5.
    /// 시작 후 3초 정지. 동선은 매번 동일(무작위 배치 없음, 공정성).
    /// 소음을 들으면 잠시 해당 위치 확인(NoiseSystem 연동).
    /// </summary>
    [RequireComponent(typeof(VisionCone))]
    public class GuardPatrol : MonoBehaviour
    {
        [SerializeField] private GuardRouteData route;

        private VisionCone _vision;
        private int _index;
        private float _waitTimer;
        private float _startTimer;
        private Vector2? _investigatePoint;
        private float _investigateTimer;

        private void Awake()
        {
            _vision = GetComponent<VisionCone>();
        }

        private void Start()
        {
            if (route != null)
            {
                _vision.Configure(route.viewDistance, route.viewAngle, LayerMask.GetMask("Obstacle"));
                _startTimer = route.startDelay;
                if (route.waypoints.Count > 0)
                    transform.position = route.waypoints[0];
            }
        }

        private void Update()
        {
            if (route == null || route.waypoints.Count == 0) return;

            if (_startTimer > 0f) { _startTimer -= Time.deltaTime; return; } // 시작 3초 정지

            // 소음 확인 우선.
            if (_investigatePoint.HasValue)
            {
                MoveTowards(_investigatePoint.Value);
                _investigateTimer -= Time.deltaTime;
                if (_investigateTimer <= 0f || Arrived(_investigatePoint.Value))
                    _investigatePoint = null;
                return;
            }

            if (_waitTimer > 0f) { _waitTimer -= Time.deltaTime; return; }

            Vector2 target = route.waypoints[_index];
            if (Arrived(target))
            {
                _waitTimer = route.waitAtWaypoint;
                _index++;
                if (_index >= route.waypoints.Count)
                    _index = route.loop ? 0 : route.waypoints.Count - 1;
            }
            else MoveTowards(target);
        }

        private void MoveTowards(Vector2 target)
        {
            Vector2 pos = transform.position;
            Vector2 dir = (target - pos).normalized;
            _vision.FacingDir = dir;
            transform.position = Vector2.MoveTowards(pos, target, route.moveSpeed * Time.deltaTime);
        }

        private bool Arrived(Vector2 target) => Vector2.Distance(transform.position, target) < 0.1f;

        /// <summary>NoiseSystem이 호출: 소음 위치를 잠시 확인.</summary>
        public void HearNoise(Vector2 point, float checkSeconds = 2f)
        {
            _investigatePoint = point;
            _investigateTimer = checkSeconds;
        }
    }
}
