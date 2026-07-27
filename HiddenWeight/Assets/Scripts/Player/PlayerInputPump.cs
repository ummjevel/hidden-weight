using UnityEngine;

namespace HiddenWeight.Player
{
    // PlayerInput.Pump()를 매 Update에 돌려, Update 주기에서만 참인 GetKeyDown을 놓치지 않게 한다.
    // 플레이어와 같은 GameObject에 붙는다. 이 컴포넌트가 없으면 점프·대시가 프레임 타이밍에 따라
    // 씹힌다(PlayerInput.Pump 주석 참고).
    [DefaultExecutionOrder(-500)] // 다른 컴포넌트가 입력을 읽기 전에 갱신한다
    public class PlayerInputPump : MonoBehaviour
    {
        void Update() => PlayerInput.Pump();
    }
}
