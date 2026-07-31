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

        public void Configure(string id, string message, EmotionId skill = EmotionId.None, bool awareness = false)
        {
            fragmentId = id;
            text = message;
            grantsSkill = skill;
            grantsAwareness = awareness;
        }

        protected virtual bool IsCollectable => true;

        protected virtual void Start()
        {
            // 이미 먹은 파편은 오브젝트째 감춘다. 남겨 두면 지급은 막히지만 화면에는 계속 보여
            // "아직 안 먹은 것"처럼 읽힌다(재방문 시 혼란).
            if (GameManager.Instance != null && !string.IsNullOrEmpty(fragmentId)
                && GameManager.Instance.Progress.HasFragment(fragmentId))
                gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            Collect();
        }

        public virtual void Collect()
        {
            if (!IsCollectable) return;
            var p = GameManager.Instance.Progress;
            if (!p.CollectFragment(fragmentId, text)) return; // 이미 먹은 것
            if (grantsSkill != EmotionId.None) p.UnlockSkill(grantsSkill);
            if (grantsAwareness) p.GrantAwareness();
            AudioManager.Instance?.PlaySfx(SfxCue.Fragment, 0.75f);
            GameManager.FragmentPresenter?.Invoke(text);
            gameObject.SetActive(false);
            EmotionSkillController.Instance?.RefreshActive();
        }
    }
}
