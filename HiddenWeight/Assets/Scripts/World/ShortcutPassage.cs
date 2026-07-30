using UnityEngine;
using HiddenWeight.Player;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 열린 숏컷을 실제 양방향 통로로 만든다. 메뉴 순간이동이 아니라 월드 안의 문/승강기
    // 트리거를 밟아 반대편 출구로 이동하며, 짧은 잠금으로 왕복 튕김을 막는다.
    [RequireComponent(typeof(Collider2D))]
    public class ShortcutPassage : MonoBehaviour
    {
        [SerializeField] Shortcut shortcut;
        [SerializeField] Transform destination;
        [SerializeField] Vector2 arrivalOffset = new Vector2(0f, 0.8f);

        static float _nextUseTime;

        public Shortcut RequiredShortcut => shortcut;
        public Transform Destination => destination;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _nextUseTime = 0f;

        public void Configure(Shortcut requiredShortcut, Transform target, Vector2 offset)
        {
            shortcut = requiredShortcut;
            destination = target;
            arrivalOffset = offset;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject) || destination == null) return;
            if (shortcut != null && !shortcut.IsOpen) return;
            if (!PlayerInput.InteractPressed) return;
            if (Time.unscaledTime < _nextUseTime) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            _nextUseTime = Time.unscaledTime + 0.75f;
            player.TeleportTo((Vector2)destination.position + arrivalOffset);
        }
    }
}
