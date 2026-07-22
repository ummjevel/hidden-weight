using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HanGame.Data;
using HanGame.Day;

namespace HanGame.UI
{
    /// <summary>
    /// 레벨업 강화 선택 패널. 기획서 7.3.
    /// 서로 다른 강화 3개 표시, 선택 시 DayCombatManager.ResolveLevelUp 호출.
    /// 시간제한 없음. 시간 정지는 DayCombatManager가 처리.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [System.Serializable]
        public struct OptionSlot
        {
            public GameObject root;
            public Text title;
            public Text description;
            public Image icon;
            public Button button;
        }

        [SerializeField] private GameObject panel;
        [SerializeField] private OptionSlot[] slots; // 3개

        private DayCombatManager _manager;
        private List<UpgradeData> _current;

        private void Start()
        {
            _manager = FindObjectOfType<DayCombatManager>();
            if (_manager != null) _manager.LevelUpOffered += Show;
            if (panel != null) panel.SetActive(false);
        }

        private void Show(List<UpgradeData> options)
        {
            _current = options;
            if (panel != null) panel.SetActive(true);

            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < options.Count;
                if (slots[i].root != null) slots[i].root.SetActive(has);
                if (!has) continue;

                var u = options[i];
                if (slots[i].title != null) slots[i].title.text = u.displayName;
                if (slots[i].description != null) slots[i].description.text = u.description;
                if (slots[i].icon != null && u.icon != null) slots[i].icon.sprite = u.icon;

                int captured = i;
                if (slots[i].button != null)
                {
                    slots[i].button.onClick.RemoveAllListeners();
                    slots[i].button.onClick.AddListener(() => Pick(captured));
                }
            }
        }

        private void Pick(int index)
        {
            if (_current == null || index >= _current.Count) return;
            var picked = _current[index];
            if (panel != null) panel.SetActive(false);
            if (_manager != null) _manager.ResolveLevelUp(picked);
        }
    }
}
