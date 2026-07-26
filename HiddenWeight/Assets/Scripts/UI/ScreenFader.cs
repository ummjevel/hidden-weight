using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 씬 전환 시 화면을 검게 덮는 페이드. DontDestroyOnLoad 싱글턴이며, Canvas + 전체 화면 검은
    // Image를 코드로 만든다. Awake에서 SceneFlow.FadeLoader에 자신을 등록해
    // Core → UI 참조 없이 SceneFlow.LoadWithFade가 이 클래스로 위임되게 한다.
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        Image _image;
        Coroutine _routine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildHierarchy();
            SceneFlow.FadeLoader = FadeAndLoad;
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("FaderCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();

            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);
            _image = imageGO.AddComponent<Image>();
            _image.color = new Color(0f, 0f, 0f, 0f);
            _image.raycastTarget = false; // 평소엔 입력을 가리지 않는다

            var rt = _image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void SetAlpha(float alpha)
        {
            var c = _image.color;
            c.a = Mathf.Clamp01(alpha);
            _image.color = c;
        }

        public IEnumerator FadeTo(float alpha, float seconds)
        {
            float start = _image.color.a;

            if (seconds <= 0f)
            {
                SetAlpha(alpha);
                yield break;
            }

            float t = 0f;
            while (t < seconds)
            {
                // 일시정지(Time.timeScale = 0) 중에도 페이드가 진행되어야 하므로 unscaled 시간을 쓴다.
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, alpha, t / seconds));
                yield return null;
            }

            SetAlpha(alpha);
        }

        public void FadeAndLoad(string sceneName, float seconds = 0.5f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeAndLoadRoutine(sceneName, seconds));
        }

        IEnumerator FadeAndLoadRoutine(string sceneName, float seconds)
        {
            yield return FadeTo(1f, seconds);
            SceneManager.LoadScene(sceneName);
            yield return FadeTo(0f, seconds);
            _routine = null;
        }
    }
}
