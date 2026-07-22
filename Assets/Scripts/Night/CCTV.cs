using UnityEngine;

namespace HanGame.Night
{
    /// <summary>
    /// CCTV. 좌우로 천천히 회전. 시야에 들면 즉시 발각. 벽이 차단. 기획서 11.7.
    /// 해킹·정지 기능은 초기 버전 제외.
    /// </summary>
    [RequireComponent(typeof(VisionCone))]
    public class CCTV : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 30f; // 도/초
        [SerializeField] private float sweepAngle = 70f;  // 중심에서 ±각도
        [SerializeField] private float centerAngleDeg = 0f; // 기준 방향(도)
        [SerializeField] private float viewDistance = 5f;
        [SerializeField] private float viewAngle = 40f;

        private VisionCone _vision;
        private float _phase;

        private void Awake() => _vision = GetComponent<VisionCone>();

        private void Start()
        {
            _vision.Configure(viewDistance, viewAngle, LayerMask.GetMask("Obstacle"));
        }

        private void Update()
        {
            // 좌우 왕복(sine).
            _phase += rotateSpeed * Time.deltaTime;
            float offset = Mathf.Sin(_phase * Mathf.Deg2Rad) * sweepAngle;
            float ang = (centerAngleDeg + offset) * Mathf.Deg2Rad;
            _vision.FacingDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }
    }
}
