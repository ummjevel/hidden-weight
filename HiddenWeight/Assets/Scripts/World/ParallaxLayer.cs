using UnityEngine;

namespace HiddenWeight.World
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float multiplier = 0.5f;

        Vector3 _anchorLayer;
        Vector3 _anchorCamera;
        Transform _cameraTransform;

        void OnEnable()
        {
            BindMainCamera();
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
