using UnityEngine;

namespace HiddenWeight.Data
{
    // 적 1종의 수치를 담는 ScriptableObject.
    [CreateAssetMenu(fileName = "EnemyData", menuName = "HiddenWeight/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public int maxHealth = 2;
        public float moveSpeed = 1.5f;
        public int contactDamage = 1;
        public Color tint = Color.white;
        public float knockbackForce = 6f;
        public float wobbleAmplitude = 0f; // 균열 지역만 0.2
        public float wobbleFrequency = 3f;

        // --- 행동 모듈 공통 수치 (CONTENT_SYSTEM.md 3.3절) ---
        // 기존 순찰형 에셋은 아래 값을 쓰지 않으므로 기본값 그대로 둬도 동작이 바뀌지 않는다.

        [Header("감지·예고")]
        public float detectRange = 8f;      // 이 거리 안에 들어오면 행동 시작
        public float loseRange = 12f;       // 이 거리 밖으로 나가면 추적 중단
        public float telegraphSeconds = 0.8f; // 공격 예고(플레이어가 읽을 시간)
        public float recoverSeconds = 1.0f;   // 공격 후 빈틈

        [Header("돌진형")]
        public float chargeSpeed = 10f;
        public float chargeMaxSeconds = 1.5f;
        public float stunSeconds = 1.5f;    // 벽에 부딪혔을 때 경직

        [Header("매복형")]
        public float dropSpeed = 14f;

        [Header("방어형")]
        public float guardArc = 120f;       // 이 각도 안에서 들어온 피해를 막는다
        public float attackRange = 2f;
    }
}
