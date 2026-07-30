using UnityEngine;

namespace HiddenWeight.Player
{
    // 키 입력을 이 파일에만 모아둔다. 다른 스크립트는 Input 클래스를 직접 건드리지 않고
    // 이 static 클래스를 통해서만 입력을 읽는다.
    // Enabled가 false면 PausePressed·AwarenessHeld를 제외한 모든 값이 0/false로 고정된다
    // (일시정지 화면에서도 Escape로 해제할 수 있어야 하고, Ending 시퀀스에서도 이동/공격은
    // 막되 자각(L) 입력만은 받아야 하므로).
    public static class PlayerInput
    {
        public static bool Enabled { get; set; } = true;

        // 자동 플레이테스트가 키보드 대신 입력을 흘려넣기 위한 훅. 배치모드 테스트에는
        // 키보드가 없어서, 이것 없이는 이동·점프·대시를 실제로 돌려볼 방법이 없다.
        // null이면(=평소 플레이) 아래 값들은 전부 진짜 키 입력을 그대로 읽는다.
        // 검증: Assets/Tests/PlayMode/PlaythroughTests.cs
        public static Frame? Injected;

        public struct Frame
        {
            public float horizontal;
            public bool run;
            public bool jumpPressed;
            public bool jumpHeld;
            public bool dashPressed;
            public bool attackPressed;
            public bool interactPressed;
            public bool skillPressed;
            public bool skillHeld;
            public bool awarenessHeld;
            public bool pausePressed;
            public bool mapPressed;
        }

        // --- GetKeyDown 유실 방지 ---
        // 점프·대시는 PlayerController.FixedUpdate가 읽는다. 그런데 Input.GetKeyDown은 키를 누른
        // "그 Update 프레임"에만 참이고, FixedUpdate는 50Hz라 안 도는 프레임이 있다. 그 프레임에
        // 누르면 입력이 통째로 사라진다 — 점프가 가끔 씹히고 눌린 것도 한 박자 늦게 나가는 원인이다.
        //
        // 그래서 Update에서 누른 시각을 적어 두고(Pump), 짧은 창(BufferSeconds) 동안 "눌림"으로
        // 취급한다. 그 창은 물리 한 스텝(0.02초)보다 넉넉해서 FixedUpdate가 반드시 한 번은 본다.
        // 실제 점프 성립 여부는 PlayerController가 자기 jumpBufferTime으로 따로 판단하므로
        // 이 창 때문에 두 번 뛰지는 않는다.
        const float BufferSeconds = 0.05f;

        static float _jumpDownTime = float.NegativeInfinity;
        static float _dashDownTime = float.NegativeInfinity;

        // 플레이어에 붙은 PlayerInputPump가 매 Update에 호출한다.
        public static void Pump()
        {
            if (Injected.HasValue) return; // 테스트가 주입 중이면 실제 키를 섞지 않는다

            InputPrompts.PollDevice();

            if (Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Jump)) || Input.GetKeyDown(KeyCode.JoystickButton0))
                _jumpDownTime = Time.unscaledTime;
            if (Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Dash)) || Input.GetKeyDown(KeyCode.JoystickButton4))
                _dashDownTime = Time.unscaledTime;
        }

        static bool Buffered(float downTime) => Time.unscaledTime - downTime <= BufferSeconds;

        public static float Horizontal
            => !Enabled ? 0f : Injected.HasValue ? Injected.Value.horizontal : Input.GetAxisRaw("Horizontal");

        public static bool RunHeld
            => Enabled && (Injected.HasValue ? Injected.Value.run : Input.GetKey(KeyCode.LeftShift));

        public static bool JumpPressed
            => Enabled && (Injected.HasValue ? Injected.Value.jumpPressed : Buffered(_jumpDownTime));

        public static bool JumpHeld
            => Enabled && (Injected.HasValue ? Injected.Value.jumpHeld
                : Input.GetKey(InputPrompts.GetKeyboardKey(InputActionId.Jump)) || Input.GetKey(KeyCode.JoystickButton0));

        public static bool DashPressed
            => Enabled && (Injected.HasValue ? Injected.Value.dashPressed : Buffered(_dashDownTime));

        public static bool AttackPressed
            => Enabled && (Injected.HasValue ? Injected.Value.attackPressed
                : Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Attack)) || Input.GetKeyDown(KeyCode.JoystickButton2));

        public static bool InteractPressed
            => Enabled && (Injected.HasValue ? Injected.Value.interactPressed
                : Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Interact)) || Input.GetKeyDown(KeyCode.JoystickButton1));

        public static bool SkillPressed
            => Enabled && (Injected.HasValue ? Injected.Value.skillPressed
                : Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Skill)) || Input.GetKeyDown(KeyCode.JoystickButton3));

        public static bool SkillHeld
            => Enabled && (Injected.HasValue ? Injected.Value.skillHeld
                : Input.GetKey(InputPrompts.GetKeyboardKey(InputActionId.Skill)) || Input.GetKey(KeyCode.JoystickButton3));

        // 일시정지와 마찬가지로 Enabled 여부와 무관하게 항상 동작한다 — Ending 시퀀스는
        // PlayerInput.Enabled = false로 이동/공격을 막아둔 채 이 값만 직접 읽는다.
        public static bool AwarenessHeld
            => Injected.HasValue ? Injected.Value.awarenessHeld
                : Input.GetKey(InputPrompts.GetKeyboardKey(InputActionId.Awareness)) || Input.GetKey(KeyCode.JoystickButton5);

        // 일시정지 해제용이므로 Enabled 여부와 무관하게 항상 동작한다.
        public static bool PausePressed
            => Injected.HasValue ? Injected.Value.pausePressed
                : Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Pause))
                    || Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.JoystickButton1);

        public static bool MapPressed
            => Injected.HasValue ? Injected.Value.mapPressed
                : Input.GetKeyDown(InputPrompts.GetKeyboardKey(InputActionId.Map)) || Input.GetKeyDown(KeyCode.JoystickButton6);
    }
}
