using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 룸 단위로 플레이어를 따라가는 카메라. 결과 위치를 현재 룸 경계 안으로 클램프한다.
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        // 카메라 추적 부드러움 계수. 밸런스 수치가 아니라 연출용 값이라 여기 인스펙터 필드로 둔다.
        [SerializeField] float followLerp = 8f;

        Camera _cam;

        public static RoomCamera Instance { get; private set; }
        public Room CurrentRoom { get; private set; }

        void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return; // 씬에 플레이어가 아직 없을 수 있다

            Vector2 target = ComputeClampedTarget(player.transform.position);
            transform.position = Vector3.Lerp(transform.position, new Vector3(target.x, target.y, transform.position.z), followLerp * Time.deltaTime);
        }

        // CurrentRoom 경계 안으로 클램프된 카메라 목표 위치를 계산한다.
        // CurrentRoom이 없으면 원래 목표(플레이어 위치)를 그대로 돌려준다.
        Vector2 ComputeClampedTarget(Vector2 target)
        {
            if (CurrentRoom == null) return target;

            var half = new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);
            var b = CurrentRoom.WorldBounds;
            // 룸이 화면보다 작으면 룸 중심에 고정한다.
            float x = b.size.x <= half.x * 2f
                ? b.center.x
                : Mathf.Clamp(target.x, b.min.x + half.x, b.max.x - half.x);
            float y = b.size.y <= half.y * 2f
                ? b.center.y
                : Mathf.Clamp(target.y, b.min.y + half.y, b.max.y - half.y);
            return new Vector2(x, y);
        }

        // CurrentRoom을 바꾸기만 한다. 급전환은 LateUpdate의 Lerp가 자연히 흡수한다.
        public void SetRoom(Room room)
        {
            CurrentRoom = room;
        }

        // Lerp 없이 즉시 플레이어 위치로 이동한다 (씬 진입·리스폰용).
        public void SnapToPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 target = ComputeClampedTarget(player.transform.position);
            transform.position = new Vector3(target.x, target.y, transform.position.z);
        }
    }
}
