using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.World
{
    // 물리적으로 열리는 숏컷(사슬다리·승강기·문). 할로우 나이트식으로, 한 번 열면 영구히 열려
    // 있고 지역을 다시 들어와도 열린 채 시작한다(RESIDUE_LEVEL_DESIGN.md 숏컷 A/B/C).
    //
    // 열리는 조건은 이 컴포넌트가 판단하지 않는다. 되감기 대상(Rewindable)이든 보스 승리든
    // 바깥에서 Open()을 부르면 된다 — 잠긴 상태를 만드는 방식과 저장하는 방식을 분리해 둔다.
    public class Shortcut : MonoBehaviour
    {
        [SerializeField] string shortcutId;   // 예: residue_shortcut_a
        [SerializeField] GameObject blocker;  // 닫혀 있을 때 길을 막는 콜라이더
        [SerializeField] GameObject openedVisual; // 열렸을 때만 보이는 다리·승강기 본체

        // 열리고 닫히는 장치의 상태 애니메이션(ResidueRoomTransitions_v1).
        // 없으면 지금까지처럼 blocker/openedVisual을 켜고 끄는 것으로 끝난다.
        [SerializeField] SpriteAnimator transitionAnimator;
        [SerializeField] string closedClip = "SealClose";
        [SerializeField] string openClip = "SealOpen";

        public string Id => shortcutId;
        public bool IsOpen { get; private set; }

        public void Configure(string id, GameObject closedBlocker = null, GameObject openVisual = null)
        {
            shortcutId = id;
            if (closedBlocker != null) blocker = closedBlocker;
            if (openVisual != null) openedVisual = openVisual;
        }

        void Start()
        {
            // 이전 방문에서 이미 열었으면 연출 없이 열린 상태로 시작한다.
            if (GameManager.Instance != null && GameManager.Instance.Progress.IsShortcutOpen(shortcutId))
                Apply(true);
            else
                Apply(false);
        }

        public void Open()
        {
            if (IsOpen) return;

            Apply(true);
            AudioManager.Instance?.PlaySfx(SfxCue.ShortcutOpen, 0.7f);
            if (GameManager.Instance != null) GameManager.Instance.Progress.MarkShortcutOpen(shortcutId);
        }

        void PlayTransition(bool open)
        {
            if (transitionAnimator == null) return;

            string clip = open ? openClip : closedClip;
            if (!string.IsNullOrEmpty(clip) && transitionAnimator.Has(clip))
                transitionAnimator.Play(clip, true);
        }

        void Apply(bool open)
        {
            IsOpen = open;
            if (blocker != null) blocker.SetActive(!open);
            if (openedVisual != null) openedVisual.SetActive(open);
            PlayTransition(open);
        }
    }
}
