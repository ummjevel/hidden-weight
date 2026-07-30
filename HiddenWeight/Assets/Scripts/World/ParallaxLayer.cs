using UnityEngine;

namespace HiddenWeight.World
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float multiplier = 0.5f;

        Vector3 _anchorLayer;
        Vector3 _anchorCamera;
        Transform _cameraTransform;

        // 배치된 원위치(방 중앙). 앵커가 씬 시작 카메라 기준으로 한 번만 잡히면, 시작점에서
        // 300유닛 떨어진 방에 도착했을 때 원경이 0.15×300 = 45유닛 밀려 방 밖으로 나가 있다.
        // 방에 들어올 때마다 여기로 되돌리고 다시 앵커를 잡는다(RoomVisualCuller가 호출).
        Vector3 _initialPosition;
        bool _initialCaptured;

        void Awake()
        {
            _initialPosition = transform.position;
            _initialCaptured = true;
        }

        void OnEnable()
        {
            BindMainCamera();
        }

        // 방 진입 시 원위치로 되돌리고 지금 카메라 위치를 새 기준으로 삼는다.
        public void Rebase(Vector3 cameraPosition)
        {
            if (_initialCaptured) transform.position = _initialPosition;
            SetAnchor(cameraPosition);
        }

        void LateUpdate()
        {
            if (_cameraTransform == null)
                BindMainCamera();

            FollowBoundCamera();
        }

        public void SetAnchor(Vector3 cameraPosition)
        {
            _anchorLayer = transform.position;
            _anchorCamera = cameraPosition;
        }

        public void ApplyCameraPosition(Vector3 cameraPosition)
        {
            Vector3 delta = cameraPosition - _anchorCamera;
            transform.position = _anchorLayer + new Vector3(
                delta.x * multiplier,
                delta.y * multiplier,
                0f);
        }

        void BindMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            _cameraTransform = mainCamera.transform;
            SetAnchor(_cameraTransform.position);
        }

        void FollowBoundCamera()
        {
            if (_cameraTransform != null)
                ApplyCameraPosition(_cameraTransform.position);
        }

#if UNITY_EDITOR
        public void SetMultiplierForTest(float value)
        {
            multiplier = value;
        }

        public void BindCameraForTest(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        public void FollowBoundCameraForTest()
        {
            FollowBoundCamera();
        }
#endif
    }
}
