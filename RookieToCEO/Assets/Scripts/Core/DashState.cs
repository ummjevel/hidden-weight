namespace RookieToCEO.Core
{
    // GDD 6번 "긴급 수정 포스트잇" (돌진형): 잠시 멈춘 후 빠르게 돌진하고, 다시 멈추는 순환.
    public enum DashPhase
    {
        Windup,   // 잠시 멈춤(돌진 준비)
        Dashing,  // 빠르게 돌진
        Cooldown, // 돌진 후 재정비
    }

    // MonoBehaviour와 분리한 순수 상태머신. Tick(deltaTime)으로 직접 시간을 흘려보내
    // EditMode 테스트에서 Windup -> Dashing -> Cooldown -> Windup 순환을 검증할 수 있다.
    public class DashState
    {
        private readonly float _windupSeconds;
        private readonly float _dashSeconds;
        private readonly float _cooldownSeconds;

        private float _timer;

        public DashPhase Phase { get; private set; }

        public DashState(float windupSeconds, float dashSeconds, float cooldownSeconds)
        {
            _windupSeconds = windupSeconds;
            _dashSeconds = dashSeconds;
            _cooldownSeconds = cooldownSeconds;
            Phase = DashPhase.Windup;
            _timer = windupSeconds;
        }

        public void Tick(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;

            switch (Phase)
            {
                case DashPhase.Windup:
                    Phase = DashPhase.Dashing;
                    _timer = _dashSeconds;
                    break;
                case DashPhase.Dashing:
                    Phase = DashPhase.Cooldown;
                    _timer = _cooldownSeconds;
                    break;
                case DashPhase.Cooldown:
                    Phase = DashPhase.Windup;
                    _timer = _windupSeconds;
                    break;
            }
        }

        // 돌진 중일 때만 빠른 배율을, 그 외에는 느린 배율을 돌려준다.
        public float CurrentSpeedMultiplier(float idleMultiplier, float dashMultiplier)
        {
            return Phase == DashPhase.Dashing ? dashMultiplier : idleMultiplier;
        }
    }
}
