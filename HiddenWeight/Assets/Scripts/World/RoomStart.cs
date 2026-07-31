using UnityEngine;

namespace HiddenWeight.World
{
    // 문을 거치지 않고 이 방에 들어올 때 플레이어가 서는 자리.
    // 지역 첫 진입, 체크포인트 복귀, 테스트가 특정 방을 바로 띄울 때 쓴다.
    // 위치만 있으면 되므로 필드가 없다.
    public class RoomStart : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1.4f, 0f));
        }
    }
}
