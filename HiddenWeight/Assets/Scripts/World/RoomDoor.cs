using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 방 씬에 놓이는 포탈 문. 트리거에 닿으면 RoomLoader에 전환을 요청하는 것이 전부다.
    // 씬을 로드하는 일도 플레이어를 옮기는 일도 문의 책임이 아니다 — 그건 RoomLoader가
    // 혼자 소유해야 전환 도중의 상태를 한곳에서 관리할 수 있다.
    [RequireComponent(typeof(Collider2D))]
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] string doorId;
        [SerializeField] Side side;
        [SerializeField] string targetRoom;
        [SerializeField] string targetDoorId;
        [SerializeField] Vector2 arrivalOffset;
        // 비어 있으면 항상 통과 가능. 값이 있으면 그 숏컷이 열려야만 문이 반응한다
        // (RoomLink.requiredShortcutId 참고 — 물리적 숏컷 A/B/C용 문에 쓴다).
        [SerializeField] string requiredShortcutId;

        public string DoorId => doorId;
        public Side Side => side;
        public string TargetRoom => targetRoom;
        public string TargetDoorId => targetDoorId;
        public Vector2 ArrivalPosition => (Vector2)transform.position + arrivalOffset;

        // 도착한 문은 플레이어가 자기 트리거를 벗어날 때까지 발동하지 않는다.
        // 시간 쿨다운이 아니라 상태로 막아야 로드가 느려도, 문 앞에서 머뭇거려도 안전하다.
        public bool Armed { get; private set; } = true;

        public void Disarm() => Armed = false;

        public void Configure(string id, Side doorSide, string room, string targetDoor, Vector2 offset,
            string shortcutId = null)
        {
            doorId = id;
            side = doorSide;
            targetRoom = room;
            targetDoorId = targetDoor;
            arrivalOffset = offset;
            requiredShortcutId = shortcutId;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        bool ShortcutSatisfied()
            => string.IsNullOrEmpty(requiredShortcutId)
               || (GameManager.Instance != null && GameManager.Instance.Progress.IsShortcutOpen(requiredShortcutId));

        public static Vector2 DefaultArrivalOffset(Side side) => side switch
        {
            Side.W or Side.NW or Side.SW => new Vector2(1.5f, 0f),
            Side.E or Side.NE or Side.SE => new Vector2(-1.5f, 0f),
            Side.U => new Vector2(0f, -1.5f),
            Side.D => new Vector2(0f, 1.5f),
            _ => Vector2.zero,
        };

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!Armed || !PlayerLayers.IsPlayer(other.gameObject)) return;
            if (!ShortcutSatisfied()) return;
            RoomLoader.Instance?.RequestTransition(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            Armed = true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}
