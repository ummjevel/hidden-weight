using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번 "이메일 봉투": 기본 적, 플레이어에게 직선 이동.
    public class EmailEnvelopeEnemy : EnemyBase
    {
        protected override void ApplyBalanceOverride()
        {
            if (balanceData == null) return;

            maxHp = balanceData.emailEnvelope.maxHp;
            moveSpeed = balanceData.emailEnvelope.moveSpeed;
            contactDamage = balanceData.emailEnvelope.contactDamage;
        }

        protected override Vector2 GetMoveDirection()
        {
            if (PlayerTransform == null) return Vector2.zero;

            var toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
            return toPlayer.sqrMagnitude > 0f ? toPlayer.normalized : Vector2.zero;
        }
    }
}
