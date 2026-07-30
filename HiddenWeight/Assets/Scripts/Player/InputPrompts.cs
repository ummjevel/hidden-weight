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
            switch (key)
            {
                case KeyCode.LeftControl: return "Ctrl";
                case KeyCode.LeftShift: return "Shift";
                case KeyCode.Escape: return "Esc";
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

        public static string ControlsSummary()
            => $"이동  {Get(InputActionId.Move)}\n점프  {Get(InputActionId.Jump)}\n대시  {Get(InputActionId.Dash)}\n공격  {Get(InputActionId.Attack)}\n상호작용  {Get(InputActionId.Interact)}\n감정 스킬  {Get(InputActionId.Skill)}\n자각  {Get(InputActionId.Awareness)}\n지도  {Get(InputActionId.Map)}\n일시정지  {Get(InputActionId.Pause)}";
    }
}
