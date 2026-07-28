using UnityEngine;
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
        [SerializeField] SpriteRenderer visual;   // 열리면 색이 바뀐다
        [SerializeField] Color openedTint = new Color(0.9f, 0.85f, 0.6f);

        void Start()
        {
            if (target != null && target.IsOpen) MarkOpened();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            if (target == null || target.IsOpen) return;

            target.Open();
            MarkOpened();
        }

        void MarkOpened()
        {
            if (visual != null) visual.color = openedTint;
        }
    }
}
