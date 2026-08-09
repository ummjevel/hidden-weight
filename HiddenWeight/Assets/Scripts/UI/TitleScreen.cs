using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 타이틀 화면. 제목 "Hidden Weight" + 부제 "눈뜨는 꿈", 버튼 "시작하기"/"종료".
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] Sprite backdropArt;
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

            BuildBackdrop(canvasGO.transform);

            var brand = UIBuilder.CreateMenuPanel(canvasGO.transform, "TitleBrandPanel",
                new Color(0.040f, 0.036f, 0.068f, 0.90f));
            var brandRt = brand.rectTransform;
            brandRt.anchorMin = brandRt.anchorMax = new Vector2(0.33f, 0.54f);
            brandRt.sizeDelta = new Vector2(720f, 500f);
            brandRt.anchoredPosition = Vector2.zero;

            var eyebrow = UIBuilder.CreateMenuText(brand.transform, "TitleEyebrow",
                "A DREAM OF MEMORY", 17, TextAnchor.MiddleLeft, true);
            var eyebrowRt = eyebrow.rectTransform;
            eyebrowRt.anchorMin = eyebrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            eyebrowRt.sizeDelta = new Vector2(570f, 36f);
            eyebrowRt.anchoredPosition = new Vector2(0f, 154f);
            eyebrow.color = new Color(UIBuilder.MenuEdge.r, UIBuilder.MenuEdge.g, UIBuilder.MenuEdge.b, 0.88f);

            var title = UIBuilder.CreateMenuText(brand.transform, "Title_Main", "Hidden Weight", 76,
                TextAnchor.MiddleLeft, true);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(570f, 112f);
            titleRt.anchoredPosition = new Vector2(0f, 82f);

            UIBuilder.AddDivider(brand.transform, new Vector2(0.5f, 0.5f), new Vector2(540f, 2f),
                new Vector2(0f, 16f));

            var subtitle = UIBuilder.CreateMenuText(brand.transform, "Title_Subtitle", "눈뜨는 꿈", 30,
                TextAnchor.MiddleLeft, true);
            var subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(570f, 52f);
            subRt.anchoredPosition = new Vector2(0f, -36f);

            var copy = UIBuilder.CreateMenuText(brand.transform, "Title_Copy",
                "흩어진 기억의 끝에서,\n비로소 나를 마주하는 이야기", 21, TextAnchor.UpperLeft);
            copy.lineSpacing = 1.35f;
            copy.color = UIBuilder.MenuTextMuted;
            var copyRt = copy.rectTransform;
            copyRt.anchorMin = copyRt.anchorMax = new Vector2(0.5f, 0.5f);
            copyRt.sizeDelta = new Vector2(570f, 90f);
            copyRt.anchoredPosition = new Vector2(0f, -116f);

            var menu = UIBuilder.CreateMenuPanel(canvasGO.transform, "TitleMenuPanel", UIBuilder.MenuGlass);
            var menuRt = menu.rectTransform;
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0.77f, 0.52f);
            menuRt.sizeDelta = new Vector2(420f, SaveService.HasSave ? 570f : 505f);
            menuRt.anchoredPosition = Vector2.zero;

            var menuHeading = UIBuilder.CreateMenuText(menu.transform, "MenuHeading", "기억을 열다", 26,
                TextAnchor.MiddleCenter, true);
            var headingRt = menuHeading.rectTransform;
            headingRt.anchorMin = headingRt.anchorMax = new Vector2(0.5f, 1f);
            headingRt.sizeDelta = new Vector2(320f, 62f);
            headingRt.anchoredPosition = new Vector2(0f, -54f);

            UIBuilder.AddDivider(menu.transform, new Vector2(0.5f, 1f), new Vector2(300f, 2f),
                new Vector2(0f, -94f));

            float firstY = SaveService.HasSave ? 126f : 92f;
            if (SaveService.HasSave)
            {
                _continueButton = UIBuilder.CreateButton(menu.transform, "이어하기", firstY, ContinueGame);
                firstY -= 70f;
            }
            _newGameButton = UIBuilder.CreateButton(menu.transform, "새 게임", firstY, StartGame);
            _settingsButton = UIBuilder.CreateButton(menu.transform, "설정", firstY - 70f, ShowSettings);
            _creditsButton = UIBuilder.CreateButton(menu.transform, "제작진", firstY - 140f, ShowCredits);
            UIBuilder.CreateButton(menu.transform, "종료", firstY - 210f, Quit);

            var version = UIBuilder.CreateMenuText(canvasGO.transform, "BuildVersion",
                "Hidden Weight  ·  " + Application.version, 14, TextAnchor.MiddleRight);
            version.color = new Color(1f, 1f, 1f, 0.40f);
            var versionRt = version.rectTransform;
            versionRt.anchorMin = versionRt.anchorMax = new Vector2(1f, 0f);
            versionRt.pivot = new Vector2(1f, 0f);
            versionRt.sizeDelta = new Vector2(360f, 32f);
            versionRt.anchoredPosition = new Vector2(-34f, 22f);

            _dialog = canvasGO.AddComponent<ConfirmDialog>();
            _sections = canvasGO.AddComponent<PauseSectionPanel>();
        }

        void BuildBackdrop(Transform parent)
        {
            var backdrop = new GameObject("TitleBackdrop", typeof(RectTransform));
            backdrop.transform.SetParent(parent, false);
            var rt = (RectTransform)backdrop.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = backdrop.AddComponent<Image>();
            image.sprite = backdropArt != null ? backdropArt : UIBuilder.MenuGradient;
            image.type = Image.Type.Simple;
            image.color = backdropArt != null
                ? new Color(0.50f, 0.54f, 0.68f, 0.62f)
                : Color.white;

            // 기억 파편처럼 보이는 정적인 작은 마름모. 반복 애니메이션 없이도 빈 검정 배경을
            // 채우며, 동작 줄이기 설정과 충돌하지 않는다.
            for (int i = 0; i < 18; i++)
            {
                var shard = new GameObject("MemoryDust_" + i, typeof(RectTransform));
                shard.transform.SetParent(backdrop.transform, false);
                var shardRt = (RectTransform)shard.transform;
                float x = ((i * 47) % 101) / 100f;
                float y = ((i * 71 + 13) % 97) / 96f;
                shardRt.anchorMin = shardRt.anchorMax = new Vector2(x, y);
                float size = 3f + (i % 4) * 2f;
                shardRt.sizeDelta = new Vector2(size, size);
                shardRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
                shard.AddComponent<Image>().color = new Color(0.67f, 0.73f, 0.94f, 0.10f + (i % 3) * 0.04f);
            }

            var veil = new GameObject("TitleVeil", typeof(RectTransform));
            veil.transform.SetParent(backdrop.transform, false);
            var veilRt = (RectTransform)veil.transform;
            veilRt.anchorMin = Vector2.zero;
            veilRt.anchorMax = Vector2.one;
            veilRt.offsetMin = veilRt.offsetMax = Vector2.zero;
            veil.AddComponent<Image>().color = new Color(0.01f, 0.012f, 0.025f, 0.18f);
        }

    }
}
