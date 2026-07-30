using UnityEngine;

namespace HiddenWeight.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraLockedRoomBackground : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera != null)
                Refresh(camera);
        }

        public void Refresh(Camera camera)
        {
            if (camera == null || !camera.orthographic)
                return;

            transform.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                transform.position.z);

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float requiredHeight = camera.orthographicSize * 2f;
            float requiredWidth = requiredHeight * camera.aspect;
            float scale = Mathf.Max(
                requiredWidth / spriteSize.x,
                requiredHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
