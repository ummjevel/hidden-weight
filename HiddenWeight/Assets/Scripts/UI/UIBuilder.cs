using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.UI
{
    // HUD/PauseMenu/TitleScreen에 각각 중복돼 있던 uGUI 조립 헬퍼를 한 곳으로 모은 것.
    // 동작은 기존 세 파일의 구현과 완전히 동일하게 유지한다(리팩터링, 기능 변경 아님).
    public static class UIBuilder
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        static Font _menuRegular;
        static Font _menuBold;
        static Sprite _menuGradient;

        public static Font MenuRegular => _menuRegular ??= Resources.Load<Font>("Fonts/NanumMyeongjo-Regular")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font MenuBold => _menuBold ??= Resources.Load<Font>("Fonts/NanumMyeongjo-Bold")
            ?? MenuRegular;
        // 월드 안내도 이미 빌드에 포함되는 메뉴 폰트 인스턴스를 공유한다. 별도 폰트나
        // 정적 아틀라스를 추가하지 않아 WebGL 빌드 용량을 늘리지 않는다.
        public static Font WorldHintFont => MenuBold;
        // 화면마다 따로 하드코딩돼 있던 색을 한 곳으로 모은 것 — 값 자체는 기존 화면들이
        // 이미 쓰던 것 중 하나로 통일했을 뿐, 새로운 톤을 도입한 게 아니다. 실제 아트(커스텀
        // 폰트·아이콘)는 별도 작업으로 미루고, 지금은 색·형태의 일관성만 맞춘다.
        static ZoneId CurrentZone => GameManager.Instance != null && GameManager.Instance.CurrentZoneData != null
            ? GameManager.Instance.CurrentZoneData.id : ZoneId.Prologue;

        public static Color PanelBackground => CurrentZone switch
        {
            ZoneId.Residue => new Color(0.075f, 0.062f, 0.045f, 0.94f),
            ZoneId.Gaze => new Color(0.035f, 0.065f, 0.075f, 0.94f),
            ZoneId.Fracture => new Color(0.085f, 0.055f, 0.072f, 0.94f),
            _ => new Color(0.045f, 0.042f, 0.055f, 0.92f),
        };
        public static Color ButtonIdle => CurrentZone switch
        {
            ZoneId.Residue => new Color(0.48f, 0.37f, 0.22f, 0.34f),
            ZoneId.Gaze => new Color(0.23f, 0.42f, 0.44f, 0.32f),
            ZoneId.Fracture => new Color(0.48f, 0.30f, 0.39f, 0.30f),
            _ => new Color(0.42f, 0.38f, 0.52f, 0.28f),
        };
        public static Color TextPrimary => UISettings.HighContrast
            ? new Color(1f, 0.98f, 0.90f, 1f)
            : new Color(0.92f, 0.89f, 0.82f, 1f);
        public static Color AccentColor => CurrentZone switch
        {
            ZoneId.Residue => new Color(0.83f, 0.61f, 0.27f, 1f),
            ZoneId.Gaze => new Color(0.45f, 0.72f, 0.68f, 1f),
            ZoneId.Fracture => new Color(0.88f, 0.56f, 0.66f, 1f),
            _ => new Color(0.68f, 0.62f, 0.80f, 1f),
        };
        // 체력 결정핵의 색. 지역을 따라간다.
        //
        // 예전에는 어느 지역에서나 새빨간 (0.78, 0.24, 0.22) 하나였다. 균열은 연보라·청록의
        // 밝은 수채 배경인데 그 위에 선명한 핏빛 도트 하트 다섯 개가 얹혀, 화면에서 유일하게
        // 다른 게임에서 온 것처럼 보였다(브랜드 성격 "몽환적·회복적"과도 어긋난다).
        // 잔재의 앰버, 응시의 청록, 균열의 결정빛으로 나눈다.
        public static Color HeartFull => CurrentZone switch
        {
            ZoneId.Residue => new Color(0.80f, 0.42f, 0.26f, 1f),
            ZoneId.Gaze => new Color(0.42f, 0.74f, 0.72f, 1f),
            ZoneId.Fracture => new Color(0.72f, 0.72f, 0.94f, 1f),
            _ => new Color(0.78f, 0.24f, 0.22f, 1f),
        };
        // 지금은 HUD가 하트를 enabled on/off로만 표시한다(빈 하트를 별도로 그리지 않음).
        // 최대 체력 확장 때 빈 하트 아웃라인을 그리게 되면 이 색을 쓴다.
        public static readonly Color HeartEmpty = new Color(1f, 1f, 1f, 0.25f);

        // 런타임 생성 Canvas가 화면마다 서로 다른 스케일 규칙을 쓰지 않도록 한곳에서 맞춘다.
        // 16:9를 기준으로 하되 가로·세로 차이를 절반씩 반영해 720p~1440p와 울트라와이드에서
        // 같은 물리 크기로 보이게 한다(UI_UX_DESIGN 4.4절).
        public static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution / UISettings.UiScale;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            if (scaler.GetComponent<UIScaleWatcher>() == null) scaler.gameObject.AddComponent<UIScaleWatcher>();
        }
        // 버튼 호버/눌림 피드백용 배율 — Selectable.colors(ColorBlock)가 ButtonIdle에 곱한다.
        public static readonly Color HoverTint = new Color(1.08f, 1.06f, 1.02f, 1f);
        public static readonly Color PressedTint = new Color(0.82f, 0.80f, 0.76f, 1f);

        public static readonly Color MenuInk = new Color(0.025f, 0.027f, 0.045f, 0.97f);
        public static readonly Color MenuGlass = new Color(0.075f, 0.065f, 0.105f, 0.94f);
        public static readonly Color MenuEdge = new Color(0.62f, 0.57f, 0.82f, 0.62f);
        public static readonly Color MenuEdgeSoft = new Color(0.52f, 0.62f, 0.80f, 0.22f);
        public static readonly Color MenuTextMuted = new Color(0.74f, 0.72f, 0.80f, 0.82f);

        // 비인게임 화면 전용 서체. HUD는 기존 CreateText를 그대로 사용하므로 영향을 받지 않는다.
        public static Text CreateMenuText(Transform parent, string name, string content, int fontSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var text = CreateText(parent, name, fontSize, alignment);
            text.text = content;
            StyleMenuText(text, bold);
            return text;
        }

        public static void StyleMenuText(Text text, bool bold = false)
        {
            if (text == null) return;
            text.font = bold ? MenuBold : MenuRegular;
            text.color = TextPrimary;
        }

        // 반투명 유리판과 얇은 이중 테두리. 9-slice 그림 대신 앵커로 조립해 어떤 비율에서도
        // 모서리와 선 두께가 유지된다.
        public static Image CreateMenuPanel(Transform parent, string name, Color color, bool ornament = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = MenuEdge;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            if (ornament)
            {
                AddEdge(go.transform, "FrameTop", new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(18f, -2f), new Vector2(-18f, 0f), MenuEdgeSoft);
                AddEdge(go.transform, "FrameBottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(18f, 0f), new Vector2(-18f, 2f), MenuEdgeSoft);
                AddDiamond(go.transform, "CornerTL", new Vector2(0f, 1f), new Vector2(12f, -12f), 12f);
                AddDiamond(go.transform, "CornerTR", new Vector2(1f, 1f), new Vector2(-12f, -12f), 12f);
                AddDiamond(go.transform, "CornerBL", new Vector2(0f, 0f), new Vector2(12f, 12f), 12f);
                AddDiamond(go.transform, "CornerBR", new Vector2(1f, 0f), new Vector2(-12f, 12f), 12f);
            }
            return image;
        }

        public static void AddDivider(Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var line = new GameObject("MemoryDivider", typeof(RectTransform));
            line.transform.SetParent(parent, false);
            var rt = (RectTransform)line.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            line.AddComponent<Image>().color = MenuEdgeSoft;
            AddDiamond(line.transform, "DividerGem", new Vector2(0.5f, 0.5f), Vector2.zero, 10f);
        }

        public static Sprite MenuGradient
        {
            get
            {
                if (_menuGradient != null) return _menuGradient;
                const int height = 256;
                var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
                {
                    name = "RuntimeMenuGradient",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                var bottom = new Color(0.018f, 0.020f, 0.040f, 1f);
                var middle = new Color(0.075f, 0.060f, 0.105f, 1f);
                var top = new Color(0.025f, 0.055f, 0.085f, 1f);
                for (int y = 0; y < height; y++)
                {
                    float t = y / (height - 1f);
                    Color color = t < 0.55f
                        ? Color.Lerp(bottom, middle, t / 0.55f)
                        : Color.Lerp(middle, top, (t - 0.55f) / 0.45f);
                    texture.SetPixel(0, y, color);
                    texture.SetPixel(1, y, color);
                }
                texture.Apply(false, true);
                _menuGradient = Sprite.Create(texture, new Rect(0f, 0f, 2f, height), new Vector2(0.5f, 0.5f), 100f);
                _menuGradient.name = "RuntimeMenuGradientSprite";
                _menuGradient.hideFlags = HideFlags.HideAndDontSave;
                return _menuGradient;
            }
        }

        static void AddEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            go.AddComponent<Image>().color = color;
        }

        static void AddDiamond(Transform parent, string name, Vector2 anchor, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = Vector2.one * size;
            rt.anchoredPosition = position;
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            go.AddComponent<Image>().color = MenuEdge;
        }

        // HUD.cs가 쓰던 버전: GameObject 이름을 그대로 받고, 정렬을 지정하며, 텍스트는 빈 채로 시작한다.
        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextPrimary;
            text.text = string.Empty;
            return text;
        }

        // PauseMenu.cs/TitleScreen.cs가 쓰던 버전: GameObject 이름이 "Text_"+content, 정렬은
        // 항상 가운데, 텍스트는 content로 바로 채워진다.
        public static Text CreateText(Transform parent, string content, int fontSize)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextPrimary;
            text.text = content;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, float yPos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 56f);
            rt.anchoredPosition = new Vector2(0f, yPos);

            var img = go.AddComponent<Image>();
            img.color = ButtonIdle;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = MenuEdgeSoft;
            outline.effectDistance = new Vector2(1f, -1f);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => AudioManager.Instance?.PlaySfx(SfxCue.UiConfirm, 0.35f));
            button.onClick.AddListener(onClick);
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            go.AddComponent<UIFocusPulse>();

            var colors = button.colors;
            colors.normalColor = ButtonIdle;
            colors.highlightedColor = new Color(0.92f, 0.86f, 0.72f, 0.46f);
            colors.pressedColor = new Color(0.72f, 0.62f, 0.48f, 0.62f);
            colors.selectedColor = UISettings.HighContrast
                ? new Color(0.94f, 0.72f, 0.26f, 0.95f)
                : AccentColor * new Color(1f, 1f, 1f, 0.58f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.06f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var text = CreateText(go.transform, label, 24);
            StyleMenuText(text, true);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var accent = new GameObject("SelectionGem", typeof(RectTransform));
            accent.transform.SetParent(go.transform, false);
            var accentRt = (RectTransform)accent.transform;
            accentRt.anchorMin = accentRt.anchorMax = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(10f, 10f);
            accentRt.anchoredPosition = new Vector2(16f, 0f);
            accentRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            accent.AddComponent<Image>().color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.72f);

            return button;
        }

        // 메뉴를 열 때 키보드·게임패드 포커스가 월드 뒤에 남지 않게 명시적으로 옮긴다.
        public static void Select(Selectable selectable)
        {
            if (EventSystem.current == null || selectable == null || !selectable.IsActive()) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
