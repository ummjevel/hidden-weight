using UnityEngine;

namespace HanGame.Data
{
    /// <summary>
    /// 적 한 종류의 기본 수치. 기획서 9.4.
    /// Assets/Data/Enemies/ 아래에 종류별 에셋 생성.
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("식별")]
        public EnemyType type;
        public EnemyBehavior behavior;
        public string displayName = "이메일 봉투";
        public GameObject prefab;

        [Header("기본 능력치 (1층 100% 기준)")]
        public float maxHp = 20f;
        public float moveSpeed = 2f;
        public float contactDamage = 5f;   // 접촉 피해

        [Header("공격")]
        public float attackInterval = 1f;  // 접촉/원거리 공격 주기
        public float attackRange = 0f;     // 0이면 근접(접촉). 원거리형은 > 0
        public float projectileSpeed = 4f; // 원거리형 발사체 속도

        [Header("돌진형(Dasher)")]
        public float dashTelegraph = 0.6f; // 예고 시간
        public float dashSpeed = 8f;
        public float dashRange = 5f;       // 돌진 시작 거리

        [Header("디버프형(Debuffer)")]
        public float debuffRadius = 2.5f;
        public float attackSpeedDebuff = 0.4f; // 근처 시 플레이어 공격속도 -40%

        [Header("보상")]
        public int expReward = 1; // 경험치 지급량
        [Range(0f, 1f)] public float coffeeDropChance = 0.05f; // 아메리카노 드롭 확률
    }
}
