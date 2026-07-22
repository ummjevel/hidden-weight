using System.Collections.Generic;
using RookieToCEO.Core;
using RookieToCEO.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RookieToCEO.UI
{
    // GDD 4번 "레벨업 강화 3개 중 하나 선택" 화면.
    // 선택 로직/시간정지는 이미 DayWaveManager(IsPaused/PendingChoices/ResolveStatChoice)가
    // 가지고 있으므로, 이 스크립트는 그 상태를 화면에 보여주고 버튼 클릭을 연결하기만 한다.
    public class LevelUpChoiceUI : MonoBehaviour
    {
        // GDD 4번 표시 명칭 그대로.
        private static readonly Dictionary<StatType, string> DisplayNames = new Dictionary<StatType, string>
        {
            { StatType.WorkPower, "업무처리력" },
            { StatType.HandSpeed, "손속도" },
            { StatType.Awareness, "눈치" },
            { StatType.MentalCare, "멘탈 관리" },
            { StatType.WorkSense, "일머리" },
            { StatType.Seniority, "짬" },
        };

        [SerializeField] private DayWaveManager dayWaveManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] choiceButtons; // 3개, choiceLabels와 순서 대응
        [SerializeField] private Text[] choiceLabels;

        private bool _wasPaused;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (dayWaveManager == null || panelRoot == null) return;

            var isPaused = dayWaveManager.IsPaused;

            // 방금 레벨업으로 멈춘 순간에만 버튼 내용을 새로 채운다(매 프레임 다시 채울 필요 없음).
            if (isPaused && !_wasPaused)
            {
                PopulateChoices();
            }

            if (isPaused != panelRoot.activeSelf)
            {
                panelRoot.SetActive(isPaused);
            }

            _wasPaused = isPaused;
        }

        private void PopulateChoices()
        {
            var choices = dayWaveManager.PendingChoices;

            for (var i = 0; i < choiceButtons.Length; i++)
            {
                var hasChoice = choices != null && i < choices.Count;
                choiceButtons[i].gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;

                var stat = choices[i];
                choiceLabels[i].text = DisplayNames.TryGetValue(stat, out var name) ? name : stat.ToString();

                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => dayWaveManager.ResolveStatChoice(stat));
            }
        }
    }
}
