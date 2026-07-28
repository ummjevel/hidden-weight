using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.Emotions;

namespace HiddenWeight.UI
{
    // 플레이 중 상단에 HP·현재 감정 스킬(쿨타임 포함)·되감기 채널링 게이지·수집 파편 수를 보여준다.
    // GameManager.State가 Playing이 아니면 캔버스를 감춘다.
    public class HUD : MonoBehaviour
    {
        const int HeartCount = 3;

        GameObject _canvasGO;
        Image[] _hearts;
        GameObject _skillGroup;
        Text _skillText;
        GameObject _rewindGaugeGO;
        Image _rewindGaugeFill;
        Text _fragmentText;

        PlayerHealth _health;

        void Awake()
        {
            BuildHierarchy();
        }

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= HandleStateChanged;
            if (_health != null) _health.HealthChanged -= HandleHealthChanged;
        }

        void Start()
        {
            ApplyVisibility(GameManager.Instance != null ? GameManager.Instance.State : GameState.Boot);
        }

        void Update()
        {
            if (_health == null) TryBindPlayerHealth();
            UpdateSkillDisplay();
            UpdateFragmentCount();
        }

        void TryBindPlayerHealth()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;

            _health = pc.GetComponent<PlayerHealth>();
            if (_health == null) return;

            _health.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(_health.Current, _health.Max); // 초기 상태 동기화
        }

        void HandleHealthChanged(int current, int max)
        {
            // 성장 조각으로 최대 체력이 늘면 하트도 그만큼 보여야 한다. 미리 만들어 둔 칸이
            // 부족하면 늘어난 만큼은 표시하지 못하므로, 최소한 max 범위까지는 켠다.
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].enabled = i < Mathf.Min(current, _hearts.Length);
        }

        void HandleStateChanged(GameState next) => ApplyVisibility(next);

        void ApplyVisibility(GameState state)
        {
            _canvasGO.SetActive(state == GameState.Playing);
        }

        void UpdateSkillDisplay()
        {
            var active = EmotionSkillController.Instance != null ? EmotionSkillController.Instance.Active : null;

            if (active == null)
            {
                _skillGroup.SetActive(false);
                _rewindGaugeGO.SetActive(false);
                return;
            }

            _skillGroup.SetActive(true);
            _skillText.text = active.CooldownRemaining > 0f
                ? $"{active.Data.displayName} ({active.CooldownRemaining:F1})"
                : active.Data.displayName;

            bool showGauge = active is RewindSkill rewind && rewind.IsActive;
            _rewindGaugeGO.SetActive(showGauge);
            if (showGauge) _rewindGaugeFill.fillAmount = ((RewindSkill)active).ChannelProgress;
        }

        void UpdateFragmentCount()
        {
            if (GameManager.Instance == null) return;
            _fragmentText.text = $"파편 {GameManager.Instance.Progress.FragmentCount}   재화 {GameManager.Instance.Progress.Currency}";
        }

        void BuildHierarchy()
        {
            _canvasGO = new GameObject("HUDCanvas");
            _canvasGO.transform.SetParent(transform, false);
            var canvas = _canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            _canvasGO.AddComponent<CanvasScaler>();

            BuildHearts(_canvasGO.transform);
            BuildSkillGroup(_canvasGO.transform);
            BuildFragmentText(_canvasGO.transform);
        }

        void BuildHearts(Transform parent)
        {
            _hearts = new Image[HeartCount];
            for (int i = 0; i < HeartCount; i++)
            {
                var go = new GameObject($"Heart{i}");
                go.transform.SetParent(parent, false);
                var img = go.AddComponent<Image>();
                img.color = Color.red;

                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(32f, 32f);
                rt.anchoredPosition = new Vector2(16f + i * 40f, -16f);

                _hearts[i] = img;
            }
        }

        void BuildSkillGroup(Transform parent)
        {
            // 자식들이 화면 기준으로 앵커링될 수 있도록 화면 전체를 덮는 RectTransform으로 만든다.
            _skillGroup = new GameObject("SkillGroup", typeof(RectTransform));
            _skillGroup.transform.SetParent(parent, false);
            var groupRt = (RectTransform)_skillGroup.transform;
            groupRt.anchorMin = Vector2.zero;
            groupRt.anchorMax = Vector2.one;
            groupRt.offsetMin = Vector2.zero;
            groupRt.offsetMax = Vector2.zero;

            _skillText = CreateText(_skillGroup.transform, "SkillText", 22, TextAnchor.UpperRight);
            var textRt = _skillText.rectTransform;
            textRt.anchorMin = textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(1f, 1f);
            textRt.sizeDelta = new Vector2(260f, 32f);
            textRt.anchoredPosition = new Vector2(-16f, -16f);

            _rewindGaugeGO = new GameObject("RewindGauge");
            _rewindGaugeGO.transform.SetParent(_skillGroup.transform, false);
            var bgImg = _rewindGaugeGO.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.5f);
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.pivot = new Vector2(1f, 1f);
            bgRt.sizeDelta = new Vector2(200f, 12f);
            bgRt.anchoredPosition = new Vector2(-16f, -52f);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(_rewindGaugeGO.transform, false);
            _rewindGaugeFill = fillGO.AddComponent<Image>();
            _rewindGaugeFill.color = Color.cyan;
            _rewindGaugeFill.type = Image.Type.Filled;
            _rewindGaugeFill.fillMethod = Image.FillMethod.Horizontal;
            _rewindGaugeFill.fillAmount = 0f;
            var fillRt = _rewindGaugeFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            _skillGroup.SetActive(false);
        }

        void BuildFragmentText(Transform parent)
        {
            _fragmentText = CreateText(parent, "FragmentText", 22, TextAnchor.LowerLeft);
            var rt = _fragmentText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(200f, 32f);
            rt.anchoredPosition = new Vector2(16f, 16f);
        }

        static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }
    }
}
