using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 6번 "서류 더미": 탱커, 느리지만 높은 HP. 이동 방식 자체는 이메일 봉투와 같은 직선 추적이고
    // HP/속도만 다르다(GDD에 별도 이동 패턴 언급 없음).
    public class DocumentStackEnemy : EnemyBase
    {
        protected override void Awake()
        {
            // 정확한 수치는 M9 밸런싱에서 확정(docs/DEVELOPMENT_PLAN.md). 임시로 기본보다 HP는 높고 속도는 낮게.
            maxHp = 40;
            moveSpeed = 0.8f;
            base.Awake();
        }

        protected override Vector2 GetMoveDirection()
        {
            if (PlayerTransform == null) return Vector2.zero;

            var toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
            return toPlayer.sqrMagnitude > 0f ? toPlayer.normalized : Vector2.zero;
        }
    }
}
