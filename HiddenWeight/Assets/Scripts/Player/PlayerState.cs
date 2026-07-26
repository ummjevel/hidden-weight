namespace HiddenWeight.Player
{
    // PlayerController의 상태머신 상태. PlayerAnimator가 (int)state를 애니메이터
    // 정수 파라미터로 그대로 넘기므로 순서를 바꾸지 않는다.
    public enum PlayerState
    {
        Idle,
        Walk,
        Run,
        Jump,
        AirMove,
        Fall,
        Land,
        Attack,
        Dash,
        WallCling,
        WallJump
    }
}
