using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번 "긴급 수정 포스트잇": 돌진형, 잠시 멈춘 후 빠르게 돌진(DashState 참고).
    public class PostItRushEnemy : EnemyBase
    {
        [SerializeField] private float windupSeconds = 1f;
        [SerializeField] private float dashSeconds = 0.4f;
        [SerializeField] private float cooldownSeconds = 1.2f;
        [SerializeField] private float idleSpeedMultiplier = 0.3f;
        [SerializeField] private float dashSpeedMultiplier = 3f;

        private DashState _dash;

        protected override void Awake()
        {
            moveSpeed = 1f;
            base.Awake();
            _dash = new DashState(windupSeconds, dashSeconds, cooldownSeconds);
        }

        protected override void Update()
        {
            base.Update();
            _dash.Tick(Time.deltaTime);
        }

        protected override Vector2 GetMoveDirection()
        {
            if (PlayerTransform == null) return Vector2.zero;

            var toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
            var direction = toPlayer.sqrMagnitude > 0f ? toPlayer.normalized : Vector2.zero;
            var speedMultiplier = _dash.CurrentSpeedMultiplier(idleSpeedMultiplier, dashSpeedMultiplier);

            return direction * speedMultiplier;
        }
    }
}
