using UnityEngine;

namespace HiddenWeight.Player
{
    // 키 입력을 이 파일에만 모아둔다. 다른 스크립트는 Input 클래스를 직접 건드리지 않고
    // 이 static 클래스를 통해서만 입력을 읽는다.
    // Enabled가 false면 PausePressed를 제외한 모든 값이 0/false로 고정된다
    // (일시정지 화면에서도 Escape로 해제할 수 있어야 하므로).
    public static class PlayerInput
    {
        public static bool Enabled { get; set; } = true;

        public static float Horizontal
            => Enabled ? Input.GetAxisRaw("Horizontal") : 0f;

        public static bool RunHeld
            => Enabled && Input.GetKey(KeyCode.LeftShift);

        public static bool JumpPressed
            => Enabled && Input.GetKeyDown(KeyCode.Space);

        public static bool JumpHeld
            => Enabled && Input.GetKey(KeyCode.Space);

        public static bool DashPressed
            => Enabled && Input.GetKeyDown(KeyCode.LeftControl);

        public static bool AttackPressed
            => Enabled && Input.GetKeyDown(KeyCode.J);

        public static bool SkillPressed
            => Enabled && Input.GetKeyDown(KeyCode.K);

        public static bool SkillHeld
            => Enabled && Input.GetKey(KeyCode.K);

        public static bool AwarenessHeld
            => Enabled && Input.GetKey(KeyCode.L);

        // 일시정지 해제용이므로 Enabled 여부와 무관하게 항상 동작한다.
        public static bool PausePressed
            => Input.GetKeyDown(KeyCode.Escape);
    }
}
