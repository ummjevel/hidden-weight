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

        void Update()
        {
            bool open = IsOpen;
            if (blocker != null) blocker.SetActive(!open);
            if (hintIcon != null) hintIcon.enabled = !open; // 닫혀 있을 때만 필요 스킬 아이콘을 보여준다
        }
    }
}
