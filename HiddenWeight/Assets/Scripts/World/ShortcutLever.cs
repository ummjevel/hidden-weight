using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 안쪽에서 직접 여는 숏컷 장치. 응시 G05의 "안쪽에서 여는 사슬막"(숏컷 A)이 이것이다
    // (GAZE_LEVEL_DESIGN.md 8.2절).
    //
    // Shortcut은 "열린 상태를 만들고 저장하는" 쪽만 맡고 여는 조건은 바깥이 정한다는 규칙을
    // 그대로 따른다. 잔재는 되감기 대상이, 균열은 미래 문(FutureEcho)과 승강기가, 보스 방은
    // Encounter 승리가 같은 자리를 채운다 — 지역마다 조건만 다르고 결과는 하나다.
    [RequireComponent(typeof(Collider2D))]
    public class ShortcutLever : MonoBehaviour
    {
        [SerializeField] Shortcut target;
        // 방이 씬으로 갈라진 뒤로 레버와 숏컷은 서로 다른 씬에 산다(G05→G03). 유니티는 씬을
        // 넘는 오브젝트 참조를 저장하지 못해 target이 null로 구워지므로, Rewindable과 같은
        // 방식으로 id 기반 대안을 둔다(ResidueZoneBuilder LinkRewindToShortcut과 동일 이유).
        [SerializeField] string targetShortcutId;
        [SerializeField] SpriteRenderer visual;   // 열리면 색이 바뀐다
        [SerializeField] Color openedTint = new Color(0.9f, 0.85f, 0.6f);

        void Start()
        {
            if (target != null && target.IsOpen) MarkOpened();
            else if (target == null && IsLinkedShortcutOpen()) MarkOpened();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            if (target != null)
            {
                if (target.IsOpen) return;
                target.Open();
                MarkOpened();
                return;
            }

            // 숏컷이 다른 방 씬에 있어 지금 메모리에 없는 경우다. 진행 상태에만 열림을 남긴다
            // (Rewindable.TryOpenLinkedShortcut과 같은 이유 — 효과음·봉인 연출은 로드되지
            // 않은 씬에서 재생할 수 없으므로 일부러 생략한다).
            if (string.IsNullOrEmpty(targetShortcutId) || GameManager.Instance == null) return;
            if (GameManager.Instance.Progress.IsShortcutOpen(targetShortcutId)) return;

            GameManager.Instance.Progress.MarkShortcutOpen(targetShortcutId);
            MarkOpened();
        }

        bool IsLinkedShortcutOpen()
            => !string.IsNullOrEmpty(targetShortcutId) && GameManager.Instance != null
               && GameManager.Instance.Progress.IsShortcutOpen(targetShortcutId);

        void MarkOpened()
        {
            if (visual != null) visual.color = openedTint;
        }
    }
}
