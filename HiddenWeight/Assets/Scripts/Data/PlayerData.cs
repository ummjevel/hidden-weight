using UnityEngine;

namespace HiddenWeight.Data
{
    // 플레이어 이동/전투 밸런스 수치. 코드에 하드코딩하지 않고
    // 전부 이 ScriptableObject의 직렬화된 필드로 관리한다.
    [CreateAssetMenu(fileName = "PlayerData", menuName = "HiddenWeight/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [Header("이동")]
        public float walkSpeed = 6f; // 걷기
        public float runSpeed = 9f; // Shift 홀드
        public float jumpVelocity = 14f; // 점프 초기 속도
        public float gravityScale = 3.5f; // Rigidbody2D 기본 중력
        public float fallGravityMultiplier = 1.6f; // 하강 중 중력 배수
        public float coyoteTime = 0.1f; // 발판 이탈 후 점프 허용 시간
        public float jumpBufferTime = 0.1f; // 착지 전 입력 기억 시간
        public float variableJumpCut = 0.5f; // 상승 중 키를 떼면 곱할 속도 배수

        [Header("대시")]
        public float dashDistance = 4f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.8f;

        [Header("벽")]
        public float wallSlideSpeed = 2f; // 벽잡기 하강 속도
        public Vector2 wallJumpVelocity = new Vector2(9f, 13f); // 벽 반대 방향 x, 위쪽 y
        public float wallJumpLockTime = 0.15f; // 벽점프 직후 좌우 입력 무시 시간

        [Header("생존")]
        public int maxHealth = 3;
        public float invulnerableTime = 0.8f;
        public float blinkInterval = 0.1f; // 무적 시간 중 스프라이트 점멸 간격
        public float knockbackForce = 8f; // 피격 넉백

        [Header("공격")]
        public float attackRadius = 1.2f;
        public float attackAngle = 90f; // 부채꼴 각도(도)
        public float attackActiveTime = 0.1f; // 히트박스 유지 시간
        public float attackCooldown = 0.35f;
        public int attackDamage = 1;
    }
}
