using System.Collections.Generic;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 레벨업 시 강화 3종 후보 제시 + 선택 적용. 기획서 7.2/7.3.
    /// 최대 중첩 도달·미보유 무기 조건 강화는 후보에서 제외.
    /// 누적은 RunState.UpgradeStacks에 저장되어 다음 층까지 유지, 회귀 시 초기화.
    /// </summary>
    public class StatUpgradeSystem : MonoBehaviour
    {
        [SerializeField] private List<UpgradeData> allUpgrades = new();
        [SerializeField] private int optionsPerLevel = 3;

        private PlayerStats _stats;
        private PlayerHealth _health;
        private RunState Run => GameManager.Instance != null ? GameManager.Instance.Run : null;

        private void Awake()
        {
            _stats = Player.Local != null ? Player.Local.Stats : FindObjectOfType<PlayerStats>();
            _health = Player.Local != null ? Player.Local.Health : FindObjectOfType<PlayerHealth>();
            ReapplyAll(); // 이전 층에서 넘어온 스탯 복원
        }

        /// <summary>레벨업 UI가 보여줄 후보 3종을 뽑는다.</summary>
        public List<UpgradeData> RollOptions()
        {
            var pool = new List<UpgradeData>();
            foreach (var u in allUpgrades)
            {
                if (u == null) continue;
                if (Run != null && Run.UpgradeLevel(u.id) >= u.maxStacks) continue; // 최대 중첩
                if (!string.IsNullOrEmpty(u.requiresWeaponId) && (Run == null || !Run.HasWeapon(u.requiresWeaponId))) continue;
                pool.Add(u);
            }

            // 무작위 3개(중복 없이).
            var result = new List<UpgradeData>();
            for (int i = 0; i < optionsPerLevel && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }

        /// <summary>선택한 강화 적용.</summary>
        public void Pick(UpgradeData upgrade)
        {
            if (upgrade == null || Run == null) return;
            Run.AddUpgrade(upgrade.id);
            ReapplyAll();

            if (upgrade.stat == UpgradeStat.MaxHp && _health != null)
                _health.IncreaseMaxHp(upgrade.valuePerStack, upgrade.healPercentOnPick);
        }

        /// <summary>RunState 누적치를 PlayerStats에 반영.</summary>
        public void ReapplyAll()
        {
            if (_stats == null || Run == null) return;
            _stats.Reset();
            foreach (var u in allUpgrades)
            {
                if (u == null) continue;
                int stacks = Run.UpgradeLevel(u.id);
                if (stacks <= 0) continue;
                switch (u.stat)
                {
                    case UpgradeStat.AttackPower: _stats.ApplyAttackPower(u.valuePerStack, stacks); break;
                    case UpgradeStat.AttackSpeed: _stats.ApplyAttackSpeed(u.valuePerStack, stacks); break;
                    case UpgradeStat.MoveSpeed: _stats.ApplyMoveSpeed(u.valuePerStack, stacks); break;
                    case UpgradeStat.AttackRange: _stats.ApplyAttackRange(u.valuePerStack, stacks); break;
                    case UpgradeStat.ActiveCooldown: _stats.ApplyActiveCooldown(u.valuePerStack, stacks); break;
                    // MaxHp는 즉시 반영형(Pick에서 처리). 회귀 후 재적용은 최대 HP 재계산 필요 시 확장.
                }
            }
            // 이동속도 배수를 컨트롤러에 반영.
            if (Player.Local != null && Player.Local.Controller != null)
                Player.Local.Controller.SetSpeedBonus(_stats.MoveSpeedMul);
        }
    }
}
