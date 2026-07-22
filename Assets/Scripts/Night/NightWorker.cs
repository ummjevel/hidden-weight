using UnityEngine;

namespace HanGame.Night
{
    /// <summary>
    /// 야근자. 한 구역에 머물다 정해진 시간에 다른 책상으로 이동. 기획서 11.6.
    /// 경비보다 시야 좁지만 예고 없이 방향 전환. 층당 최대 1명(초기 버전).
    /// </summary>
    [RequireComponent(typeof(VisionCone))]
    public class NightWorker : MonoBehaviour
    {
        [SerializeField] private Vector2[] desks;   // 이동할 책상 위치
        [SerializeField] private float stayDuration = 4f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float viewDistance = 3f;
        [SerializeField] private float viewAngle = 45f;

        private VisionCone _vision;
        private int _index;
        private float _stayTimer;
        private bool _moving;

        private void Awake() => _vision = GetComponent<VisionCone>();

        private void Start()
        {
            _vision.Configure(viewDistance, viewAngle, LayerMask.GetMask("Obstacle"));
            _stayTimer = stayDuration;
            if (desks.Length > 0) transform.position = desks[0];
        }

        private void Update()
        {
            if (desks.Length == 0) return;

            if (_moving)
            {
                Vector2 target = desks[_index];
                Vector2 dir = ((Vector2)transform.position - target).sqrMagnitude > 0.0001f
                    ? (target - (Vector2)transform.position).normalized : _vision.FacingDir;
                _vision.FacingDir = dir;
                transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                if (Vector2.Distance(transform.position, target) < 0.05f)
                {
                    _moving = false;
                    _stayTimer = stayDuration;
                }
                return;
            }

            _stayTimer -= Time.deltaTime;
            if (_stayTimer <= 0f)
            {
                // 예고 없이 다음 책상으로(방향 즉시 전환).
                _index = (_index + 1) % desks.Length;
                _moving = true;
            }
        }
    }
}
