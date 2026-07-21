using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10~11번의 경비원/CCTV가 공유하는 시야 판정. 두 감시자 모두 "정면 부채꼴 범위 안에
    // 플레이어가 들어오면 즉시 발각"이라는 동일한 규칙을 쓰기 때문에(추격전 없음, GDD 11번),
    // M4에서 만든 ConeTargetingUtility(무기 타겟팅용)를 그대로 재사용해 중복을 피했다.
    public abstract class VisionSensorBase : MonoBehaviour
    {
        [SerializeField] private float halfAngleDegrees = 45f;
        [SerializeField] private float range = 5f;

        private NightManager _nightManager;
        private Transform _playerTransform;
        private readonly System.Collections.Generic.List<Vector2> _singleCandidateBuffer = new System.Collections.Generic.List<Vector2>(1);

        protected virtual void Start()
        {
            _nightManager = FindObjectOfType<NightManager>();
            var player = FindObjectOfType<PlayerController>();
            if (player != null) _playerTransform = player.transform;
        }

        protected virtual void Update()
        {
            if (_playerTransform == null || _nightManager == null || _nightManager.IsFinished) return;

            _singleCandidateBuffer.Clear();
            _singleCandidateBuffer.Add(_playerTransform.position);

            var hits = ConeTargetingUtility.FindTargetsInCone(
                transform.position, FacingDirection, halfAngleDegrees, range, _singleCandidateBuffer, maxTargets: 1);

            if (hits.Count > 0)
            {
                _nightManager.ReportDetection();
            }
        }

        // 경비는 고정 방향(또는 자체 패트롤), CCTV는 고정 방향인 경우가 보통이라 하위 클래스가 결정한다.
        protected abstract Vector2 FacingDirection { get; }
    }
}
