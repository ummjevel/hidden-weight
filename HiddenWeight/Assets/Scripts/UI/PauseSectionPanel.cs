using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    public enum PauseSection { Map, Journal, Controls, Settings }

    // 일시정지의 보조 화면. 발견한 정보만 보여 주며, 뒤로가면 기존 일시정지 메뉴로 복귀한다.
    public class PauseSectionPanel : MonoBehaviour
    {
        GameObject _panel;
        Text _title;
        Text _body;
        Button _backButton;
        readonly List<GameObject> _settingButtons = new List<GameObject>();

        public bool IsVisible => _panel != null && _panel.activeSelf;
        public PauseSection CurrentSection { get; private set; }

        void Awake() => Build();
        void OnEnable() => InputPrompts.DeviceChanged += HandleDeviceChanged;
        void OnDisable() => InputPrompts.DeviceChanged -= HandleDeviceChanged;

        void HandleDeviceChanged(InputDeviceKind _)
        {
            if (IsVisible && CurrentSection == PauseSection.Controls) Rebuild();
        }

        public void Show(PauseSection section)
        {
            CurrentSection = section;
            _panel.SetActive(true);
            Rebuild();
            UIBuilder.Select(_backButton);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        void Rebuild()
        {
            foreach (var go in _settingButtons) Destroy(go);
            _settingButtons.Clear();

            switch (CurrentSection)
            {
                case PauseSection.Map: BuildMap(); break;
                case PauseSection.Journal: BuildJournal(); break;
                case PauseSection.Controls: BuildControls(); break;
                case PauseSection.Settings: BuildSettings(); break;
            }
        }

        void BuildMap()
        {
            _title.text = "지도";
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress == null || progress.VisitedRooms.Count == 0)
            {
                _body.text = "아직 기억에 남은 방이 없습니다.\n공간을 지나면 이곳에 흔적이 이어집니다.";
                return;
            }

            var sb = new StringBuilder("발견한 방\n\n");
            string currentRoom = HiddenWeight.World.RoomCamera.Instance != null
                && HiddenWeight.World.RoomCamera.Instance.CurrentRoom != null
                ? HiddenWeight.World.RoomCamera.Instance.CurrentRoom.gameObject.name : string.Empty;
            int index = 0;
            foreach (var room in progress.VisitedRooms)
            {
                bool current = !string.IsNullOrEmpty(currentRoom) && room.EndsWith("/" + currentRoom);
                sb.Append(index++ == 0 ? string.Empty : "│\n")
                    .Append(current ? "◉  " : "●  ").AppendLine(room);
            }
            sb.Append("\n열린 지름길  ").Append(progress.OpenedShortcutCount)
                .Append("\n최근 체크포인트  ")
                .Append(progress.LastCheckpoint == Vector3.zero ? "기록 없음" : progress.LastCheckpoint.ToString("F1"));
            _body.text = sb.ToString();
        }

        void BuildJournal()
        {
            _title.text = "기억 기록";
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress == null || progress.FragmentTexts.Count == 0)
            {
                _body.text = "아직 모은 기억 파편이 없습니다.";
                return;
            }

            var sb = new StringBuilder();
            foreach (var entry in progress.FragmentTexts)
            {
                sb.Append("◇ ").Append(entry.Key).AppendLine();
                sb.AppendLine(string.IsNullOrWhiteSpace(entry.Value) ? "기억의 형체만 남아 있습니다." : entry.Value);
                sb.AppendLine();
            }
            _body.text = sb.ToString();
        }

        void BuildControls()
        {
            _title.text = "조작법";
            _body.text = InputPrompts.ControlsSummary() + "\n\n입력 장치가 바뀌면 안내도 즉시 바뀝니다.\n키보드 항목 버튼을 누르면 다음 키로 변경됩니다.";
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
            int index = _settingButtons.Count;
            var button = UIBuilder.CreateButton(_panel.transform, label + "  [" + InputPrompts.Get(action) + "]",
                150f - index * 58f, () => { InputPrompts.CycleKeyboardBinding(action); Rebuild(); });
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.72f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 48f);
            _settingButtons.Add(button.gameObject);
        }

        void BuildSettings()
        {
            _title.text = "설정 · 접근성";
            _body.text = "버튼을 누르면 즉시 적용되며 다음 실행에도 유지됩니다.";
            AddSetting("전체 음량", () => Cycle(UISettings.MasterVolume, v => UISettings.MasterVolume = v));
            AddSetting("배경음", () => Cycle(UISettings.BgmVolume, v => UISettings.BgmVolume = v));
            AddSetting("효과음", () => Cycle(UISettings.SfxVolume, v => UISettings.SfxVolume = v));
            AddSetting("UI 크기", () =>
            {
                float next = UISettings.UiScale >= 1.29f ? 0.8f : UISettings.UiScale + 0.1f;
                UISettings.UiScale = next;
                Rebuild();
            });
            AddSetting("메시지 시간", () =>
            {
                UISettings.MessageDuration = UISettings.MessageDuration >= 1.99f ? 0.8f : UISettings.MessageDuration + 0.2f;
                Rebuild();
            });
            AddSetting("동작 줄이기", () => { UISettings.ReduceMotion = !UISettings.ReduceMotion; Rebuild(); });
            AddSetting("섬광 줄이기", () => { UISettings.ReduceFlash = !UISettings.ReduceFlash; Rebuild(); });
            AddSetting("고대비", () => { UISettings.HighContrast = !UISettings.HighContrast; Rebuild(); });
        }

        void Cycle(float value, System.Action<float> setter)
        {
            setter(value >= 0.99f ? 0f : value + 0.25f);
            Rebuild();
        }

        void AddSetting(string label, UnityEngine.Events.UnityAction action)
        {
            int index = _settingButtons.Count;
            string value = SettingValue(label);
            var button = UIBuilder.CreateButton(_panel.transform, label + "  " + value, 135f - index * 58f, action);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.72f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 48f);
            _settingButtons.Add(button.gameObject);
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

        void Build()
        {
            _panel = new GameObject("PauseSection", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(0.12f, 0.1f);
            rt.anchorMax = new Vector2(0.88f, 0.9f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.97f);

            _title = UIBuilder.CreateText(_panel.transform, "SectionTitle", 34, TextAnchor.UpperLeft);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.85f); titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(48f, 0f); titleRt.offsetMax = new Vector2(-48f, -22f);

            _body = UIBuilder.CreateText(_panel.transform, "SectionBody", 23, TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            var bodyRt = _body.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0.13f); bodyRt.anchorMax = new Vector2(0.58f, 0.84f);
            bodyRt.offsetMin = new Vector2(48f, 0f); bodyRt.offsetMax = new Vector2(-20f, 0f);

            _backButton = UIBuilder.CreateButton(_panel.transform, "뒤로", -260f, Hide);
            var backRt = (RectTransform)_backButton.transform;
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.SetActive(false);
        }
    }
}
