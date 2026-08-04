using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.UI
{
    // 타이틀 화면. 제목 "Hidden Weight" + 부제 "눈뜨는 꿈", 버튼 "시작하기"/"종료".
    public class TitleScreen : MonoBehaviour
    {
        Button _newGameButton;
        Button _continueButton;
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
            UIBuilder.Select(_continueButton != null ? _continueButton : _newGameButton);
        }

        void StartGame()
        {
            if (SaveService.HasSave)
            {
                _dialog.ShowConfirm("새 기억 시작", "이어가던 기억을 놓고 처음부터 시작할까요?",
                    "새로 시작", BeginNewGame, _newGameButton);
                return;
            }
            BeginNewGame();
        }

        void BeginNewGame() => GameManager.Instance.BeginNewGame();

        void ContinueGame()
        {
            if (GameManager.Instance.ContinueGame()) return;
            _dialog.ShowInfo("기억을 열 수 없습니다",
                "저장된 기억이 손상되었습니다. 새 게임을 시작하면 새 기억으로 교체됩니다.",
                _continueButton != null ? _continueButton : _newGameButton);
        }

        // 지역으로 바로 들어가는 QA용 입구. 정식 빌드에는 절대 노출하지 않는다.
        // 잔재는 R05에서 되감기를 처음 배우는 흐름까지 검수해야 하므로 선해금하지 않는다.
        // 응시·균열 바로가기는 기존 QA 편의를 유지한다(이번 작업 범위 밖).
        void StartZoneTest(string sceneName)
        {
            var progress = GameManager.Instance.Progress;
            progress.ResetAll();
            if (!sceneName.Contains("Residue"))
            {
                progress.UnlockSkill(EmotionId.Rewind);
                progress.UnlockSkill(EmotionId.Hush);
                progress.UnlockSkill(EmotionId.Foresight);
                progress.GrantAwareness();
            }

            GameManager.Instance.SetState(GameState.Playing);
            SceneFlow.LoadWithFade(sceneName);
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

        void LateUpdate()
        {
            // 설정 패널의 뒤로 버튼은 공용 패널 내부에서 닫히므로 타이틀 버튼 포커스를 모른다.
            // 닫힌 뒤 선택이 비어 있으면 설정 버튼으로 돌려 홈 화면의 강조가 깨끗하게 하나만 남게 한다.
            if (_sections != null && !_sections.IsVisible && _settingsButton != null
                && UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
                UIBuilder.Select(_settingsButton);
        }

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

            float firstY = -20f;
            if (SaveService.HasSave)
            {
                _continueButton = UIBuilder.CreateButton(canvasGO.transform, "이어하기", firstY, ContinueGame);
                firstY -= 70f;
            }
            _newGameButton = UIBuilder.CreateButton(canvasGO.transform, "새 게임", firstY, StartGame);
            _settingsButton = UIBuilder.CreateButton(canvasGO.transform, "설정", firstY - 70f, ShowSettings);
            _creditsButton = UIBuilder.CreateButton(canvasGO.transform, "제작진", firstY - 140f, ShowCredits);

            BuildStageRow(canvasGO.transform, firstY - 225f);
            UIBuilder.CreateButton(canvasGO.transform, "종료", firstY - 295f, Quit);

            _dialog = canvasGO.AddComponent<ConfirmDialog>();
            _sections = canvasGO.AddComponent<PauseSectionPanel>();
        }

        // 테스트용 단계 바로가기. 정식 진행 저장은 건드리지 않고 해당 단계에 필요한 능력만
        // 임시로 열어 바로 맵과 전투를 검수할 수 있게 한다.
        void BuildStageRow(Transform parent, float y)
        {
            var caption = UIBuilder.CreateText(parent, "단계 선택 · 테스트", 15);
            var captionRt = caption.rectTransform;
            captionRt.anchorMin = captionRt.anchorMax = new Vector2(0.5f, 0.5f);
            captionRt.sizeDelta = new Vector2(400f, 24f);
            captionRt.anchoredPosition = new Vector2(0f, y + 42f);
            caption.color = new Color(1f, 1f, 1f, 0.45f);

            var zones = new (string label, string scene)[]
            {
                // 현재 QA가 완료된 한 장짜리 잔재 씬을 실제 진행과 테스트 진입 모두에서 쓴다.
                ("1단계 · 잔재", SceneFlow.Residue),
                ("2단계 · 응시", "Zone_Gaze"),
                ("3단계 · 균열", "Zone_Fracture_Full"),
            };

            for (int i = 0; i < zones.Length; i++)
            {
                string scene = zones[i].scene; // 클로저가 루프 변수를 공유하지 않게 복사
                var button = UIBuilder.CreateButton(parent, zones[i].label, y, () => StartZoneTest(scene));
                var rt = (RectTransform)button.transform;
                rt.sizeDelta = new Vector2(190f, 48f);
                rt.anchoredPosition = new Vector2((i - (zones.Length - 1) * 0.5f) * 200f, y);
            }
        }
    }
}
