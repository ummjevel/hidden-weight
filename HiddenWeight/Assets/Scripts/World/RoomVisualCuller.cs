using UnityEngine;

namespace HiddenWeight.World
{
    // 한 씬에 15개 방이 함께 있어도 현재 방의 대형 배경만 그린다.
    // 오브젝트 자체를 끄지 않아 다음 RoomChanged 이벤트를 계속 받을 수 있게 Renderer만 제어한다.
    public class RoomVisualCuller : MonoBehaviour
    {
        Room _owner;
        Renderer[] _renderers;
        RoomCamera _camera;

        void Awake()
        {
            _owner = GetComponentInParent<Room>();
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        void Start() => TryBind();

        void Update()
        {
            if (_camera == null) TryBind();
        }

        void OnDisable()
        {
            if (_camera != null) _camera.RoomChanged -= Apply;
            _camera = null;
        }

        void TryBind()
        {
            if (RoomCamera.Instance == null) return;
            _camera = RoomCamera.Instance;
            _camera.RoomChanged += Apply;
            var current = _camera.CurrentRoom;
            if (current == null && Player.PlayerController.Instance != null && _owner != null
                && _owner.Contains(Player.PlayerController.Instance.transform.position))
                current = _owner;
            Apply(current);
        }

        void Apply(Room current)
        {
            bool visible = current == null || current == _owner;
            foreach (var renderer in _renderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}
