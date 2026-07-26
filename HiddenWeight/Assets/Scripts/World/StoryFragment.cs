using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;

namespace HiddenWeight.World
{
    // 지역 곳곳의 이야기 파편. 스킬 해금·자각 해금 지점으로도 쓰인다.
    [RequireComponent(typeof(Collider2D))]
    public class StoryFragment : MonoBehaviour
    {
        [SerializeField] string fragmentId; // 지역별 고유 문자열 (residue_01 등)
        [SerializeField, TextArea(2, 4)] string text; // 화면에 뜰 한 줄
        [SerializeField] EmotionId grantsSkill = EmotionId.None; // 스킬 획득 지점으로도 쓴다
        [SerializeField] bool grantsAwareness; // 응시 지역의 자각 해금 지점

        public string FragmentId => fragmentId;

        protected virtual bool IsCollectable => true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            Collect();
        }

        public virtual void Collect()
        {
            if (!IsCollectable) return;
            var p = GameManager.Instance.Progress;
            if (!p.CollectFragment(fragmentId)) return; // 이미 먹은 것
            if (grantsSkill != EmotionId.None) p.UnlockSkill(grantsSkill);
            if (grantsAwareness) p.GrantAwareness();
            Debug.Log(text);
            gameObject.SetActive(false);
            EmotionSkillController.Instance?.RefreshActive();
        }
    }
}
