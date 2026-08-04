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
            ApplyResiduePresentation();

            // 이미 먹은 파편은 오브젝트째 감춘다. 남겨 두면 지급은 막히지만 화면에는 계속 보여
            // "아직 안 먹은 것"처럼 읽힌다(재방문 시 혼란).
            if (GameManager.Instance != null && !string.IsNullOrEmpty(fragmentId)
                && GameManager.Instance.Progress.HasFragment(fragmentId))
                gameObject.SetActive(false);
        }

        void ApplyResiduePresentation()
        {
            // 잔재의 중요 파편은 StoryFragment 프리팹의 기본 흰 타일 대신 용도별 전용
            // 문양을 쓴다. id를 명시해 다른 지역과 일반 파편의 표현은 바꾸지 않는다.
            string resourcePath;
            string objectName;
            float worldSize;
            switch (fragmentId)
            {
                case "residue_skill":
                    resourcePath = "Art/Residue/UI/RewindSkillIcon";
                    objectName = "ResidueSkillIcon";
                    worldSize = 1.15f;
                    break;
                case "residue_r11":
                    resourcePath = "Art/Residue/UI/MemoryFragmentIcon";
                    objectName = "ResidueMemoryFragmentIcon";
                    worldSize = 1.1f;
                    break;
                case "residue_core":
                    resourcePath = "Art/Residue/UI/MemoryCoreIcon";
                    objectName = "ResidueMemoryCoreIcon";
                    worldSize = 1.25f;
                    break;
                default:
                    return;
            }

            var icon = Resources.Load<Sprite>(resourcePath);
            var placeholder = GetComponent<SpriteRenderer>();
            if (icon == null || placeholder == null) return;

            placeholder.enabled = false;
            var art = new GameObject(objectName);
            art.transform.SetParent(transform, false);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.color = Color.white;
            renderer.sortingLayerID = placeholder.sortingLayerID;
            renderer.sortingOrder = placeholder.sortingOrder;
            float sourceSize = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            if (sourceSize > 0f)
                art.transform.localScale = Vector3.one * (worldSize / sourceSize);
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
