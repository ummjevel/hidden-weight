using UnityEngine;

namespace HiddenWeight.World
{
    // 한 씬에 15개 방이 함께 있어도 현재 방의 대형 배경만 그린다.
    // 오브젝트 자체를 끄지 않아 다음 RoomChanged 이벤트를 계속 받을 수 있게 Renderer만 제어한다.
    public class RoomVisualCuller : MonoBehaviour
    {
        Room _owner;
        Renderer[] _renderers;
        bool[] _initialRendererStates;
        RoomCamera _camera;

        void Awake()
        {
            _owner = GetComponentInParent<Room>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _initialRendererStates = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _initialRendererStates[i] = _renderers[i] != null && _renderers[i].enabled;
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
            bool residueScene = gameObject.scene.name.Contains("Residue");
            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;

                // V3 실제 표면 모듈로 교체된 뒤에도 씬에 남아 있는 구형 FloorArt는
                // 충돌과 무관한 큰 바닥 그림이다. 방 전환 시 다시 켜면 가짜 길이 된다.
                if (residueScene && renderer.name == "FloorArt")
                {
                    renderer.enabled = false;
                    continue;
                }

                // R07_StairVisual은 실제 충돌 없는 아치 벽까지 포함한 구형 장식이다.
                // 방 전환 때 다시 켜지면 통과 가능한 가짜 벽이 되므로 계속 숨긴다.
                Transform fakeStair = renderer.transform;
                while (fakeStair != null && fakeStair.name != "R07_StairVisual")
                    fakeStair = fakeStair.parent;
                if (residueScene && fakeStair != null)
                {
                    renderer.enabled = false;
                    continue;
                }

                // R04 마지막 오른쪽 벽은 통과 경로로 제거됐다. 씬에 남아 있는 구형 자식
                // 렌더러를 방 전환 때 다시 켜면 충돌 없는 가짜 벽이 되므로 계속 숨긴다.
                if (residueScene
                    && renderer.GetComponentInParent<BoxCollider2D>()?.name == "R04_Chimney_R")
                {
                    renderer.enabled = false;
                    continue;
                }

                // 잔재 V3 세로벽은 런타임 모듈로 실제 콜라이더 bounds에 맞춰 다시 그린다.
                // 방 전환 때 구형 Art까지 일괄 활성화하면 그 그림이 콜라이더 위·아래로
                // 돌출되어 R04에서 "보이는데 통과되는 벽"이 다시 생긴다.
                var wall = renderer.GetComponentInParent<BoxCollider2D>();
                Transform runtimeWallArt = wall != null
                    ? wall.transform.Find("WallClimbSurfaces_Runtime") : null;
                if (residueScene && runtimeWallArt != null
                    && !renderer.transform.IsChildOf(runtimeWallArt))
                {
                    renderer.enabled = false;
                    continue;
                }

                renderer.enabled = visible && _initialRendererStates[i];
            }

            // 방이 화면에 들어오는 순간 패럴랙스를 다시 기준 잡는다. 씬 시작 때 잡힌 앵커를
            // 그대로 쓰면 멀리 있는 방일수록 배경이 방 중심에서 밀려나 있다(ParallaxLayer 주석).
            if (visible && _camera != null)
                foreach (var layer in GetComponentsInChildren<ParallaxLayer>(true))
                    layer.Rebase(_camera.transform.position);
        }
    }
}
