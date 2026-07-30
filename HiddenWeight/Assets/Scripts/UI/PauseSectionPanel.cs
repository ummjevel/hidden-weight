using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    public enum PauseSection { Map, Journal, Controls, Settings }

    // 발견한 정보만 보여 주는 스크롤형 보조 화면. 텍스트 목록은 테스트/스크린리더용으로
    // 그대로 유지하고, 실제 화면에서는 지역 문양을 쓴 노드와 기억 카드로 다시 구성한다.
    public class PauseSectionPanel : MonoBehaviour
    {
        GameObject _panel;
        RectTransform _content;
        Text _title;
        Text _body;
        Button _backButton;
        readonly List<GameObject> _dynamicItems = new List<GameObject>();
        readonly List<Button> _actionButtons = new List<Button>();

        public bool IsVisible => _panel != null && _panel.activeSelf;
        public PauseSection CurrentSection { get; private set; }

        void Awake() => Build();
        void OnEnable() => InputPrompts.DeviceChanged += HandleDeviceChanged;
        void OnDisable() => InputPrompts.DeviceChanged -= HandleDeviceChanged;

        void HandleDeviceChanged(InputDeviceKind _)
        {
            if (IsVisible && CurrentSection == PauseSection.Controls) Rebuild(0);
        }

        public void Show(PauseSection section)
        {
            CurrentSection = section;
            _panel.SetActive(true);
            Rebuild();
            UIBuilder.Select(_actionButtons.Count > 0 ? _actionButtons[0] : _backButton);
        }

        public void Hide() => _panel.SetActive(false);

        void Rebuild(int preferredButton = -1)
        {
            foreach (var go in _dynamicItems) Destroy(go);
            _dynamicItems.Clear();
            _actionButtons.Clear();
            _body.enabled = true;

            switch (CurrentSection)
            {
                case PauseSection.Map: BuildMap(); break;
                case PauseSection.Journal: BuildJournal(); break;
                case PauseSection.Controls: BuildControls(); break;
                case PauseSection.Settings: BuildSettings(); break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            if (preferredButton >= 0 && _actionButtons.Count > 0)
                UIBuilder.Select(_actionButtons[Mathf.Clamp(preferredButton, 0, _actionButtons.Count - 1)]);
        }

        void BuildMap()
        {
            _title.text = "지나온 공간";
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress == null || progress.VisitedRooms.Count == 0)
            {
                _body.text = "아직 기억에 남은 방이 없습니다.\n공간을 지나면 이곳에 흔적이 이어집니다.";
                return;
            }

            string currentRoom = HiddenWeight.World.RoomCamera.Instance != null
                && HiddenWeight.World.RoomCamera.Instance.CurrentRoom != null
                ? HiddenWeight.World.RoomCamera.Instance.CurrentRoom.gameObject.name : string.Empty;

            if (GameManager.Instance.Progress.CurrentZone == ZoneId.Residue)
            {
                BuildResidueMap(progress, currentRoom);
                return;
            }

            var rooms = progress.VisitedRooms.OrderBy(RoomSortKey).ToList();
            var accessible = new StringBuilder();
            foreach (string room in rooms) accessible.AppendLine(room);
            _body.text = accessible.ToString();
            _body.enabled = false;

            string lastZone = null;
            for (int i = 0; i < rooms.Count; i++)
            {
                string room = rooms[i];
                string zone = ZonePart(room);
                if (zone != lastZone)
                {
                    CreateSectionLabel(ZoneDisplayName(zone));
                    lastZone = zone;
                }

                bool current = !string.IsNullOrEmpty(currentRoom) && room.EndsWith("/" + currentRoom);
                CreateMapNode(room, current, i > 0 && ZonePart(rooms[i - 1]) == zone);
            }

            CreateSectionLabel("열린 지름길  " + progress.OpenedShortcutCount
                + "    ·    최근 체크포인트  "
                + (progress.LastCheckpoint == Vector3.zero ? "기록 없음" : progress.LastCheckpoint.ToString("F1")));
        }

        void BuildResidueMap(ProgressState progress, string currentRoom)
        {
            _body.text = string.Join("\n", progress.VisitedRooms);
            _body.enabled = false;
            CreateSectionLabel("과거 · 잔재    R01 → R12");

            var visited = new HashSet<string>(progress.VisitedRooms
                .Where(id => id.StartsWith("Residue/"))
                .Select(id => id.Substring(id.IndexOf('/') + 1)));
            string[] main = { "Room01", "Room02", "Room03", "Room04", "Room05", "Room06",
                              "Room07", "Room08", "Room09", "Room10", "Room11", "Room12" };

            bool previousVisible = false;
            foreach (string room in main)
            {
                if (!visited.Contains(room)) continue;
                CreateMapNode("Residue/" + room, room == currentRoom, previousVisible);
                previousVisible = true;

                if (room == "Room04") CreateSecretBranch(visited, "Secret01", "R04 아래 · 납골당", currentRoom);
                if (room == "Room06") CreateSecretBranch(visited, "Secret02", "R06 되감기 길 · 죄인의 심층", currentRoom);
                if (room == "Room11") CreateSecretBranch(visited, "Secret03", "R11 위 · 감춰진 눈", currentRoom);
            }

            CreateSectionLabel("지름길 A  R05↔R03    ·    B  R08↔R03    ·    C  R10↔R07");
            CreateSectionLabel("열린 지름길  " + progress.OpenedShortcutCount
                + "    ·    최근 체크포인트  "
                + (progress.LastCheckpoint == Vector3.zero ? "기록 없음" : progress.LastCheckpoint.ToString("F1")));
        }

        void CreateSecretBranch(HashSet<string> visited, string room, string label, string currentRoom)
        {
            if (!visited.Contains(room)) return;
            CreateSectionLabel("↳  " + label);
            CreateMapNode("Residue/" + room, room == currentRoom, false);
        }

        void BuildJournal()
        {
            _title.text = "이어 붙인 기억";
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress == null || progress.FragmentTexts.Count == 0)
            {
                _body.text = "아직 모은 기억 파편이 없습니다.\n발견한 목소리는 이곳에서 서로의 곁을 찾습니다.";
                return;
            }

            var accessible = new StringBuilder();
            string region = null;
            foreach (var entry in progress.FragmentTexts.OrderBy(e => MemoryCatalog.SortKey(e.Key)))
            {
                string text = string.IsNullOrWhiteSpace(entry.Value) ? "기억의 형체만 남아 있습니다." : entry.Value;
                string nextRegion = MemoryCatalog.RegionFor(entry.Key);
                if (nextRegion != region)
                {
                    CreateSectionLabel(nextRegion);
                    region = nextRegion;
                }
                accessible.Append("◇ ").Append(MemoryCatalog.TitleFor(entry.Key)).AppendLine().AppendLine(text);
                CreateJournalCard(entry.Key, text);
            }
            _body.text = accessible.ToString();
            _body.enabled = false;
        }

        void BuildControls()
        {
            _title.text = "손에 남는 조작";
            _body.text = InputPrompts.ControlsSummary()
                + "\n\n입력 장치가 바뀌면 안내도 함께 바뀝니다. 키보드 항목을 누르면 다음 키로 이동합니다.";
            if (InputPrompts.CurrentDevice == InputDeviceKind.Gamepad) return;
            AddBinding("점프", InputActionId.Jump);
            AddBinding("대시", InputActionId.Dash);
            AddBinding("공격", InputActionId.Attack);
            AddBinding("감정 스킬", InputActionId.Skill);
            AddBinding("자각", InputActionId.Awareness);
            AddBinding("지도", InputActionId.Map);
            AddBinding("일시정지", InputActionId.Pause);
        }

        void AddBinding(string label, InputActionId action)
        {
            int index = _actionButtons.Count;
            AddFlowButton(label + "    " + InputPrompts.Get(action), () =>
            {
                InputPrompts.CycleKeyboardBinding(action);
                Rebuild(index);
            });
        }

        void BuildSettings()
        {
            _title.text = "감각 조율";
            _body.text = "변경 사항은 즉시 적용되고 다음 실행에도 남습니다.";
            AddSetting("전체 음량", () => Cycle(UISettings.MasterVolume, v => UISettings.MasterVolume = v));
            AddSetting("배경음", () => Cycle(UISettings.BgmVolume, v => UISettings.BgmVolume = v));
            AddSetting("효과음", () => Cycle(UISettings.SfxVolume, v => UISettings.SfxVolume = v));
            AddSetting("UI 크기", () =>
            {
                UISettings.UiScale = UISettings.UiScale >= 1.49f ? 0.8f : UISettings.UiScale + 0.1f;
            });
            AddSetting("메시지 시간", () =>
            {
                UISettings.MessageDuration = UISettings.MessageDuration >= 1.99f ? 0.8f : UISettings.MessageDuration + 0.2f;
            });
            AddSetting("동작 줄이기", () => UISettings.ReduceMotion = !UISettings.ReduceMotion);
            AddSetting("섬광 줄이기", () => UISettings.ReduceFlash = !UISettings.ReduceFlash);
            AddSetting("고대비", () => UISettings.HighContrast = !UISettings.HighContrast);
        }

        void Cycle(float value, System.Action<float> setter) => setter(value >= 0.99f ? 0f : value + 0.25f);

        void AddSetting(string label, UnityEngine.Events.UnityAction action)
        {
            int index = _actionButtons.Count;
            AddFlowButton(label + "    " + SettingValue(label), () =>
            {
                action();
                Rebuild(index);
            });
        }

        string SettingValue(string label)
        {
            switch (label)
            {
                case "전체 음량": return Mathf.RoundToInt(UISettings.MasterVolume * 100f) + "%";
                case "배경음": return Mathf.RoundToInt(UISettings.BgmVolume * 100f) + "%";
                case "효과음": return Mathf.RoundToInt(UISettings.SfxVolume * 100f) + "%";
                case "UI 크기": return Mathf.RoundToInt(UISettings.UiScale * 100f) + "%";
                case "메시지 시간": return UISettings.MessageDuration.ToString("0.0") + "x";
                case "동작 줄이기": return UISettings.ReduceMotion ? "켬" : "끔";
                case "섬광 줄이기": return UISettings.ReduceFlash ? "켬" : "끔";
                default: return UISettings.HighContrast ? "켬" : "끔";
            }
        }

        void AddFlowButton(string label, UnityEngine.Events.UnityAction action)
        {
            var button = UIBuilder.CreateButton(_content, label, 0f, action);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 56f);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            _dynamicItems.Add(button.gameObject);
            _actionButtons.Add(button);
        }

        void CreateSectionLabel(string value)
        {
            var text = UIBuilder.CreateText(_content, "MapSection", 20, TextAnchor.MiddleLeft);
            text.text = value;
            text.color = UIBuilder.AccentColor;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            _dynamicItems.Add(text.gameObject);
        }

        void CreateMapNode(string roomId, bool current, bool connected)
        {
            if (connected)
            {
                var link = new GameObject("MapLink", typeof(RectTransform));
                link.transform.SetParent(_content, false);
                link.AddComponent<Image>().color = new Color(UIBuilder.AccentColor.r, UIBuilder.AccentColor.g,
                    UIBuilder.AccentColor.b, 0.38f);
                link.AddComponent<LayoutElement>().preferredHeight = 5f;
                _dynamicItems.Add(link);
            }

            var row = new GameObject("MapNode_" + roomId, typeof(RectTransform));
            row.transform.SetParent(_content, false);
            row.AddComponent<Image>().color = current
                ? new Color(UIBuilder.AccentColor.r, UIBuilder.AccentColor.g, UIBuilder.AccentColor.b, 0.34f)
                : new Color(1f, 1f, 1f, 0.055f);
            row.AddComponent<LayoutElement>().preferredHeight = 72f;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 10);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            var iconGo = new GameObject("StateIcon", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.sprite = MapIcon(current ? 4 : 1);
            icon.color = icon.sprite != null ? Color.white : UIBuilder.AccentColor;
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = 48f;

            var label = UIBuilder.CreateText(row.transform, "RoomLabel", 22, TextAnchor.MiddleLeft);
            label.text = RoomDisplayName(roomId) + (current ? "    현재 머무는 곳" : string.Empty);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _dynamicItems.Add(row);
        }

        void CreateJournalCard(string id, string memory)
        {
            var card = new GameObject("MemoryCard_" + id, typeof(RectTransform));
            card.transform.SetParent(_content, false);
            card.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.065f);
            var element = card.AddComponent<LayoutElement>();
            element.minHeight = 112f;
            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 14, 16);
            layout.spacing = 7f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var heading = UIBuilder.CreateText(card.transform, "MemoryTitle", 19, TextAnchor.MiddleLeft);
            heading.text = "◇  " + MemoryCatalog.TitleFor(id);
            heading.color = UIBuilder.AccentColor;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            var body = UIBuilder.CreateText(card.transform, "MemoryText", 22, TextAnchor.UpperLeft);
            body.text = memory;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _dynamicItems.Add(card);
        }

        Sprite MapIcon(int stateIndex)
        {
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            return zone != null && zone.mapStateIcons != null && stateIndex >= 0
                && stateIndex < zone.mapStateIcons.Length ? zone.mapStateIcons[stateIndex] : null;
        }

        static string ZonePart(string room) => room.Contains("/") ? room.Split('/')[0] : string.Empty;
        static string RoomDisplayName(string room)
        {
            string id = room.Contains("/") ? room.Substring(room.IndexOf('/') + 1) : room;
            switch (id)
            {
                case "Room01": return "R01  입구 경계";
                case "Room02": return "R02  애도교";
                case "Room03": return "R03  손바닥 광장";
                case "Room04": return "R04  매몰된 하층 폐허";
                case "Room05": return "R05  되감기 성소";
                case "Room06": return "R06  손가락 내부";
                case "Room07": return "R07  갈비 곡선교";
                case "Room08": return "R08  상층 승강축";
                case "Room09": return "R09  끊어진 상층 고가교";
                case "Room10": return "R10  손목 감시탑";
                case "Room11": return "R11  후회의 회랑";
                case "Room12": return "R12  기억의 교수대";
                case "Secret01": return "S1  납골당";
                case "Secret02": return "S2  죄인의 심층";
                case "Secret03": return "S3  감춰진 눈";
                default: return id;
            }
        }
        static string RoomSortKey(string room) => ZonePart(room) + "/" + RoomDisplayName(room).PadLeft(16, '0');
        static string ZoneDisplayName(string zone)
        {
            if (System.Enum.TryParse(zone, out ZoneId parsed))
            {
                switch (parsed)
                {
                    case ZoneId.Residue: return "과거 · 잔재";
                    case ZoneId.Gaze: return "현재 · 응시";
                    case ZoneId.Fracture: return "미래 · 균열";
                    default: return "기억의 입구";
                }
            }
            return "흩어진 공간";
        }

        void Build()
        {
            _panel = new GameObject("PauseSection", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(0.09f, 0.07f);
            rt.anchorMax = new Vector2(0.91f, 0.93f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = UIBuilder.PanelBackground;

            _title = UIBuilder.CreateText(_panel.transform, "SectionTitle", 36, TextAnchor.UpperLeft);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.86f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(54f, 0f);
            titleRt.offsetMax = new Vector2(-54f, -24f);

            var viewport = new GameObject("SectionViewport", typeof(RectTransform));
            viewport.transform.SetParent(_panel.transform, false);
            var viewportRt = (RectTransform)viewport.transform;
            viewportRt.anchorMin = new Vector2(0.05f, 0.13f);
            viewportRt.anchorMax = new Vector2(0.95f, 0.84f);
            viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.10f);
            viewport.AddComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("SectionContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            _content = (RectTransform)contentGo.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = Vector2.zero;
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(30, 42, 24, 28);
            contentLayout.spacing = 14f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;

            _body = UIBuilder.CreateText(_content, "SectionBody", 22, TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _body.lineSpacing = 1.15f;
            _body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _backButton = UIBuilder.CreateButton(_panel.transform, "뒤로", -365f, Hide);
            var backRt = (RectTransform)_backButton.transform;
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0.5f);
            backRt.sizeDelta = new Vector2(220f, 54f);
            _panel.SetActive(false);
        }
    }
}
