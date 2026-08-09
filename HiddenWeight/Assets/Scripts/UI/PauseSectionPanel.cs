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
        ScrollRect _scroll;
        GameObject _lastScrolledTo;
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

        // 스크롤은 마우스 휠로만 움직였다. 키보드·게임패드로 아래 항목에 포커스를 옮기면
        // 화면 밖으로 나간 항목이 그대로 보이지 않아, UI 크기를 키운 플레이어는 설정
        // 화면에서 아래쪽 항목을 아예 조작할 수 없었다 — 크기를 되돌릴 수도 없었다.
        void Update()
        {
            if (!IsVisible || _scroll == null || _content == null || EventSystem.current == null) return;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || selected == _lastScrolledTo) return;
            if (!selected.transform.IsChildOf(_content)) return;

            _lastScrolledTo = selected;
            ScrollIntoView((RectTransform)selected.transform);
        }

        static readonly Vector3[] _itemCorners = new Vector3[4];
        static readonly Vector3[] _viewCorners = new Vector3[4];

        void ScrollIntoView(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            target.GetWorldCorners(_itemCorners);
            _scroll.viewport.GetWorldCorners(_viewCorners);

            float scale = _content.lossyScale.y;
            if (scale <= 0f) return;

            // 한 줄이 딱 맞게 걸치면 다음 항목이 있다는 걸 알 수 없다. 한 칸 여유를 둔다.
            float margin = 24f * scale;
            float below = (_viewCorners[0].y + margin) - _itemCorners[0].y;
            float above = _itemCorners[1].y - (_viewCorners[1].y - margin);

            float delta = below > 0f ? below : above > 0f ? -above : 0f;
            if (Mathf.Abs(delta) < 0.001f) return;

            _content.anchoredPosition += new Vector2(0f, delta / scale);
        }

        // 섹션 패널이 열리고 닫힐 때 알린다. 일시정지 메뉴의 탭·버튼은 이 패널과 같은
        // 부모에 있고 패널이 화면 대부분을 덮으므로, 그대로 두면 지도 위에 글자가 겹쳐 찍힌다.
        public event System.Action<bool> VisibilityChanged;

        public void Show(PauseSection section)
        {
            CurrentSection = section;
            _panel.SetActive(true);
            _lastScrolledTo = null;
            if (_content != null) _content.anchoredPosition = Vector2.zero;
            Rebuild();
            UIBuilder.Select(_actionButtons.Count > 0 ? _actionButtons[0] : _backButton);
            VisibilityChanged?.Invoke(true);
        }

        public void Hide()
        {
            // 설정을 바꾸며 Rebuild한 오브젝트는 Destroy가 프레임 끝에 처리된다. 같은 프레임에
            // 홈으로 돌아오면 이전 버튼이 한 장 더 남은 것처럼 보이므로 먼저 즉시 숨긴다.
            foreach (var go in _dynamicItems)
                if (go != null) go.SetActive(false);
            _panel.SetActive(false);
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_panel.transform))
                EventSystem.current.SetSelectedGameObject(null);

            // 패널이 완전히 내려간 뒤에 알린다. 일시정지 메뉴의 탭·버튼이 이 신호로 돌아오므로
            // 빠뜨리면 섹션을 닫아도 크롬이 영영 감춰진 채로 남는다.
            VisibilityChanged?.Invoke(false);
        }

        void Rebuild(int preferredButton = -1)
        {
            foreach (var go in _dynamicItems)
            {
                if (go == null) continue;
                go.SetActive(false); // Destroy 지연 중 한 프레임 겹쳐 보이는 잔상을 막는다.
                Destroy(go);
            }
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
            _title.text = "전체 지역 지도";
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress == null)
            {
                _body.text = "아직 기억에 남은 방이 없습니다.\n공간을 지나면 이곳에 흔적이 이어집니다.";
                return;
            }

            string currentRoom = HiddenWeight.World.RoomCamera.Instance != null
                && HiddenWeight.World.RoomCamera.Instance.CurrentRoom != null
                ? HiddenWeight.World.RoomCamera.Instance.CurrentRoom.gameObject.name : string.Empty;

            ZoneId currentZone = GameManager.Instance.Progress.CurrentZone;
            if (currentZone == ZoneId.Residue || currentZone == ZoneId.Gaze || currentZone == ZoneId.Fracture)
            {
                BuildFullZoneMap(progress, currentRoom, currentZone);
                return;
            }

            if (progress.VisitedRooms.Count == 0)
            {
                _body.text = "아직 기억에 남은 방이 없습니다.\n공간을 지나면 이곳에 흔적이 이어집니다.";
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
                string zonePart = ZonePart(room);
                if (zonePart != lastZone)
                {
                    CreateSectionLabel(ZoneDisplayName(zonePart));
                    lastZone = zonePart;
                }

                bool current = !string.IsNullOrEmpty(currentRoom) && room.EndsWith("/" + currentRoom);
                CreateMapNode(room, current, i > 0 && ZonePart(rooms[i - 1]) == zonePart);
            }

            // 체크포인트의 월드 좌표는 적지 않는다. "(3.0, 4.0, 0.0)"은 개발자에게만
            // 뜻이 있고, 플레이어에게는 화면에 새어 나온 내부 값으로 보인다.
            CreateSectionLabel("열린 지름길  " + progress.OpenedShortcutCount);
        }

        // 지역 지도. 세로 목록이 아니라 노드 그래프로 그린다.
        //
        // 예전 구현은 방을 위에서 아래로 늘어놓아 "긴 목록"이었고, 어느 방이 어느 방과
        // 이어지는지, 비밀방이 어디서 갈라지는지, 지금 어디에 있는지가 글 속에 묻혔다.
        // 주 동선은 가로축, 비밀방은 그 아래 가지, 숏컷은 별도 줄로 분리한다.
        const int MainRooms = 12;
        static readonly int[] SecretParents = { 4, 6, 11 };   // 비밀방이 갈라져 나오는 방 번호

        void BuildFullZoneMap(ProgressState progress, string currentRoom, ZoneId zone)
        {
            _body.text = string.Join("\n", progress.VisitedRooms);
            _body.enabled = false;

            string letter = ZoneLetter(zone);
            CreateSectionLabel($"{ZoneDisplayName(zone.ToString())}    {letter}01 → {letter}{MainRooms}");

            var visited = new HashSet<string>(progress.VisitedRooms
                .Where(id => id.StartsWith(zone + "/")));
            string prefix = zone == ZoneId.Residue ? string.Empty : zone.ToString();

            var mainRow = CreateMapRow("MainRoute", 96f);
            for (int i = 1; i <= MainRooms; i++)
            {
                if (i > 1) CreateConnector(mainRow, LinkWidth, 4f);
                string roomId = zone + "/" + prefix + "Room" + i.ToString("00");
                CreateMapChip(mainRow, roomId, roomId.EndsWith("/" + currentRoom),
                              visited.Contains(roomId));
            }

            // 비밀방 줄. 주 동선과 같은 칸 폭을 유지해 부모 방 바로 아래에 오게 한다.
            var branchRow = CreateMapRow("SecretBranches", 84f);
            for (int i = 1; i <= MainRooms; i++)
            {
                if (i > 1) CreateSpacer(branchRow, LinkWidth);

                int index = System.Array.IndexOf(SecretParents, i);
                if (index < 0)
                {
                    CreateSpacer(branchRow, ChipWidth);
                    continue;
                }

                string roomId = zone + "/" + prefix + "Secret" + (index + 1).ToString("00");
                CreateMapChip(branchRow, roomId, roomId.EndsWith("/" + currentRoom),
                              visited.Contains(roomId), branch: true);
            }

            CreateSectionLabel(ShortcutLine(progress, zone, letter));
        }

        // 숏컷은 세 지역이 같은 구조다(설계 8.2): A 05↔03, B 08↔03, C 10↔07.
        //
        // 예전에는 이 세 줄을 **열지 않았어도 전부** 적었다. 아직 가보지 않은 방 사이의
        // 연결을 지도가 미리 알려 주면 탐험이 남지 않는다. 연 것만 적고, 나머지는
        // 몇 개가 남았는지만 알린다.
        static readonly (int from, int to, string suffix)[] ShortcutRoutes =
            { (5, 3, "a"), (8, 3, "b"), (10, 7, "c") };

        static string ShortcutLine(ProgressState progress, ZoneId zone, string letter)
        {
            string zoneKey = zone.ToString().ToLowerInvariant();
            var opened = new List<string>();
            int closed = 0;
            foreach (var route in ShortcutRoutes)
            {
                if (progress.IsShortcutOpen($"{zoneKey}_shortcut_{route.suffix}"))
                    opened.Add($"{letter}{route.from:00} ↔ {letter}{route.to:00}");
                else closed++;
            }

            if (opened.Count == 0)
                return "지름길   아직 연 길이 없습니다";
            return "지름길   " + string.Join("    ·    ", opened)
                 + (closed > 0 ? $"    ·    아직 닫힌 길 {closed}" : string.Empty);
        }

        // 12칸 + 연결선 11개가 패널 안에 들어와야 한다. 넓게 잡으면 뒤쪽 방(F08~F12)이
        // 오른쪽으로 잘려 나가 지도가 절반만 보인다.
        const float ChipWidth = 74f;
        const float LinkWidth = 10f;

        RectTransform CreateMapRow(string name, float height)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(_content, false);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // 폭을 레이아웃이 통제해야 LayoutElement.preferredWidth가 먹는다. 끄면 자식이
            // 각자 기본 크기(100)로 남아 12칸이 패널을 넘어가고 뒤쪽 방이 잘려 나간다.
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            // 이걸 켜 두면 연결선이 행 높이만큼 늘어나 얇은 선이 아니라 큰 덩어리가 된다.
            layout.childForceExpandHeight = false;
            _dynamicItems.Add(row);
            return (RectTransform)row.transform;
        }

        void CreateConnector(RectTransform row, float width, float height)
        {
            var link = new GameObject("Link", typeof(RectTransform));
            link.transform.SetParent(row, false);
            link.AddComponent<Image>().color = new Color(
                UIBuilder.AccentColor.r, UIBuilder.AccentColor.g, UIBuilder.AccentColor.b, 0.5f);
            var element = link.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        void CreateSpacer(RectTransform row, float width)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(row, false);
            spacer.AddComponent<LayoutElement>().preferredWidth = width;
        }

        // 방 하나. 기호(F03)를 크게, 이름을 작게 둔다 — 좁은 칸에서는 기호가 먼저 읽힌다.
        void CreateMapChip(RectTransform row, string roomId, bool current, bool discovered,
                           bool branch = false)
        {
            // 이름은 MapNode_ 를 유지한다 — 이 이름으로 방 표시 개수를 세는 검사가 있다.
            var chip = new GameObject("MapNode_" + roomId, typeof(RectTransform));
            chip.transform.SetParent(row, false);

            chip.AddComponent<Image>().color = current
                ? new Color(UIBuilder.AccentColor.r, UIBuilder.AccentColor.g, UIBuilder.AccentColor.b, 0.85f)
                : discovered ? new Color(1f, 1f, 1f, 0.085f) : new Color(0.3f, 0.3f, 0.3f, 0.05f);

            var element = chip.AddComponent<LayoutElement>();
            element.preferredWidth = ChipWidth;
            element.preferredHeight = branch ? 72f : 84f;
            element.flexibleHeight = 0f;

            var layout = chip.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 8, 8);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var code = UIBuilder.CreateText(chip.transform, "Code", current ? 24 : 20,
                                            TextAnchor.MiddleCenter);
            code.text = (branch ? "↳ " : string.Empty) + RoomCode(roomId);
            if (current) code.color = Color.white;
            else if (!discovered) code.color = new Color(code.color.r, code.color.g, code.color.b, 0.4f);

            var label = UIBuilder.CreateText(chip.transform, "Name", 11, TextAnchor.UpperCenter);
            label.text = discovered ? RoomName(roomId) : "미탐사";
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.color = new Color(label.color.r, label.color.g, label.color.b,
                                    current ? 0.95f : discovered ? 0.6f : 0.3f);

            _dynamicItems.Add(chip);
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
            _body.text = InputPrompts.ControlsSummary();
            if (InputPrompts.CurrentDevice == InputDeviceKind.Gamepad) return;
            AddBinding("점프", InputActionId.Jump);
            AddBinding("대시", InputActionId.Dash);
            AddBinding("공격", InputActionId.Attack);
            AddBinding("감정 스킬", InputActionId.Skill);
            AddBinding("지도", InputActionId.Map);
            AddBinding("일시정지", InputActionId.Pause);
        }

        void AddBinding(string label, InputActionId action)
        {
            int index = _actionButtons.Count;
            AddFlowButton(label, InputPrompts.Get(action) + InputPrompts.AlternateSuffix(action), () =>
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
            AddFlowButton(label, SettingValue(label), () =>
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

        // 이름은 왼쪽, 값은 오른쪽. 예전에는 "전체 음량    100%"를 한 덩어리로 만들어
        // 가운데 정렬했다 — 칸은 화면 폭만큼 넓은데 글자만 한가운데 떠 있어, 어느 항목이
        // 어떤 값인지 훑어 내려가며 읽을 수가 없었다.
        void AddFlowButton(string label, string value, UnityEngine.Events.UnityAction action)
        {
            var button = UIBuilder.CreateButton(_content, label, 0f, action);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 56f);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            var labelText = button.GetComponentInChildren<Text>();
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.rectTransform.offsetMin = new Vector2(28f, 0f);
            labelText.rectTransform.offsetMax = new Vector2(-28f, 0f);

            if (!string.IsNullOrEmpty(value))
            {
                var valueText = UIBuilder.CreateText(button.transform, "Value", 24, TextAnchor.MiddleRight);
                UIBuilder.StyleMenuText(valueText, true);
                valueText.text = value;
                valueText.color = UIBuilder.AccentColor;
                valueText.raycastTarget = false;
                var valueRt = valueText.rectTransform;
                valueRt.anchorMin = Vector2.zero;
                valueRt.anchorMax = Vector2.one;
                valueRt.offsetMin = new Vector2(28f, 0f);
                valueRt.offsetMax = new Vector2(-28f, 0f);
            }

            _dynamicItems.Add(button.gameObject);
            _actionButtons.Add(button);
        }

        void CreateSectionLabel(string value)
        {
            var text = UIBuilder.CreateText(_content, "MapSection", 20, TextAnchor.MiddleLeft);
            UIBuilder.StyleMenuText(text, true);
            text.text = value;
            text.color = UIBuilder.AccentColor;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            _dynamicItems.Add(text.gameObject);
        }

        void CreateMapNode(string roomId, bool current, bool connected, bool discovered = true)
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
                : discovered ? new Color(1f, 1f, 1f, 0.055f) : new Color(0.3f, 0.3f, 0.3f, 0.04f);
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
            icon.sprite = MapIcon(current ? 4 : discovered ? 1 : 0);
            icon.color = icon.sprite != null ? (discovered ? Color.white : new Color(1f, 1f, 1f, 0.28f))
                : (discovered ? UIBuilder.AccentColor : new Color(1f, 1f, 1f, 0.2f));
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = 48f;

            var label = UIBuilder.CreateText(row.transform, "RoomLabel", 22, TextAnchor.MiddleLeft);
            UIBuilder.StyleMenuText(label, current);
            label.text = RoomDisplayName(roomId) + (current ? "    현재 머무는 곳" : string.Empty);
            if (!discovered) label.color = new Color(label.color.r, label.color.g, label.color.b, 0.42f);
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
            UIBuilder.StyleMenuText(heading, true);
            heading.text = "◇  " + MemoryCatalog.TitleFor(id);
            heading.color = UIBuilder.AccentColor;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            var body = UIBuilder.CreateText(card.transform, "MemoryText", 22, TextAnchor.UpperLeft);
            UIBuilder.StyleMenuText(body);
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

        // 방 이름은 지역마다 다르다. 예전에는 잔재 이름표만 있고 응시·균열은
        // "R03 균열 구역" 같은 임시 문자열로 떨어져, 지도가 어느 지역인지도, 그 방이
        // 무엇인지도 알려 주지 못했다. 지역 문서(LEVEL_20/30/40)의 이름을 그대로 쓴다.
        static readonly string[] ResidueNames =
        {
            "입구 경계", "애도교", "손바닥 광장", "매몰된 하층 폐허", "되감기 성소",
            "손가락 내부", "갈비 곡선교", "상층 승강축", "끊어진 상층 고가교",
            "손목 감시탑", "후회의 회랑", "기억의 교수대",
        };
        static readonly string[] ResidueSecrets = { "납골당", "죄인의 심층", "감춰진 눈" };

        static readonly string[] GazeNames =
        {
            "눈꺼풀 경계", "고정된 시선교", "관객 광장", "하층 새장원", "숨죽임 성소",
            "속삭임 통로", "회전 홍채교", "시선 승강정", "상층 관객석",
            "홍채 감시탑", "자기 초상의 회랑", "만인의 극장",
        };
        static readonly string[] GazeSecrets = { "무대 뒤편", "무언의 우리", "안쪽 눈" };

        static readonly string[] FractureNames =
        {
            "유리 정원", "어긋난 산책로", "가능성 광장", "흔들리는 하층정원", "예지 성소",
            "시차 온실", "부유 건축군", "역행 승강축", "거울 가능성실",
            "초침 감시탑", "아직 오지 않은 폐허", "내일의 균열",
        };
        static readonly string[] FractureSecrets = { "버려진 가능성", "멈춘 오후", "선택되지 않은 문" };

        // 지역을 한 글자로. 지도 노드는 좁아서 이름보다 이 기호가 먼저 읽힌다.
        static string ZoneLetter(ZoneId zone) => zone switch
        {
            ZoneId.Gaze => "G",
            ZoneId.Fracture => "F",
            _ => "R",
        };

        static void SplitRoomId(string room, out ZoneId zone, out bool secret, out int number)
        {
            string id = room.Contains("/") ? room.Substring(room.IndexOf('/') + 1) : room;
            zone = id.StartsWith("Gaze") ? ZoneId.Gaze
                 : id.StartsWith("Fracture") ? ZoneId.Fracture
                 : ZoneId.Residue;

            string tail = id.StartsWith("Gaze") ? id.Substring(4)
                        : id.StartsWith("Fracture") ? id.Substring(8)
                        : id;
            secret = tail.StartsWith("Secret");
            string digits = tail.StartsWith("Secret") ? tail.Substring(6)
                          : tail.StartsWith("Room") ? tail.Substring(4)
                          : string.Empty;
            number = int.TryParse(digits, out int parsed) ? parsed : 0;
        }

        static string RoomCode(string room)
        {
            SplitRoomId(room, out var zone, out bool secret, out int number);
            if (number <= 0) return room;
            return ZoneLetter(zone) + (secret ? "S" + number : number.ToString("00"));
        }

        static string RoomName(string room)
        {
            SplitRoomId(room, out var zone, out bool secret, out int number);
            string[] table = zone switch
            {
                ZoneId.Gaze => secret ? GazeSecrets : GazeNames,
                ZoneId.Fracture => secret ? FractureSecrets : FractureNames,
                _ => secret ? ResidueSecrets : ResidueNames,
            };
            return number >= 1 && number <= table.Length ? table[number - 1] : string.Empty;
        }

        static string RoomDisplayName(string room)
        {
            string name = RoomName(room);
            return string.IsNullOrEmpty(name) ? RoomCode(room) : RoomCode(room) + "  " + name;
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
            var panelImage = UIBuilder.CreateMenuPanel(transform, "PauseSection", UIBuilder.MenuGlass);
            _panel = panelImage.gameObject;
            var rt = panelImage.rectTransform;
            rt.anchorMin = new Vector2(0.075f, 0.055f);
            rt.anchorMax = new Vector2(0.925f, 0.945f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var caption = UIBuilder.CreateMenuText(_panel.transform, "SectionCaption", "MEMORY ARCHIVE", 14,
                TextAnchor.UpperRight, true);
            caption.color = new Color(UIBuilder.MenuEdge.r, UIBuilder.MenuEdge.g, UIBuilder.MenuEdge.b, 0.76f);
            var captionRt = caption.rectTransform;
            captionRt.anchorMin = new Vector2(0.55f, 0.90f);
            captionRt.anchorMax = new Vector2(1f, 1f);
            captionRt.offsetMin = new Vector2(0f, 0f);
            captionRt.offsetMax = new Vector2(-54f, -35f);

            _title = UIBuilder.CreateText(_panel.transform, "SectionTitle", 36, TextAnchor.UpperLeft);
            UIBuilder.StyleMenuText(_title, true);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.86f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(54f, 0f);
            titleRt.offsetMax = new Vector2(-54f, -24f);

            UIBuilder.AddDivider(_panel.transform, new Vector2(0.5f, 0.865f), new Vector2(0f, 2f), Vector2.zero);
            var divider = _panel.transform.Find("MemoryDivider") as RectTransform;
            if (divider != null)
            {
                divider.anchorMin = new Vector2(0.04f, 0.865f);
                divider.anchorMax = new Vector2(0.96f, 0.865f);
                divider.offsetMin = divider.offsetMax = Vector2.zero;
            }

            var viewport = new GameObject("SectionViewport", typeof(RectTransform));
            viewport.transform.SetParent(_panel.transform, false);
            var viewportRt = (RectTransform)viewport.transform;
            // 아래를 0.13에서 0.19로 올린다. 0.13이면 스크롤 영역의 마지막 줄이 "뒤로"
            // 버튼과 겹쳐, 마지막 항목이 버튼에 반쯤 잘린 채로 보였다.
            viewportRt.anchorMin = new Vector2(0.05f, 0.19f);
            viewportRt.anchorMax = new Vector2(0.95f, 0.84f);
            viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.015f, 0.016f, 0.030f, 0.38f);
            var viewportOutline = viewport.AddComponent<Outline>();
            viewportOutline.effectColor = UIBuilder.MenuEdgeSoft;
            viewportOutline.effectDistance = new Vector2(1f, -1f);
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

            _scroll = viewport.AddComponent<ScrollRect>();
            _scroll.viewport = viewportRt;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 38f;

            _body = UIBuilder.CreateText(_content, "SectionBody", 22, TextAnchor.UpperLeft);
            UIBuilder.StyleMenuText(_body);
            _body.color = UIBuilder.MenuTextMuted;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _body.lineSpacing = 1.15f;
            _body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _backButton = UIBuilder.CreateButton(_panel.transform, "뒤로", 0f, Hide);
            var backRt = (RectTransform)_backButton.transform;
            // 패널 아래쪽에 붙인다. 가운데 기준 고정 오프셋(-365)은 화면 비율이 바뀌어
            // 패널 높이가 달라지면 스크롤 영역 위로 파고들었다.
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.sizeDelta = new Vector2(220f, 54f);
            backRt.anchoredPosition = new Vector2(0f, 34f);
            _panel.SetActive(false);
        }
    }
}
