using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 타이틀 화면. 제목 "Hidden Weight" + 부제 "눈뜨는 꿈", 버튼 "시작하기"/"종료".
    public class TitleScreen : MonoBehaviour
    {
        Button _newGameButton;
        Button _creditsButton;
        Button _settingsButton;
        ConfirmDialog _dialog;
        PauseSectionPanel _sections;

        void Awake()
        {
            BuildHierarchy();
        }

        void Start()
        {
            // 타이틀 씬에 있다는 사실 자체가 곧 Title 상태다. 콜드 부트 등으로 아직
            // 상태가 반영되지 않았을 수 있으므로 여기서 명시적으로 맞춘다.
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Title);
            StartCoroutine(SelectInitialButton());
        }

        IEnumerator SelectInitialButton()
        {
            // EventSystem.Start가 이 컴포넌트보다 늦을 수 있으므로 한 프레임 뒤 포커스를 준다.
            yield return null;
            UIBuilder.Select(_newGameButton);
        }

        void StartGame()
        {
            GameManager.Instance.Progress.ResetAll();
            GameManager.Instance.SetState(GameState.Playing);
            SceneFlow.LoadWithFade(SceneFlow.Prologue);
        }

        // 작업 중인 지역으로 바로 들어가는 개발용 입구. 정식 빌드에는 절대 노출하지 않는다.
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

        void ShowCredits()
        {
            _dialog.ShowInfo(
                "제작진",
                "Hidden Weight\n\n기획 · 개발 · 아트\nHidden Weight Team",
                _creditsButton);
        }

        void ShowSettings() => _sections.Show(PauseSection.Settings);

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("TitleCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            UIBuilder.ConfigureScaler(canvasGO.AddComponent<CanvasScaler>());
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

            _newGameButton = UIBuilder.CreateButton(canvasGO.transform, "새 게임", -20f, StartGame);
            _settingsButton = UIBuilder.CreateButton(canvasGO.transform, "설정", -90f, ShowSettings);
            _creditsButton = UIBuilder.CreateButton(canvasGO.transform, "제작진", -160f, ShowCredits);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UIBuilder.CreateButton(canvasGO.transform, "잔재 지역 (개발용)", -230f, StartResidueTest);
            UIBuilder.CreateButton(canvasGO.transform, "종료", -300f, Quit);
#else
            UIBuilder.CreateButton(canvasGO.transform, "종료", -230f, Quit);
#endif

            _dialog = canvasGO.AddComponent<ConfirmDialog>();
            _sections = canvasGO.AddComponent<PauseSectionPanel>();
        }
    }
}
