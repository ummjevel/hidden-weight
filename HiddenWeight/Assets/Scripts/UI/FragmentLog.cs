using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 화면 하단 중앙에 파편 텍스트를 한 줄, 페이드 인 → 유지 → 페이드 아웃으로 띄운다.
    // TMP 패키지 추가를 피하기 위해 TextMeshProUGUI가 아니라 uGUI Text를 쓴다.
    // DontDestroyOnLoad 싱글턴. Awake에서 GameManager.FragmentPresenter에 자신을 등록해
    // World → UI 참조 없이 StoryFragment.Collect()가 이 클래스로 위임되게 한다.
    public class FragmentLog : MonoBehaviour
    {
        public static FragmentLog Instance { get; private set; }

        Text _text;
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
            GameManager.FragmentPresenter = s => Show(s);
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("FragmentLogCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasGO.AddComponent<CanvasScaler>();

            var textGO = new GameObject("FragmentText");
            textGO.transform.SetParent(canvasGO.transform, false);
            _text = textGO.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 28;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.color = new Color(1f, 1f, 1f, 0f);
            _text.text = string.Empty;

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0f);
            rt.anchorMax = new Vector2(0.9f, 0.15f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // 연달아 호출되면 앞의 것을 즉시 끝내고 새 것을 띄운다(코루틴 교체).
        public void Show(string text, float seconds = 4f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(text, seconds));
        }

        IEnumerator ShowRoutine(string text, float seconds)
        {
            _text.text = text;
            yield return Fade(1f, 0.3f);
            yield return new WaitForSecondsRealtime(seconds);
            yield return Fade(0f, 0.5f);
            _routine = null;
        }

        IEnumerator Fade(float target, float duration)
        {
            float start = _text.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var c = _text.color;
                c.a = Mathf.Lerp(start, target, t / duration);
                _text.color = c;
                yield return null;
            }
            var final = _text.color;
            final.a = target;
            _text.color = final;
        }
    }
}
