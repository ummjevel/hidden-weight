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

        // --- 응시 지역 (GAZE_LEVEL_DESIGN.md 6.1절) ---
        [Header("응시")]
        public float hushedDetectMultiplier = 0.35f; // 숨죽인 플레이어에 대한 감지 반경 배율
        public float alertSeconds = 4f;              // 비명이 시선 장치를 켜 두는 시간

        // --- 균열 지역 (FRACTURE_LEVEL_DESIGN.md 6.1절) ---
        // 예지가 "정확히 2초 뒤"를 보여줘야 하므로(7.2절 공정성 규칙) 균열의 적은 위치를
        // 시간의 함수로 움직인다. 그 궤도를 정하는 값들이다.
        [Header("균열")]
        public float patrolWidth = 4f;      // 결정론적 왕복 폭
        public float patrolPeriod = 4f;     // 왕복 1회 주기
        public float feintSeconds = 0.5f;   // 가짜 방향 전환을 유지하는 시간
        public float leadSeconds = 2f;      // 미래 표시 선행 시간(예지와 같은 값)
        public float blastFuse = 2f;        // 지연 폭발 도화선
        public float blastRadius = 1.6f;
    }
}
