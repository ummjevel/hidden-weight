using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;

namespace HiddenWeight.UI
{
    // 화면 하단 중앙에 파편 텍스트를 한 줄, 페이드 인 → 유지 → 페이드 아웃으로 띄운다.
    // TMP 패키지 추가를 피하기 위해 TextMeshProUGUI가 아니라 uGUI Text를 쓴다.
    // 씬 단위 싱글턴 (DontDestroyOnLoad 아님). 지역 씬마다 HUD 프리팹이 자기 FragmentLog를
    // 새로 들고 있으므로 씬 전환 때마다 자연스럽게 다시 만들어진다 — 예전에는 여기서
    // DontDestroyOnLoad(gameObject)를 호출했는데, HUD가 Zone 루트의 자식으로 배치돼 있어
    // (루트 오브젝트에서만 동작하는) DontDestroyOnLoad가 씬 로드마다 에러를 냈다. Awake에서
    // GameManager.FragmentPresenter에 자신을 등록해 World → UI 참조 없이
    // StoryFragment.Collect()가 이 클래스로 위임되게 한다 — 씬마다 다시 등록되어야 하므로
    // (이전 씬의 델리게이트는 그 씬과 함께 사라진다) 이 등록도 매 Awake에서 새로 한다.
    public class FragmentLog : MonoBehaviour
    {
        public static FragmentLog Instance { get; private set; }

        Text _text;
        Coroutine _routine;

        void Awake()
        {
            // 같은 씬 안에 중복 배치된 경우에만 방어한다. 이전 씬의 인스턴스는 씬 언로드로
            // 이미 파괴되어 있으므로(Unity의 오버로드 == 비교상 null과 동일하게 취급된다)
            // 여기서 Instance != null이 참이 되는 건 진짜 같은 씬 내 중복뿐이다.
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;

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
