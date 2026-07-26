using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 맵 경계 밖 허공으로 떨어졌을 때의 소프트락 방지. 일정 깊이 아래로 내려가면
    // 마지막 체크포인트로 리스폰한다. 지역의 "추락 시 안전 바닥"들은 y -8 부근이므로
    // 그보다 훨씬 아래(-15)를 기준으로 잡아 정상 플레이와 겹치지 않게 한다.
    public class VoidRespawn : MonoBehaviour
    {
        [SerializeField] float voidY = -15f;

        void Update()
        {
            if (transform.position.y >= voidY) return;
            if (GameManager.Instance == null) return;

            GameManager.Instance.RespawnPlayer();
        }
    }
}
