using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // PlayerInput.PausePressed(Enabled 여부와 무관하게 항상 동작)로 토글되는 일시정지 화면.
    // 열릴 때 GameManager.SetState(Paused) + PlayerInput.Enabled = false,
    // 닫힐 때 Playing + PlayerInput.Enabled = true.
    public class PauseMenu : MonoBehaviour
    {
        GameObject _root;

        void Awake()
        {
            BuildHierarchy();
        }

        void Start()
        {
            // 이미 Paused 상태로 씬이 시작하는 경우는 없지만, 방어적으로 현재 상태를 반영한다.
            _root.SetActive(GameManager.Instance != null && GameManager.Instance.State == GameState.Paused);
        }

        void Update()
        {
            if (!PlayerInput.PausePressed) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Playing) Open();
            else if (gm.State == GameState.Paused) Close();
        }

        void Open()
        {
            _root.SetActive(true);
            PlayerInput.Enabled = false;
            GameManager.Instance.SetState(GameState.Paused);
        }

        void Close()
        {
            _root.SetActive(false);
            PlayerInput.Enabled = true;
            GameManager.Instance.SetState(GameState.Playing);
        }

        void GoToTitle()
        {
            _root.SetActive(false);
            PlayerInput.Enabled = true;
            GameManager.Instance.Progress.ResetAll();
            // 타이틀 씬으로 넘어가므로 timeScale을 되돌리는 의미로도 상태를 Title로 되돌린다.
            GameManager.Instance.SetState(GameState.Title);
            SceneFlow.LoadWithFade(SceneFlow.Title);
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("PauseCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            _root = new GameObject("PausePanel", typeof(RectTransform));
            _root.transform.SetParent(canvasGO.transform, false);
            var panelRt = (RectTransform)_root.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            var title = CreateText(_root.transform, "일시정지", 36);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.65f);
            titleRt.sizeDelta = new Vector2(400f, 60f);
            titleRt.anchoredPosition = Vector2.zero;

            CreateButton(_root.transform, "계속하기", -20f, Close);
            CreateButton(_root.transform, "타이틀로", -90f, GoToTitle);

            _root.SetActive(false);
        }

        static Text CreateText(Transform parent, string content, int fontSize)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        static void CreateButton(Transform parent, string label, float yPos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 56f);
            rt.anchoredPosition = new Vector2(0f, yPos);

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, label, 24);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }
    }
}
