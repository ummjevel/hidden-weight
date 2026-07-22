using UnityEngine;

namespace HanGame.Data
{
    /// <summary>플레이어 기본 능력치. 기획서 19.4.</summary>
    [CreateAssetMenu(menuName = "HanGame/Player Data", fileName = "PlayerData")]
    public class PlayerData : ScriptableObject
    {
        [Header("기본")]
        public float maxHp = 100f;
        public float moveSpeed = 5f;
        public float runMultiplier = 1.6f;

        [Header("평판")]
        public int startingReputation = 3;

        [Header("경험치/레벨업")]
        public int baseExpToLevel = 8;      // 1→2 레벨 필요 경험치
        public float expGrowthPerLevel = 1.4f; // 레벨당 필요량 배수

        [Header("커피 회복")]
        public float coffeeHealAmount = 25f;
    }
}
