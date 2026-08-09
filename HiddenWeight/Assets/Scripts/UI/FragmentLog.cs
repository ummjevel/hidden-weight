using System.Collections;
using System.Collections.Generic;
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
        readonly Queue<Entry> _queue = new Queue<Entry>();

        struct Entry
        {
            public string text;
            public float seconds;
        }

        public int PendingCount => _queue.Count + (_routine != null ? 1 : 0);
        public string CurrentText => _text != null ? _text.text : string.Empty;

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

        void OnEnable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Progress.CurrencyChanged += HandleCurrencyChanged;
            GameManager.Instance.Progress.HealthShardsChanged += HandleHealthShardsChanged;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Progress.CurrencyChanged -= HandleCurrencyChanged;
                GameManager.Instance.Progress.HealthShardsChanged -= HandleHealthShardsChanged;
            }
            if (Instance == this) GameManager.FragmentPresenter = null;
        }

        void HandleCurrencyChanged(int amount, int _) => Show($"+{amount} 재화", 1.5f);
        void HandleHealthShardsChanged(int _) => Show("체력 조각을 품었습니다.", 2.5f);

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("FragmentLogCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            UIBuilder.ConfigureScaler(canvasGO.AddComponent<CanvasScaler>());

            var textGO = new GameObject("FragmentText");
            textGO.transform.SetParent(canvasGO.transform, false);
            _text = textGO.AddComponent<Text>();
            _text.font = UIBuilder.MenuRegular;
            _text.fontSize = 28;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            var textColor = UIBuilder.TextPrimary;
            textColor.a = 0f; // 평소엔 투명 — Show()가 페이드 인 시킨다
            _text.color = textColor;
            _text.text = string.Empty;

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0f);
            rt.anchorMax = new Vector2(0.9f, 0.15f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // 연속 획득도 앞 문장을 취소하지 않는다. 중요 메시지는 들어온 순서대로 모두 보여 준다.
        public void Show(string text, float seconds = 4f)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            float readingTime = Mathf.Max(seconds, text.Length * 0.12f) * UISettings.MessageDuration;
            _queue.Enqueue(new Entry { text = text, seconds = readingTime });
            if (_routine == null) _routine = StartCoroutine(ShowQueue());
        }

        IEnumerator ShowQueue()
        {
            while (_queue.Count > 0)
            {
                var entry = _queue.Dequeue();
                _text.text = entry.text;
                yield return Fade(1f, 0.3f);
                yield return new WaitForSecondsRealtime(entry.seconds);
                yield return Fade(0f, 0.35f);
            }
            _text.text = string.Empty;
            _routine = null;
        }

        IEnumerator Fade(float target, float duration)
        {
            if (UISettings.ReduceMotion)
            {
                var immediate = _text.color;
                immediate.a = target;
                _text.color = immediate;
                yield break;
            }
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
