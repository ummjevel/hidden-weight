using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Enemies;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 생존에 필요한 체력과 현재 감정만 상시 표시하고, 채널링·보스 정보는 상황 중에만 띄운다.
    // 파편·재화 총량은 전투 판단 정보가 아니므로 기억 기록/토스트로 이동한다.
    public class HUD : MonoBehaviour
    {
        [SerializeField] Sprite heartSprite;

        // 상태 문양 프레임(ResidueStatusUI_v1의 세 행). HUD는 캔버스를 런타임에 짓기 때문에
        // 프레임을 프리팹이 들고 있다가 StatusEmblem에 넘겨 준다.
        [SerializeField] Sprite[] rewindStatusFrames;
        [SerializeField] Sprite[] dangerStatusFrames;
        [SerializeField] Sprite[] progressStatusFrames;

        StatusEmblem _statusEmblem;
        float _lastCooldown;
        bool _dangerShown;
        ZoneData _statusZone;

        GameObject _canvasGO;
        RectTransform _canvasRect;
        readonly List<Image> _hearts = new List<Image>();

        GameObject _skillGroup;
        Text _skillGlyph;
        Text _skillName;
        Image _cooldownFill;

        GameObject _channelGroup;
        Image _channelFill;

        GameObject _bossGroup;
        Text _bossName;
        Image _bossHealthFill;

        PlayerHealth _health;
        Enemy _boss;

        void Awake() => BuildHierarchy();

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += HandleStateChanged;
            Encounter.EncounterStateChanged += HandleEncounterStateChanged;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= HandleStateChanged;
            Encounter.EncounterStateChanged -= HandleEncounterStateChanged;
            if (_health != null) _health.HealthChanged -= HandleHealthChanged;
            UnbindBoss();
        }

        void Start() => ApplyVisibility(GameManager.Instance != null ? GameManager.Instance.State : GameState.Boot);

        void Update()
        {
            if (_health == null) TryBindPlayerHealth();
            RefreshStatusFramesForZone();
            UpdateSkillDisplay();
        }

        void TryBindPlayerHealth()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;

            _health = pc.GetComponent<PlayerHealth>();
            if (_health == null) return;

            _health.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(_health.Current, _health.Max);
        }

        void HandleHealthChanged(int current, int max)
        {
            // 위험 문양은 마지막 한 칸이 남았을 때만 켜고, 회복하면 끈다. 체력 숫자를 읽지
            // 않아도 "이제 한 대만 더 맞으면 끝난다"가 화면에서 보여야 한다.
            bool critical = current > 0 && current <= 1;
            if (critical != _dangerShown && _statusEmblem != null)
            {
                _dangerShown = critical;
                if (critical) _statusEmblem.Play("StatusDanger");
                else _statusEmblem.Stop("StatusDanger");
            }

            EnsureHeartCount(max);
            for (int i = 0; i < _hearts.Count; i++)
            {
                bool withinMax = i < max;
                _hearts[i].gameObject.SetActive(withinMax);
                if (withinMax) _hearts[i].color = i < current ? UIBuilder.HeartFull : UIBuilder.HeartEmpty;
            }
        }

        void EnsureHeartCount(int max)
        {
            while (_hearts.Count < max)
            {
                int index = _hearts.Count;
                var go = new GameObject($"HealthCore{index}");
                go.transform.SetParent(_canvasGO.transform, false);
                var image = go.AddComponent<Image>();
                image.sprite = heartSprite;
                image.color = UIBuilder.HeartEmpty;

                var rt = image.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(34f, 34f);
                rt.anchoredPosition = new Vector2(32f + index * 42f, -32f);
                _hearts.Add(image);
            }
        }

        // 지역이 바뀌면 상태 문양 프레임을 그 지역 것으로 갈아끼운다. ZoneData가 비워 두면
        // HUD 프리팹의 기본 프레임(잔재)을 그대로 쓴다.
        void RefreshStatusFramesForZone()
        {
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            if (zone == _statusZone || _statusEmblem == null) return;

            _statusZone = zone;
            ConfigureStatusEmblem(zone);
        }

        void ConfigureStatusEmblem(ZoneData zone)
        {
            Sprite[] Pick(Sprite[] zoneFrames, Sprite[] fallback)
                => zoneFrames != null && zoneFrames.Length > 0 ? zoneFrames : fallback;

            _statusEmblem.Configure(
                new StatusEmblem.Sequence
                {
                    name = "StatusRewind", fps = 10f, loop = false,
                    frames = Pick(zone != null ? zone.statusRewindFrames : null, rewindStatusFrames),
                },
                new StatusEmblem.Sequence
                {
                    name = "StatusDanger", fps = 12f, loop = true,
                    frames = Pick(zone != null ? zone.statusDangerFrames : null, dangerStatusFrames),
                },
                new StatusEmblem.Sequence
                {
                    name = "StatusProgress", fps = 10f, loop = false,
                    frames = Pick(zone != null ? zone.statusProgressFrames : null, progressStatusFrames),
                });
        }

        void HandleStateChanged(GameState next) => ApplyVisibility(next);

        void ApplyVisibility(GameState state) => _canvasGO.SetActive(state == GameState.Playing);

        void UpdateSkillDisplay()
        {
            var active = EmotionSkillController.Instance != null ? EmotionSkillController.Instance.Active : null;
            if (active == null)
            {
                _skillGroup.SetActive(false);
                _channelGroup.SetActive(false);
                return;
            }

            _skillGroup.SetActive(true);
            _skillName.text = active.Data.displayName;
            _skillGlyph.text = GlyphFor(active.Id);

            float cooldown = active.Data.cooldown;
            _cooldownFill.fillAmount = cooldown > 0f
                ? Mathf.Clamp01(active.CooldownRemaining / cooldown)
                : 0f;

            // 스킬을 막 쓴 순간(쿨타임이 0에서 올라간 순간)에 되감기 문양을 한 번 돌린다.
            // 시트의 한 행이 충전 → 준비 → 발동 → 소진을 통째로 담고 있어서, 사용 시점에
            // 한 번 재생하는 것으로 그 흐름이 그대로 읽힌다.
            // 위험 문양이 켜져 있으면 건드리지 않는다 — 생존 정보가 우선이다.
            if (_statusEmblem != null && !_dangerShown
                && _lastCooldown <= 0.01f && active.CooldownRemaining > 0.01f)
                _statusEmblem.Play("StatusRewind");
            _lastCooldown = active.CooldownRemaining;

            if (active is RewindSkill rewind && rewind.IsActive && rewind.CurrentTarget != null)
            {
                _channelGroup.SetActive(true);
                _channelFill.fillAmount = rewind.ChannelProgress;
                PositionChannel(rewind.CurrentTarget.position);
            }
            else
            {
                _channelGroup.SetActive(false);
            }
        }

        static string GlyphFor(EmotionId id)
        {
            switch (id)
            {
                case EmotionId.Rewind: return "↶";
                case EmotionId.Hush: return "◉";
                case EmotionId.Foresight: return "◇";
                default: return "○";
            }
        }

        void PositionChannel(Vector3 worldPosition)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
            {
                _channelGroup.SetActive(false);
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screen, null, out var local))
                ((RectTransform)_channelGroup.transform).anchoredPosition = local;
        }

        void HandleEncounterStateChanged(Encounter encounter, bool active)
        {
            if (!active || encounter == null || encounter.BossEnemy == null)
            {
                if (_boss == null || encounter == null || encounter.BossEnemy == _boss) UnbindBoss();
                return;
            }

            // 보스 조우가 시작되는 순간을 진행 문양으로 한 번 알린다(시트 3행의 "보스 경고").
            // 위험 문양이 켜져 있으면 덮지 않는다 — 생존 정보가 우선이다.
            if (_statusEmblem != null && !_dangerShown) _statusEmblem.Play("StatusProgress");

            UnbindBoss();
            _boss = encounter.BossEnemy;
            _bossName.text = encounter.DisplayName;
            _boss.HealthChanged += HandleBossHealthChanged;
            _bossGroup.SetActive(true);
            HandleBossHealthChanged(_boss.Health, _boss.Data.maxHealth);
        }

        void HandleBossHealthChanged(int current, int max)
        {
            _bossHealthFill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
        }

        void UnbindBoss()
        {
            if (_boss != null) _boss.HealthChanged -= HandleBossHealthChanged;
            _boss = null;
            if (_bossGroup != null) _bossGroup.SetActive(false);
        }

        void BuildHierarchy()
        {
            _canvasGO = new GameObject("HUDCanvas", typeof(RectTransform));
            _canvasGO.transform.SetParent(transform, false);
            var canvas = _canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            UIBuilder.ConfigureScaler(_canvasGO.AddComponent<CanvasScaler>());
            _canvasRect = _canvasGO.GetComponent<RectTransform>();

            BuildSkillGroup(_canvasGO.transform);
            BuildChannelGroup(_canvasGO.transform);
            BuildBossGroup(_canvasGO.transform);
            BuildStatusEmblem(_canvasGO.transform);
        }

        // 체력 코어 바로 아래. 상시 표시가 아니라 사건이 있을 때만 켜지므로 자리를 많이 쓰지 않는다.
        void BuildStatusEmblem(Transform parent)
        {
            var go = new GameObject("StatusEmblem", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(56f, 56f);
            rt.anchoredPosition = new Vector2(32f, -78f);

            _statusEmblem = go.AddComponent<StatusEmblem>();
            ConfigureStatusEmblem(GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null);
        }

        void BuildSkillGroup(Transform parent)
        {
            _skillGroup = new GameObject("EmotionStatus", typeof(RectTransform));
            _skillGroup.transform.SetParent(parent, false);
            var groupRt = (RectTransform)_skillGroup.transform;
            groupRt.anchorMin = groupRt.anchorMax = new Vector2(1f, 1f);
            groupRt.pivot = new Vector2(1f, 1f);
            groupRt.sizeDelta = new Vector2(250f, 72f);
            groupRt.anchoredPosition = new Vector2(-32f, -32f);

            var panel = _skillGroup.AddComponent<Image>();
            panel.color = new Color(0.025f, 0.035f, 0.05f, 0.68f);

            var glyphBg = new GameObject("EmotionGlyph", typeof(RectTransform));
            glyphBg.transform.SetParent(_skillGroup.transform, false);
            var glyphBgImage = glyphBg.AddComponent<Image>();
            glyphBgImage.color = new Color(0.35f, 0.85f, 0.86f, 0.28f);
            var glyphRt = glyphBgImage.rectTransform;
            glyphRt.anchorMin = glyphRt.anchorMax = new Vector2(1f, 0.5f);
            glyphRt.pivot = new Vector2(1f, 0.5f);
            glyphRt.sizeDelta = new Vector2(52f, 52f);
            glyphRt.anchoredPosition = new Vector2(-10f, 0f);

            _skillGlyph = UIBuilder.CreateText(glyphBg.transform, "GlyphText", 32, TextAnchor.MiddleCenter);
            var glyphTextRt = _skillGlyph.rectTransform;
            glyphTextRt.anchorMin = Vector2.zero;
            glyphTextRt.anchorMax = Vector2.one;
            glyphTextRt.offsetMin = Vector2.zero;
            glyphTextRt.offsetMax = Vector2.zero;

            var cooldown = new GameObject("CooldownRing", typeof(RectTransform));
            cooldown.transform.SetParent(glyphBg.transform, false);
            _cooldownFill = cooldown.AddComponent<Image>();
            _cooldownFill.color = new Color(0.02f, 0.025f, 0.035f, 0.72f);
            _cooldownFill.type = Image.Type.Filled;
            _cooldownFill.fillMethod = Image.FillMethod.Radial360;
            _cooldownFill.fillOrigin = 2;
            _cooldownFill.fillClockwise = false;
            var cooldownRt = _cooldownFill.rectTransform;
            cooldownRt.anchorMin = Vector2.zero;
            cooldownRt.anchorMax = Vector2.one;
            cooldownRt.offsetMin = Vector2.zero;
            cooldownRt.offsetMax = Vector2.zero;

            _skillName = UIBuilder.CreateText(_skillGroup.transform, "EmotionName", 24, TextAnchor.MiddleRight);
            var nameRt = _skillName.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(12f, 8f);
            nameRt.offsetMax = new Vector2(-72f, -8f);
            _skillGroup.SetActive(false);
        }

        void BuildChannelGroup(Transform parent)
        {
            _channelGroup = new GameObject("WorldChannel", typeof(RectTransform));
            _channelGroup.transform.SetParent(parent, false);
            var rt = (RectTransform)_channelGroup.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(84f, 84f);

            var background = _channelGroup.AddComponent<Image>();
            background.color = new Color(0.1f, 0.08f, 0.03f, 0.4f);

            var fill = new GameObject("ChannelFill", typeof(RectTransform));
            fill.transform.SetParent(_channelGroup.transform, false);
            _channelFill = fill.AddComponent<Image>();
            _channelFill.color = new Color(1f, 0.78f, 0.28f, 0.9f);
            _channelFill.type = Image.Type.Filled;
            _channelFill.fillMethod = Image.FillMethod.Radial360;
            _channelFill.fillOrigin = 2;
            _channelFill.fillClockwise = false;
            var fillRt = _channelFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(6f, 6f);
            fillRt.offsetMax = new Vector2(-6f, -6f);
            _channelGroup.SetActive(false);
        }

        void BuildBossGroup(Transform parent)
        {
            _bossGroup = new GameObject("BossHUD", typeof(RectTransform));
            _bossGroup.transform.SetParent(parent, false);
            var groupRt = (RectTransform)_bossGroup.transform;
            groupRt.anchorMin = groupRt.anchorMax = new Vector2(0.5f, 0f);
            groupRt.pivot = new Vector2(0.5f, 0f);
            groupRt.sizeDelta = new Vector2(760f, 70f);
            groupRt.anchoredPosition = new Vector2(0f, 42f);

            _bossName = UIBuilder.CreateText(_bossGroup.transform, "BossName", 24, TextAnchor.MiddleCenter);
            var nameRt = _bossName.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(0f, 8f);
            nameRt.offsetMax = Vector2.zero;

            var healthBg = new GameObject("BossHealthBackground", typeof(RectTransform));
            healthBg.transform.SetParent(_bossGroup.transform, false);
            var bg = healthBg.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.14f);
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 0f);
            bgRt.anchorMax = new Vector2(1f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.sizeDelta = new Vector2(0f, 12f);
            bgRt.anchoredPosition = Vector2.zero;

            var healthFill = new GameObject("BossHealthFill", typeof(RectTransform));
            healthFill.transform.SetParent(healthBg.transform, false);
            _bossHealthFill = healthFill.AddComponent<Image>();
            _bossHealthFill.color = new Color(0.86f, 0.78f, 0.72f, 0.95f);
            _bossHealthFill.type = Image.Type.Filled;
            _bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
            var fillRt = _bossHealthFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _bossGroup.SetActive(false);
        }
    }
}
