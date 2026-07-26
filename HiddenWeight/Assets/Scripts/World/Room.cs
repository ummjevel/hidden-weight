using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 룸 단위 카메라 전환용 경계. 레벨 배치 시 씬에 배치하고 size로 룸 크기를 지정한다.
    [RequireComponent(typeof(BoxCollider2D))]
    public class Room : MonoBehaviour
    {
        [SerializeField] Vector2 size = new Vector2(24, 14);

        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(size.x, size.y, 1f));

        public bool Contains(Vector3 point)
        {
            var b = WorldBounds;
            return point.x >= b.min.x && point.x <= b.max.x
                && point.y >= b.min.y && point.y <= b.max.y;
        }

        void OnValidate()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            col.isTrigger = true;
            col.size = size;
            col.offset = Vector2.zero;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            RoomCamera.Instance.SetRoom(this);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
