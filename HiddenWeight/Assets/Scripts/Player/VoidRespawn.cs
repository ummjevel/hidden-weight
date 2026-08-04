using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 맵 경계 밖 허공으로 떨어졌을 때의 소프트락 방지.
    //
    // 예전에는 "절대 좌표 y가 -15보다 아래면 리스폰"이었다. 지역이 전부 y=0 근처에 있을 때는
    // 통했지만, 아래로 내려가는 지역(잔재 재설계는 y=-44까지 내려간다)에서는 정상 플레이 구간이
    // 통째로 기준선 아래라 매 프레임 리스폰이 걸려 그 자리에 얼어붙는다 — 실제로 R05에서
    // 봇이 좌표 그대로 정지한 채 100초를 보냈다.
    //
    // 그래서 절대 높이가 아니라 "마지막으로 발을 딛었던 높이에서 얼마나 더 떨어졌는가"로 판단한다.
    // 지역이 어느 높이에 있든 상관없이 동작하고, 정상적인 낙하 구간(가장 깊은 안전 바닥도
    // 8유닛 아래다)과도 확실히 구분된다.
    [RequireComponent(typeof(PlayerController))]
    public class VoidRespawn : MonoBehaviour
    {
        [SerializeField] float fallLimit = 30f;

        PlayerController _controller;
        float _lastGroundedY;
        bool _hasGrounded;

        // 한 번도 땅을 밟지 않은 채 떨어질 수도 있다(방 밖으로 나갔거나, 로드 직후
        // 발밑이 비어 있었거나). 그때 _hasGrounded만 보면 판단을 영원히 미루게 되어
        // 플레이어가 끝없이 떨어진다 — 실제로 y=-420까지 내려간 기록이 있다.
        // 시작 높이에서 이만큼 아래면 접지 이력과 무관하게 되돌린다.
        const float AbsoluteFallLimit = 100f;
        float _startY;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _lastGroundedY = transform.position.y;
            _startY = transform.position.y;
        }

        void Update()
        {
            if (_controller.IsGrounded)
            {
                _lastGroundedY = transform.position.y;
                _hasGrounded = true;
                return;
            }

            // 한 번도 땅을 밟은 적 없으면 보통은 판단하지 않는다(스폰 직후 낙하 중일 수
            // 있다). 다만 그 상태로 한없이 떨어지는 것은 진행 불가이므로 절대 한계를 둔다.
            if (!_hasGrounded)
            {
                if (transform.position.y >= _startY - AbsoluteFallLimit) return;
                if (GameManager.Instance == null) return;
                GameManager.Instance.RespawnPlayer();
                _startY = transform.position.y;
                return;
            }
            if (transform.position.y >= _lastGroundedY - fallLimit) return;
            if (GameManager.Instance == null) return;

            GameManager.Instance.RespawnPlayer();
            _lastGroundedY = transform.position.y; // 리스폰 직후 곧바로 다시 걸리지 않게 갱신
        }
    }
}
