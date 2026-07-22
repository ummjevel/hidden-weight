using UnityEngine;

namespace RookieToCEO.Core
{
    // M9 밸런싱 확정치를 코드 재컴파일 없이 조정할 수 있게 모아둔 ScriptableObject.
    // docs/DEVELOPMENT_PLAN.md 밸런스 표와 값이 1:1로 대응하며, 그 문서가 "왜 이 값인지"를
    // 설명하는 원본이고 이 애셋은 실제 게임이 참조하는 데이터다.
    // 각 무기/적 MonoBehaviour는 이 애셋이 인스펙터에 연결돼 있으면 Awake에서 그 값으로
    // 자신의 기본값을 덮어쓰고, 연결 안 돼 있으면(예: 유닛 테스트용 임시 오브젝트) 기존
    // [SerializeField] 기본값을 그대로 쓴다.
    [CreateAssetMenu(fileName = "BalanceData", menuName = "RookieToCEO/Balance Data")]
    public class BalanceData : ScriptableObject
    {
        [System.Serializable]
        public struct WeaponStats
        {
            public int baseDamage;
            public float baseAttackInterval;
            public float baseRange;
        }

        [System.Serializable]
        public struct EnemyStats
        {
            public int maxHp;
            public float moveSpeed;
            public int contactDamage;
        }

        [Header("무기")]
        public WeaponStats keyboardShotgun = new WeaponStats { baseDamage = 12, baseAttackInterval = 1.2f, baseRange = 3f };
        public WeaponStats staplerRapidFire = new WeaponStats { baseDamage = 6, baseAttackInterval = 0.3f, baseRange = 6f };

        [Header("업무 떠넘기기 (액티브)")]
        public float workDumpCooldownSeconds = 12f; // GDD 고정값
        public float workDumpRadius = 2.5f;
        public float workDumpKnockbackForce = 8f;

        [Header("퇴사 통보 (궁극기)")]
        public float ultimateGaugePerKill = 10f; // 적 10마리 처치 시 궁극기 충전
        public float ultimateFearDuration = 4f;
        public float ultimateSlowDuration = 4f;
        public float ultimateSlowMultiplier = 0.5f;
        public float ultimateBossPauseSeconds = 3f; // GDD 고정값

        [Header("적")]
        public EnemyStats emailEnvelope = new EnemyStats { maxHp = 20, moveSpeed = 1.5f, contactDamage = 10 };
        public EnemyStats documentStack = new EnemyStats { maxHp = 40, moveSpeed = 0.8f, contactDamage = 10 };
        public EnemyStats postItRush = new EnemyStats { maxHp = 15, moveSpeed = 1f, contactDamage = 12 };
        public EnemyStats meetingCalendar = new EnemyStats { maxHp = 25, moveSpeed = 0.9f, contactDamage = 8 };
        public EnemyStats claimPhone = new EnemyStats { maxHp = 20, moveSpeed = 1f, contactDamage = 8 };

        [Header("스폰")]
        public float spawnRateMultiplierAfter30s = 1.5f; // GDD 5번: 30~45초 생성량 증가

        [Header("커피 (회복 아이템)")]
        public float coffeeDropChance = 0.1f; // GDD 4번: "낮은 확률로" 드롭. 프로토타입은 아메리카노만
        public int coffeeHealAmount = 15;
    }
}
