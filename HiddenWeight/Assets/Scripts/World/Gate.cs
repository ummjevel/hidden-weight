using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 특정 스킬(혹은 최종 조건)을 만족해야 열리는 문.
    public class Gate : MonoBehaviour
    {
        [SerializeField] EmotionId requiredSkill;
        [SerializeField] bool requiresFinalCondition; // 잔재 백트래킹 최종 파편용
        [SerializeField] GameObject blocker;          // 실제로 길을 막는 콜라이더 오브젝트
        [SerializeField] SpriteRenderer hintIcon;      // 필요 스킬 아이콘. 없으면 무시

        public EmotionId RequiredSkill => requiredSkill;

        public bool IsOpen
        {
            get
            {
                var p = GameManager.Instance.Progress;
                return requiresFinalCondition ? p.CanOpenFinalGate() : p.CanOpenGate(requiredSkill);
            }
        }

        // 첫 Update 전에는 열림 여부를 모르므로 null로 둔다. 그래야 이미 열린 채로 시작한
        // 문이 씬 진입 순간에 열리는 소리를 내지 않는다.
        bool? _wasOpen;

        void Update()
        {
            bool open = IsOpen;
            if (blocker != null) blocker.SetActive(!open);
            if (hintIcon != null) hintIcon.enabled = !open; // 닫혀 있을 때만 필요 스킬 아이콘을 보여준다

            // 문은 스킬을 얻은 순간 조용히 상태만 바뀐다. 소리가 없으면 지나온 길이 열린 걸
            // 모르고 되돌아가지 않는다.
            if (_wasOpen.HasValue && _wasOpen.Value != open)
                AudioManager.Instance?.PlaySfx(open ? SfxCue.GateOpen : SfxCue.GateClose, 0.6f);
            _wasOpen = open;
        }
    }
}
