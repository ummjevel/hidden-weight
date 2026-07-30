using UnityEngine;
using HiddenWeight.World;
using HiddenWeight.UI;

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
            // 착지 흔들림은 뺐다(팀 결정 2026-07-30). 일반 이동의 매 착지마다 화면이 흔들려
            // 탐험 중 시각적 소음이 됐다. 더스트만 남긴다 — RoomCamera.Shake 자체는 보스
            // 낙하 등 큰 충격 연출을 위해 남아 있다.
            if (state == PlayerState.Land)
            {
                SpawnDust(landDustPrefab, groundCheck);
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
