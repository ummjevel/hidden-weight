using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번 "회의 요청 달력": 디버프형, 가까이 오면 플레이어 공격속도 감소.
    public class MeetingCalendarEnemy : EnemyBase
    {
        [SerializeField] private float auraRadius = 2f;
        [SerializeField] private float debuffMultiplier = 0.5f;
        [SerializeField] private float debuffRefreshDuration = 0.5f; // 오라 안에 있는 동안 계속 갱신

        protected override void Awake()
        {
            moveSpeed = 0.9f;
            base.Awake();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            ApplyDebuffIfInRange();
        }

        protected override Vector2 GetMoveDirection()
        {
            if (PlayerTransform == null) return Vector2.zero;

            var toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
            return toPlayer.sqrMagnitude > 0f ? toPlayer.normalized : Vector2.zero;
        }

        private void ApplyDebuffIfInRange()
        {
            if (PlayerTransform == null) return;

            var distanceSqr = ((Vector2)PlayerTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr > auraRadius * auraRadius) return;

            var player = PlayerTransform.GetComponent<PlayerController>();
            player?.ApplyAttackSpeedDebuff(debuffMultiplier, debuffRefreshDuration);
        }
    }
}
