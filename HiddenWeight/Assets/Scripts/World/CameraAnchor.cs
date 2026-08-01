using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    public enum CameraAnchorMode
    {
        Fixed,     // 퍼즐방·작은 방. 구도를 완전히 고정한다
        Landmark,  // 거대한 얼굴·손바닥 같은 서사 구간. 천천히 지정 구도로 옮긴다
        Combat,    // 전장 중심 고정. 필요하면 살짝 줌아웃한다
        Exit,      // 위치는 뺏지 않고 예측 거리와 추적 속도만 올린다
    }

    // 방 안의 한 구간에서 카메라 구도를 대신 정하는 트리거 볼륨.
    //
    // 왜 컴포넌트로 두는가: 랜드마크·전투·퍼즐 구도는 방마다 손으로 잡아야 하는 연출이라
    // 코드에서 유추할 수 없다. 겹칠 때는 나중에 들어간 앵커가 이긴다 — 전투방 안의 랜드마크처럼
    // 좁은 구간이 넓은 구간을 덮어쓰는 게 자연스럽다.
    [RequireComponent(typeof(Collider2D))]
    public class CameraAnchor : MonoBehaviour
    {
        [SerializeField] CameraAnchorMode mode = CameraAnchorMode.Landmark;

        [Tooltip("앵커 위치에서 카메라가 실제로 볼 지점까지의 오프셋. 플레이어를 화면 " +
                 "왼쪽·아래로 밀어 두고 싶을 때 쓴다.")]
        [SerializeField] Vector2 focusOffset;

        [Tooltip("0이면 기본 화면 크기를 유지한다. 강한 조우 6.25, 보스전 6.5.")]
        [SerializeField] float orthographicSizeOverride;

        [Tooltip("이 구도로 옮겨 가는 데 걸리는 시간. 서사 구간 0.6, 전투 0.35.")]
        [SerializeField] float blendSeconds = 0.6f;

        [Tooltip("카메라가 플레이어에게서 떨어질 수 있는 최대 거리. 0 이하면 제한 없음(완전 고정).")]
        [SerializeField] float maxLeash = 2.5f;

        public CameraAnchorMode Mode => mode;
        public Vector2 FocusPoint => (Vector2)transform.position + focusOffset;
        public float SizeOverride => orthographicSizeOverride;
        public float BlendSeconds => blendSeconds;
        public float MaxLeash => maxLeash;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            if (RoomCamera.Instance != null) RoomCamera.Instance.PushAnchor(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            if (RoomCamera.Instance != null) RoomCamera.Instance.PopAnchor(this);
        }

        // 방이 언로드되거나 앵커가 꺼질 때 카메라가 사라진 구도에 매달려 있지 않게 한다.
        void OnDisable()
        {
            if (RoomCamera.Instance != null) RoomCamera.Instance.PopAnchor(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = mode == CameraAnchorMode.Exit ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(FocusPoint, 0.4f);
            Gizmos.DrawLine(transform.position, FocusPoint);
        }
    }
}
