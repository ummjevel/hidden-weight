using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenWeight.Player
{
    public enum InputDeviceKind { KeyboardMouse, Gamepad }
    public enum InputActionId { Move, Jump, Dash, Attack, Interact, Skill, Awareness, Map, Pause }

    // 게임 로직과 표기 문자열 사이의 단일 경계. 향후 Input System 액션 에셋으로 교체할 때
    // UI와 튜토리얼은 이 API를 유지하고 내부 바인딩 공급자만 바꾸면 된다.
    public static class InputPrompts
    {
        static readonly Dictionary<InputActionId, KeyCode> Keyboard = new Dictionary<InputActionId, KeyCode>
        {
            { InputActionId.Jump, LoadKey(InputActionId.Jump, KeyCode.Space) },
            { InputActionId.Dash, LoadKey(InputActionId.Dash, KeyCode.LeftControl) },
            { InputActionId.Attack, LoadKey(InputActionId.Attack, KeyCode.J) },
            { InputActionId.Interact, LoadKey(InputActionId.Interact, KeyCode.E) },
            { InputActionId.Skill, LoadKey(InputActionId.Skill, KeyCode.K) },
            { InputActionId.Awareness, LoadKey(InputActionId.Awareness, KeyCode.L) },
            { InputActionId.Map, LoadKey(InputActionId.Map, KeyCode.M) },
            { InputActionId.Pause, LoadKey(InputActionId.Pause, KeyCode.Escape) }
        };

        // 문자 키를 대신할 수 있는 보조 키.
        //
        // macOS에서 한글 입력기가 켜져 있으면 문자 키 이벤트가 입력기에 먹혀 게임까지
        // 오지 않는다 — 한국어권 플레이어가 게임을 켜면 공격도 스킬도 지도도 안 되고,
        // 방향키와 Space만 듣는 상태가 된다. 원인이 게임 밖에 있어 바인딩으로는 못 고치지만,
        // 문자가 아닌 키를 하나씩 더 받아 두면 그 상태에서도 끝까지 플레이할 수 있다.
        //
        // 기존 바인딩은 그대로 살아 있다. 이건 빼는 변경이 아니라 더하는 변경이다.
        static readonly Dictionary<InputActionId, KeyCode> KeyboardAlternate =
            new Dictionary<InputActionId, KeyCode>
        {
            { InputActionId.Attack, KeyCode.Alpha1 },
            { InputActionId.Skill, KeyCode.Alpha2 },
            { InputActionId.Awareness, KeyCode.Alpha3 },
            { InputActionId.Interact, KeyCode.Return },
            { InputActionId.Map, KeyCode.Tab },
        };

        // 보조 키가 없으면 KeyCode.None을 돌려준다 — Input.GetKey(None)은 항상 false라
        // 호출부가 조건 없이 OR로 이어 붙여도 안전하다.
        public static KeyCode GetAlternateKey(InputActionId action)
            => KeyboardAlternate.TryGetValue(action, out var key) ? key : KeyCode.None;

        static readonly KeyCode[] RebindCandidates =
        {
            KeyCode.Space, KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.J, KeyCode.K,
            KeyCode.L, KeyCode.M, KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.Escape
        };

        static readonly Dictionary<InputActionId, string> Gamepad = new Dictionary<InputActionId, string>
        {
            { InputActionId.Move, "L Stick" }, { InputActionId.Jump, "A" },
            { InputActionId.Dash, "LB" }, { InputActionId.Attack, "X" },
            { InputActionId.Interact, "B" },
            { InputActionId.Skill, "Y" }, { InputActionId.Awareness, "RB" },
            { InputActionId.Map, "View" }, { InputActionId.Pause, "Menu" }
        };

        public static InputDeviceKind CurrentDevice { get; private set; } = InputDeviceKind.KeyboardMouse;
        public static event Action<InputDeviceKind> DeviceChanged;

        public static void PollDevice()
        {
            bool gamepad = false;
            var names = Input.GetJoystickNames();
            for (int i = 0; i < names.Length; i++)
                if (!string.IsNullOrWhiteSpace(names[i])) { gamepad = true; break; }

            // 키보드 입력이 발생하면 연결된 패드가 있어도 즉시 키보드 표기로 되돌린다.
            var next = Input.anyKeyDown && !AnyJoystickButtonDown()
                ? InputDeviceKind.KeyboardMouse
                : gamepad && AnyJoystickActivity() ? InputDeviceKind.Gamepad : CurrentDevice;
            if (next == CurrentDevice) return;
            CurrentDevice = next;
            DeviceChanged?.Invoke(next);
        }

        static bool AnyJoystickButtonDown()
        {
            for (KeyCode key = KeyCode.JoystickButton0; key <= KeyCode.JoystickButton19; key++)
                if (Input.GetKeyDown(key)) return true;
            return false;
        }

        static bool AnyJoystickActivity()
            => AnyJoystickButtonDown() || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.25f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.25f;

        public static string Get(InputActionId action)
        {
            if (CurrentDevice == InputDeviceKind.Gamepad) return Gamepad[action];
            if (action == InputActionId.Move) return "A / D";
            return FriendlyKey(Keyboard[action]);
        }

        public static KeyCode GetKeyboardKey(InputActionId action)
            => action == InputActionId.Move ? KeyCode.None : Keyboard[action];

        // 다음 후보 키로 순환한다. 이미 사용 중인 키면 서로 맞바꿔 필수 액션이 비지 않고
        // 중복 바인딩도 생기지 않는다.
        public static void CycleKeyboardBinding(InputActionId action)
        {
            if (action == InputActionId.Move) return;
            var oldKey = Keyboard[action];
            int index = Array.IndexOf(RebindCandidates, oldKey);
            var next = RebindCandidates[(index + 1 + RebindCandidates.Length) % RebindCandidates.Length];

            InputActionId? conflict = null;
            foreach (var pair in Keyboard)
                if (pair.Key != action && pair.Value == next) { conflict = pair.Key; break; }

            Keyboard[action] = next;
            SaveKey(action, next);
            if (conflict.HasValue)
            {
                Keyboard[conflict.Value] = oldKey;
                SaveKey(conflict.Value, oldKey);
            }
        }

        static KeyCode LoadKey(InputActionId action, KeyCode fallback)
            => (KeyCode)PlayerPrefs.GetInt("hw.input." + action, (int)fallback);

        static void SaveKey(InputActionId action, KeyCode key)
        {
            PlayerPrefs.SetInt("hw.input." + action, (int)key);
            PlayerPrefs.Save();
        }

        static string FriendlyKey(KeyCode key)
        {
            // KeyCode 이름을 그대로 내보내면 화면에 "Alpha1", "Return"이 찍힌다. 플레이어의
            // 키보드에는 그런 각인이 없다 — 실제로 조작법 화면에 그렇게 나와 있었다.
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return ((int)(key - KeyCode.Alpha0)).ToString();
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return "숫자패드 " + (int)(key - KeyCode.Keypad0);

            switch (key)
            {
                case KeyCode.LeftControl: return "Ctrl";
                case KeyCode.RightControl: return "Ctrl(오른쪽)";
                case KeyCode.LeftShift: return "Shift";
                case KeyCode.RightShift: return "Shift(오른쪽)";
                case KeyCode.LeftAlt: return "Alt";
                case KeyCode.RightAlt: return "Alt(오른쪽)";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Return: return "Enter";
                case KeyCode.KeypadEnter: return "숫자패드 Enter";
                case KeyCode.Space: return "Space";
                case KeyCode.BackQuote: return "`";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                default: return key.ToString();
            }
        }

        public static string Format(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            foreach (InputActionId action in Enum.GetValues(typeof(InputActionId)))
                message = message.Replace("{" + action + "}", "[" + Get(action) + "]");
            // 기존 씬 직렬화 문자열도 씬 파일을 일괄 재저장하지 않고 동적 표기로 승격한다.
            message = message.Replace("K 홀드", "[" + Get(InputActionId.Skill) + "] 홀드")
                .Replace("K 탭", "[" + Get(InputActionId.Skill) + "] 탭")
                .Replace("L 홀드", "[" + Get(InputActionId.Awareness) + "] 홀드")
                .Replace("Space", "[" + Get(InputActionId.Jump) + "]")
                .Replace("LeftCtrl", "[" + Get(InputActionId.Dash) + "]")
                .Replace("← →  또는  A / D", "[" + Get(InputActionId.Move) + "]");
            return message;
        }

        // 조작법 화면 위에 붙는 안내. 바꿀 수 있는 키는 아래 목록이 한 줄씩 보여 주므로
        // 여기서는 **바꿀 수 없는 것만** 적는다. 예전에는 전체 목록을 여기서 한 번 찍고
        // 아래에서 버튼으로 또 찍어, 같은 내용이 한 화면에 두 번 나와 있었다.
        public static string ControlsSummary()
            => $"이동  {Get(InputActionId.Move)}    ·    상호작용  {Get(InputActionId.Interact)}{Alt(InputActionId.Interact)}"
             + "\n\n아래 항목을 누르면 다음 키로 바뀝니다. 입력 장치를 바꾸면 표기도 함께 바뀝니다."
             + "\n한글 입력 상태에서는 문자 키가 게임에 닿지 않습니다. 그때는 괄호 안의 키를 쓰세요.";

        // 보조 키만 따로. 조작법 목록의 값 칸에 붙인다.
        public static string AlternateSuffix(InputActionId action) => Alt(action);

        // 보조 키 표기. 게임패드를 쓰는 중이면 굳이 보여 주지 않는다.
        static string Alt(InputActionId action)
        {
            var key = GetAlternateKey(action);
            return key == KeyCode.None || CurrentDevice == InputDeviceKind.Gamepad
                ? string.Empty : $"  ({FriendlyKey(key)})";
        }
    }
}
