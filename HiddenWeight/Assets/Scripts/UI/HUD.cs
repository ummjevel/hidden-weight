using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.UI
{
    // 생존에 필요한 체력과 현재 감정만 상시 표시하고, 채널링·보스 정보는 상황 중에만 띄운다.
    // 파편·재화 총량은 전투 판단 정보가 아니므로 기억 기록/토스트로 이동한다.
    public class HUD : MonoBehaviour
    {
        [SerializeField] Sprite heartSprite;
        [SerializeField] Sprite[] rewindStatusFrames;
        [SerializeField] Sprite[] dangerStatusFrames;
        [SerializeField] Sprite[] progressStatusFrames;

        StatusEmblem _statusEmblem;
        ZoneData _statusZone;
        float _lastCooldown;
        bool _dangerShown;
        AwarenessSystem _awareness;
        readonly Dictionary<GazeHazard, float> _gazeExposure = new Dictionary<GazeHazard, float>();

        GameObject _canvasGO;
        RectTransform _canvasRect;
        readonly List<Image> _hearts = new List<Image>();

        GameObject _skillGroup;
        Text _skillGlyph;
        Text _skillName;
        Image _cooldownFill;
        Image _cooldownInner;
        Image _skillIcon;
        EmotionId _lastSkillId = (EmotionId)(-1);
        float _skillNameTimer;

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
            GazeHazard.ExposureChanged += HandleGazeExposureChanged;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= HandleStateChanged;
            Encounter.EncounterStateChanged -= HandleEncounterStateChanged;
            GazeHazard.ExposureChanged -= HandleGazeExposureChanged;
            UnbindAwareness();
            if (_health != null) _health.HealthChanged -= HandleHealthChanged;
            UnbindBoss();
        }

        void Start() => ApplyVisibility(GameManager.Instance != null ? GameManager.Instance.State : GameState.Boot);

        void Update()
        {
            if (_health == null) TryBindPlayerHealth();
            TryBindAwareness();
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
            bool usesHealthDanger = _statusZone == null || _statusZone.id != ZoneId.Gaze;
            bool critical = usesHealthDanger && current > 0 && current <= 1;
            if (critical != _dangerShown && _statusEmblem != null)
            {
                _dangerShown = critical;
                if (critical) _statusEmblem.Play("StatusDanger");
                else _statusEmblem.Stop("StatusDanger");
            }

            EnsureHeartCount(max);
            // 한 핵이 담는 체력. 최대치가 핵 수로 나누어떨어지지 않아도 마지막 핵이
            // 나머지를 담으므로 총합은 항상 정확하다.
            float perCore = Mathf.Max(1f, (float)max / _hearts.Count);
            for (int i = 0; i < _hearts.Count; i++)
            {
                _hearts[i].gameObject.SetActive(true);
                float filled = Mathf.Clamp01((current - i * perCore) / perCore);
                _hearts[i].fillAmount = filled;
                _hearts[i].color = filled > 0f ? UIBuilder.HeartFull : UIBuilder.HeartEmpty;
            }
        }

        void RefreshStatusFramesForZone()
        {
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            if (zone == _statusZone || _statusEmblem == null) return;

            _statusZone = zone;
            _gazeExposure.Clear();
            _dangerShown = false;
            ConfigureStatusEmblem(zone);

            // 결정핵 색은 지역을 따라간다. 체력이 변할 때만 칠하면 지역을 넘어와도
            // 이전 지역 색이 남는다 — 체력이 가득 찬 채로 넘어오는 것이 보통이다.
            if (_health != null) HandleHealthChanged(_health.Current, _health.Max);
        }

        void ConfigureStatusEmblem(ZoneData zone)
        {
            Sprite[] Pick(Sprite[] regional, Sprite[] fallback)
                => regional != null && regional.Length > 0 ? regional : fallback;

            bool gaze = zone != null && zone.id == ZoneId.Gaze;
            _statusEmblem.Configure(
                new StatusEmblem.Sequence
                {
                    name = gaze ? "Awareness" : "Ability", fps = 10f, loop = false,
                    frames = Pick(zone != null ? zone.statusRewindFrames : null, rewindStatusFrames),
                },
                new StatusEmblem.Sequence
                {
                    name = gaze ? "Exposure" : "Danger", fps = 12f, loop = true,
                    frames = Pick(zone != null ? zone.statusDangerFrames : null, dangerStatusFrames),
                },
                new StatusEmblem.Sequence
                {
                    name = "Progress", fps = 10f, loop = false,
                    frames = Pick(zone != null ? zone.statusProgressFrames : null, progressStatusFrames),
                });

            // 기존 에셋 검증과 저장된 프리팹 이름을 깨지 않기 위한 호환 별칭.
            _statusEmblem.RegisterAlias("StatusRewind", gaze ? "Awareness" : "Ability");
            _statusEmblem.RegisterAlias("StatusDanger", gaze ? "Exposure" : "Danger");
            _statusEmblem.RegisterAlias("StatusProgress", "Progress");
        }

        void TryBindAwareness()
        {
            if (_awareness != null || AwarenessSystem.Instance == null) return;
            _awareness = AwarenessSystem.Instance;
            _awareness.AwarenessChanged += HandleAwarenessChanged;
        }

        void UnbindAwareness()
        {
            if (_awareness != null) _awareness.AwarenessChanged -= HandleAwarenessChanged;
            _awareness = null;
        }

        void HandleAwarenessChanged(bool active)
        {
            if (!active || _statusZone == null || _statusZone.id != ZoneId.Gaze || _dangerShown) return;
            _statusEmblem?.Play("Awareness");
        }

        void HandleGazeExposureChanged(GazeHazard hazard, float exposure, bool alarmed)
        {
            if (hazard == null) return;
            if (exposure <= 0f) _gazeExposure.Remove(hazard);
            else _gazeExposure[hazard] = exposure;

            if (_statusZone == null || _statusZone.id != ZoneId.Gaze || _statusEmblem == null) return;
            bool exposed = alarmed || _gazeExposure.Count > 0;
            if (exposed == _dangerShown) return;
            _dangerShown = exposed;
            if (exposed) _statusEmblem.Play("Exposure");
            else _statusEmblem.Stop("Exposure");
        }

        // 체력은 칸 하나에 1을 담지 않는다. 체력이 늘어날수록 화면 가로를 잠식해
        // 밝은 수채 배경 위에 붉은 픽셀 띠가 길게 깔렸다. 결정핵 다섯 개로 고정하고
        // 한 핵이 여러 칸을 나눠 담아, 부분 소모는 핵의 채움 정도로 보여 준다.
        const int HealthCores = 5;

        void EnsureHeartCount(int max)
        {
            int cores = Mathf.Clamp(max, 1, HealthCores);
            while (_hearts.Count < cores)
            {
                int index = _hearts.Count;
                var go = new GameObject($"HealthCore{index}");
                go.transform.SetParent(_canvasGO.transform, false);
                var image = go.AddComponent<Image>();
                image.sprite = heartSprite;
                image.color = UIBuilder.HeartEmpty;
                // 아래에서 위로 차오른다 — 씨앗이 자라는 방향이고, 가로 채움보다 작게 읽힌다.
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Vertical;
                image.fillOrigin = 0;

                var rt = image.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(26f, 26f);
                rt.anchoredPosition = new Vector2(32f + index * 32f, -32f);
                _hearts.Add(image);

                // 빈 핵도 자리를 알 수 있게 아주 옅은 바탕을 깔아 둔다.
                var socket = new GameObject($"HealthSocket{index}");
                socket.transform.SetParent(_canvasGO.transform, false);
                socket.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
                var socketImage = socket.AddComponent<Image>();
                socketImage.sprite = heartSprite;
                socketImage.color = new Color(1f, 1f, 1f, 0.14f);
                var socketRt = socketImage.rectTransform;
                socketRt.anchorMin = socketRt.anchorMax = rt.anchorMin;
                socketRt.pivot = rt.pivot;
                socketRt.sizeDelta = rt.sizeDelta;
                socketRt.anchoredPosition = rt.anchoredPosition;
            }
        }

        const float SkillNameSeconds = 2.4f;

        // 지역 UI 시트의 아이콘을 감정별로 하나씩 쓴다. 지역마다 시트가 다르므로
        // 같은 능력도 지역의 화풍으로 보인다.
        static Sprite SkillIcon(EmotionId id)
        {
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            if (zone == null || zone.mapStateIcons == null) return null;
            int index = id switch
            {
                EmotionId.Rewind => 5,
                EmotionId.Hush => 6,
                EmotionId.Foresight => 7,
                _ => -1,
            };
            return index >= 0 && index < zone.mapStateIcons.Length ? zone.mapStateIcons[index] : null;
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

            // 스킬 이름은 늘 띄우지 않는다. 획득 직후나 능력이 바뀐 순간에만 잠깐 보여주고
            // 이후에는 아이콘만 남긴다 — 매 순간 읽을 글자가 아니다.
            if (active.Id != _lastSkillId)
            {
                _lastSkillId = active.Id;
                _skillNameTimer = SkillNameSeconds;
                _skillName.text = active.Data.displayName;
            }
            if (_skillNameTimer > 0f)
            {
                _skillNameTimer -= Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(_skillNameTimer / 0.6f);
                _skillName.color = new Color(_skillName.color.r, _skillName.color.g,
                                             _skillName.color.b, alpha);
            }

            Sprite icon = SkillIcon(active.Id);
            _skillIcon.enabled = icon != null;
            _skillIcon.sprite = icon;
            _skillGlyph.enabled = icon == null;
            _skillGlyph.text = GlyphFor(active.Id);

            float cooldown = active.Data.cooldown;
            // 남은 비율이 아니라 "회복된 비율"을 그린다. 고리가 차오르면 곧 쓸 수 있다는 뜻이다.
            float ready = cooldown > 0f
                ? 1f - Mathf.Clamp01(active.CooldownRemaining / cooldown)
                : 1f;
            // 원을 끝까지 닫지 않는다. 균열의 원은 언제나 조금 어긋나 있다.
            _cooldownFill.fillAmount = ready * 0.92f;
            _cooldownInner.fillAmount = ready * 0.78f;

            if (_statusEmblem != null && !_dangerShown
                && (_statusZone == null || _statusZone.id != ZoneId.Gaze)
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


            if (_statusEmblem != null && !_dangerShown)
                _statusEmblem.Play(_statusZone != null && _statusZone.id == ZoneId.Gaze
                    ? "Progress" : "StatusProgress");

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

            // 검은 패널을 없앤다. 밝은 수채 배경 위에서 이 사각형만 다른 게임의 조각처럼
            // 떠 있었다. 아주 옅은 어둠만 남겨 아이콘 대비를 확보한다.
            // 배경판을 아예 그리지 않는다. 옅게라도 남기면 밝은 수채 배경 위에서
            // 회색 사각형 하나가 화면 구석에 떠 있는 것으로 보인다.
            var panel = _skillGroup.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0f);
            panel.raycastTarget = false;

            var glyphBg = new GameObject("EmotionGlyph", typeof(RectTransform));
            glyphBg.transform.SetParent(_skillGroup.transform, false);
            // 배경판을 그리지 않는다. 능력이 없거나 아이콘을 못 찾은 순간 화면 구석에
            // 정체불명의 옅은 하늘색 사각형만 남는다 — 실제로 그렇게 보였다.
            // 자리를 잡는 용도로만 두고 색은 비운다.
            var glyphBgImage = glyphBg.AddComponent<Image>();
            glyphBgImage.color = new Color(0f, 0f, 0f, 0f);
            glyphBgImage.raycastTarget = false;
            var glyphRt = glyphBgImage.rectTransform;
            glyphRt.anchorMin = glyphRt.anchorMax = new Vector2(1f, 0.5f);
            glyphRt.pivot = new Vector2(1f, 0.5f);
            glyphRt.sizeDelta = new Vector2(52f, 52f);
            glyphRt.anchoredPosition = new Vector2(-10f, 0f);

            // 지역 아이콘이 있으면 그림이 주인공이고, 없을 때만 글리프 문자로 되돌아간다.
            var iconGo = new GameObject("EmotionIcon", typeof(RectTransform));
            iconGo.transform.SetParent(glyphBg.transform, false);
            _skillIcon = iconGo.AddComponent<Image>();
            _skillIcon.preserveAspect = true;
            _skillIcon.raycastTarget = false;
            var iconRt = _skillIcon.rectTransform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f);
            iconRt.offsetMax = new Vector2(-6f, -6f);
            _skillIcon.enabled = false;

            _skillGlyph = UIBuilder.CreateText(glyphBg.transform, "GlyphText", 32, TextAnchor.MiddleCenter);
            var glyphTextRt = _skillGlyph.rectTransform;
            glyphTextRt.anchorMin = Vector2.zero;
            glyphTextRt.anchorMax = Vector2.one;
            glyphTextRt.offsetMin = Vector2.zero;
            glyphTextRt.offsetMax = Vector2.zero;

            // 쿨타임은 아이콘을 덮는 검은 부채꼴이 아니라 둘레의 끊어진 이중 원으로 보여준다.
            // 덮어 버리면 무슨 능력인지 읽을 수 없고, 균열의 "어긋난 원" 모티브와도 맞지 않는다.
            _cooldownFill = CreateCooldownRing(glyphBg.transform, "CooldownRingOuter", -3f, true);
            _cooldownInner = CreateCooldownRing(glyphBg.transform, "CooldownRingInner", 5f, false);

            _skillName = UIBuilder.CreateText(_skillGroup.transform, "EmotionName", 24, TextAnchor.MiddleRight);
            var nameRt = _skillName.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(12f, 8f);
            nameRt.offsetMax = new Vector2(-72f, -8f);
            _skillGroup.SetActive(false);
        }

        // 둘레를 도는 얇은 고리 하나. 두 개를 서로 반대 방향으로 겹쳐 "어긋난 이중 원"을 만든다.
        // 원 전체를 채우지 않고 항상 틈을 남긴다 — 닫힌 원은 완결을, 균열은 그 반대를 말한다.
        static Image CreateCooldownRing(Transform parent, string name, float inset, bool clockwise)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            // 스프라이트를 주지 않으면 기본 흰 사각형이 방사형으로 채워져 **사각형**이 된다
            // — 화면 구석의 정체불명 하늘색 네모가 이것이었다. 고리 모양을 직접 만들어 쓴다.
            image.sprite = RingSprite();
            image.color = new Color(0.72f, 0.94f, 1f, 0.9f);
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = 2;
            image.fillClockwise = clockwise;
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return image;
        }

        // 가운데가 빈 고리 한 장. 방사형 채우기와 함께 쓰면 원둘레를 도는 호가 된다.
        static Sprite _ringSprite;
        static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // 바깥 0.98, 안쪽 0.80 사이만 남긴다. 양 끝을 부드럽게 깎아 계단을 없앤다.
                    float a = Mathf.Clamp01((0.98f - d) / 0.06f)
                            * Mathf.Clamp01((d - 0.80f) / 0.06f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            texture.Apply();
            _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                                        new Vector2(0.5f, 0.5f), size);
            _ringSprite.name = "CooldownRing";
            return _ringSprite;
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
