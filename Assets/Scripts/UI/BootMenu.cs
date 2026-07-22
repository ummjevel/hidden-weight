using UnityEngine;
using UnityEngine.UI;
using HanGame.Common;

namespace HanGame.UI
{
    /// <summary>
    /// 타이틀/부팅 화면. 첫 출근 시작 버튼. 기획서 2.1 진입점.
    /// Boot 씬에 배치. Start 누르면 GameManager.StartNewRun()으로 1층 낮 시작.
    /// </summary>
    public class BootMenu : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnStart()
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewRun();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
