using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.Ending
{
    // 3단계 클리어 뒤 엔딩 영상을 재생하고 타이틀로 복귀한다.
    public class EndingSequence : MonoBehaviour
    {
        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Ending);

            PlayerInput.Enabled = false;
            CinematicVideoPlayer.Play("final_end_scene.mp4", ReturnToTitle);
        }

        void OnDestroy()
        {
            // 시퀀스 도중 씬이 강제로 내려가는 등 예외 상황에서도 다음 씬의 입력이
            // 막힌 채로 남지 않도록 방어적으로 복구한다.
            PlayerInput.Enabled = true;
        }

        void ReturnToTitle()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Title);
            SceneFlow.LoadWithFade(SceneFlow.Title);
        }

    }
}
