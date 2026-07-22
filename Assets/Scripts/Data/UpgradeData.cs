using UnityEngine;

namespace HanGame.Data
{
    /// <summary>강화가 영향을 주는 스탯. 기획서 7.2.</summary>
    public enum UpgradeStat
    {
        AttackPower,      // 업무처리력 — 모든 공격력
        AttackSpeed,      // 손속도 — 기본 무기 공격속도
        MoveSpeed,        // 눈치 — 이동속도
        MaxHp,            // 멘탈 관리 — 최대 HP + 현재 회복
        AttackRange,      // 일머리 — 공격 범위
        ActiveCooldown    // 짬 — 업무 떠넘기기 쿨타임 감소
    }

    /// <summary>
    /// 스탯 강화 1종. 기획서 7.2/7.3.
    /// requiresWeaponId가 있으면 해당 무기 획득 전에는 후보에 등장하지 않는다('짬').
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Upgrade Data", fileName = "UpgradeData")]
    public class UpgradeData : ScriptableObject
    {
        [Header("식별")]
        public string id = "attack_power";
        public string displayName = "업무처리력";
        [TextArea] public string description = "모든 공격력 +15%";
        public Sprite icon;

        [Header("효과")]
        public UpgradeStat stat;
        public float valuePerStack = 0.15f; // 1회 효과(비율 또는 값)
        public int maxStacks = 5;           // 최대 중첩

        [Header("멘탈 관리 전용")]
        public float healPercentOnPick = 0.2f; // MaxHp 강화 시 즉시 회복 비율

        [Header("등장 조건")]
        public string requiresWeaponId = ""; // 비어있으면 항상 등장
    }
}
