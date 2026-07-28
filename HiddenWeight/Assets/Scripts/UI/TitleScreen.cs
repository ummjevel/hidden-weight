using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 타이틀 화면. 제목 "Hidden Weight" + 부제 "눈뜨는 꿈", 버튼 "시작하기"/"종료".
    public class TitleScreen : MonoBehaviour
    {
        void Awake()
        {
            BuildHierarchy();
        }

        void Start()
        {
            // 타이틀 씬에 있다는 사실 자체가 곧 Title 상태다. 콜드 부트 등으로 아직
            // 상태가 반영되지 않았을 수 있으므로 여기서 명시적으로 맞춘다.
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Title);
        }

        void StartGame()
        {
            GameManager.Instance.Progress.ResetAll();
            GameManager.Instance.SetState(GameState.Playing);
            SceneFlow.LoadWithFade(SceneFlow.Prologue);
        }

        // 작업 중인 잔재 재설계 지역(15룸)으로 바로 들어간다. 빌드한 앱만으로도 새 지역을
        // 확인할 수 있게 두는 개발용 입구다. 정식 동선(프롤로그→잔재→응시)에는 아직 연결되어
        // 있지 않으므로, 지역이 완성되어 Zone_Residue를 교체하는 시점에 이 버튼을 지운다.
        void StartResidueTest()
        {
            GameManager.Instance.Progress.ResetAll();
            GameManager.Instance.SetState(GameState.Playing);
            SceneFlow.LoadWithFade("Zone_Residue_Full");
        }

        void Quit()
        {
            Application.Quit();
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("TitleCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var title = UIBuilder.CreateText(canvasGO.transform, "Hidden Weight", 56);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.65f);
            titleRt.sizeDelta = new Vector2(700f, 100f);
            titleRt.anchoredPosition = Vector2.zero;

            var subtitle = UIBuilder.CreateText(canvasGO.transform, "눈뜨는 꿈", 24);
            var subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.55f);
            subRt.sizeDelta = new Vector2(500f, 50f);
            subRt.anchoredPosition = Vector2.zero;

            UIBuilder.CreateButton(canvasGO.transform, "시작하기", -20f, StartGame);
            UIBuilder.CreateButton(canvasGO.transform, "잔재 지역 (작업 중)", -90f, StartResidueTest);
            UIBuilder.CreateButton(canvasGO.transform, "종료", -160f, Quit);
        }
    }
}
