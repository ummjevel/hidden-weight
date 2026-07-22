using System;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 경험치·레벨 관리. 기획서 7.1.
    /// 적 처리로 경험치 획득 → 임계치 도달 시 레벨업 이벤트(시간 정지는 매니저가 처리).
    /// </summary>
    public class ExperienceSystem : MonoBehaviour
    {
        [SerializeField] private PlayerData playerData;

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int ExpToNext { get; private set; }

        public event Action<int, int> ExpChanged; // (current, toNext)
        public event Action<int> LeveledUp;        // 새 레벨

        private void Awake()
        {
            Level = GameManager.Instance != null ? Mathf.Max(1, GameManager.Instance.Run.PlayerLevel) : 1;
            ExpToNext = RequiredFor(Level);
        }

        private int RequiredFor(int level)
        {
            float baseExp = playerData != null ? playerData.baseExpToLevel : 8f;
            float growth = playerData != null ? playerData.expGrowthPerLevel : 1.4f;
            return Mathf.RoundToInt(baseExp * Mathf.Pow(growth, level - 1));
        }

        public void AddExp(int amount)
        {
            Exp += amount;
            while (Exp >= ExpToNext)
            {
                Exp -= ExpToNext;
                Level++;
                ExpToNext = RequiredFor(Level);
                if (GameManager.Instance != null) GameManager.Instance.Run.PlayerLevel = Level;
                LeveledUp?.Invoke(Level);
            }
            ExpChanged?.Invoke(Exp, ExpToNext);
        }
    }
}
