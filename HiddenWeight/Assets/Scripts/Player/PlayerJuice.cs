using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Player
{
    // 착지·벽점프 시 더스트 파티클과 카메라 흔들림을 재생한다. 밸런스와 무관한 연출 전용 컴포넌트.
    public class PlayerJuice : MonoBehaviour
    {
        [SerializeField] Transform groundCheck;
        [SerializeField] Transform wallCheck;
        [SerializeField] GameObject landDustPrefab;
        [SerializeField] GameObject wallDustPrefab;

        PlayerController _controller;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        void OnEnable()
        {
            if (_controller != null) _controller.StateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            if (_controller != null) _controller.StateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(PlayerState state)
        {
            if (state == PlayerState.Land)
            {
                SpawnDust(landDustPrefab, groundCheck);
                RoomCamera.Instance?.Shake();
            }
            else if (state == PlayerState.WallJump)
            {
                SpawnDust(wallDustPrefab, wallCheck);
            }
        }

        void SpawnDust(GameObject prefab, Transform at)
        {
            if (prefab == null || at == null) return;
            Object.Instantiate(prefab, at.position, Quaternion.identity);
        }
    }
}
