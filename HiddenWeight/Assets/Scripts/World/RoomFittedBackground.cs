using UnityEngine;

namespace HiddenWeight.World
{
    // 방 배경을 방 경계에 한 번 맞춰 두고 월드에 고정한다.
    //
    // 예전(CameraLockedRoomBackground)에는 매 프레임 카메라 위치로 배경을 옮겼다. 카메라가
    // 방 중앙에 붙어 있을 때는 티가 나지 않았지만, 카메라가 방 안을 움직이는 순간 배경도 같이
    // 따라와서 공간이 통째로 미끄러져 보였고, 월드에 고정된 벽·천장을 그려 줄 수도 없었다.
    // 이제 배경은 방 크기의 그림이고, 카메라가 그 그림의 일부를 들여다본다.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RoomFittedBackground : MonoBehaviour
    {
        void Start() => Fit();

        public void Fit()
        {
            var room = GetComponentInParent<Room>();
            if (room == null) return;

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            var bounds = room.WorldBounds;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);

            // 방과 배경의 가로세로비가 조금씩 다르다. 축마다 따로 늘리면 그림이 일그러지므로
            // 균일 배율로 방을 덮고 넘치는 가장자리는 그대로 둔다 — 카메라가 방 경계 안으로
            // 클램프되니 넘친 부분은 화면에 들어오지 않는다.
            float scale = Mathf.Max(bounds.size.x / spriteSize.x, bounds.size.y / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
