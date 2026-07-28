using System.Collections;
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
        const float FadeDuration = 0.18f;

        GameObject _root;
        CanvasGroup _rootGroup;
        Coroutine _fadeRoutine;

        void Awake()
        {
            BuildHierarchy();
        }

        void Start()
        {
            // 이미 Paused 상태로 씬이 시작하는 경우는 없지만, 방어적으로 현재 상태를 반영한다.
            bool paused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
            _root.SetActive(paused);
            _rootGroup.alpha = paused ? 1f : 0f;
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
            PlayerInput.Enabled = false;
            GameManager.Instance.SetState(GameState.Paused);

            // GameManager.SetState가 Time.timeScale을 0으로 만들므로 페이드는 반드시
            // unscaledDeltaTime 기준으로 돌아야 한다(FragmentLog.Fade와 같은 이유).
            _root.SetActive(true);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(1f));
        }

        void Close()
        {
            PlayerInput.Enabled = true;
            GameManager.Instance.SetState(GameState.Playing);

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(0f));
        }

        IEnumerator FadeTo(float target)
        {
            float start = _rootGroup.alpha;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _rootGroup.alpha = Mathf.Lerp(start, target, t / FadeDuration);
                yield return null;
            }
            _rootGroup.alpha = target;

            // 꺼질 때만 마지막에 비활성화한다 — 켜질 때 미리 SetActive(true) 해 둬야
            // 알파가 실제로 0→1로 보간되는 걸 볼 수 있다(꺼진 오브젝트는 코루틴이 안 돈다).
            if (target <= 0f) _root.SetActive(false);
            _fadeRoutine = null;
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
            bg.color = UIBuilder.PanelBackground;

            _rootGroup = _root.AddComponent<CanvasGroup>();
            _rootGroup.alpha = 0f;

            var title = UIBuilder.CreateText(_root.transform, "일시정지", 36);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.65f);
            titleRt.sizeDelta = new Vector2(400f, 60f);
            titleRt.anchoredPosition = Vector2.zero;

            UIBuilder.CreateButton(_root.transform, "계속하기", -20f, Close);
            UIBuilder.CreateButton(_root.transform, "타이틀로", -90f, GoToTitle);

            _root.SetActive(false);
        }
    }
}
