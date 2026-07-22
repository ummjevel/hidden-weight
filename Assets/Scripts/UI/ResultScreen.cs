using UnityEngine;
using UnityEngine.UI;
using HanGame.Common;

namespace HanGame.UI
{
    /// <summary>
    /// 결과 화면. 기획서 14.4.
    /// 최종 도달 층·처리 업무 수·획득 무기·레벨·남은 평판·야간 성공 횟수·플레이 시간.
    /// GameManager 상태가 Result가 되면 표시.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button retryButton;

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnState;
            if (panel != null) panel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        }

        private void OnState(GameState state)
        {
            if (state == GameState.Result) Show();
        }

        private void Show()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (panel != null) panel.SetActive(true);
            if (summaryText != null && run != null)
            {
                summaryText.text =
                    $"도달 층: {run.Floor}층\n" +
                    $"처리한 업무: {run.TasksProcessed}\n" +
                    $"보유 무기: {run.Weapons.Count}종\n" +
                    $"최종 레벨: Lv.{run.PlayerLevel}\n" +
                    $"남은 평판: {run.Reputation}\n" +
                    $"야간 탐방 성공: {run.NightClears}회\n" +
                    $"플레이 시간: {Mathf.FloorToInt(run.PlayTime)}초";
            }
        }

        private void OnRetry()
        {
            if (panel != null) panel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.StartNewRun();
        }
    }
}
