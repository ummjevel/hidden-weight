using System;

namespace RookieToCEO.Core
{
    // GDD 4번(낮 스탯 성장): 업무 처리 -> 경험치 서류 드롭 -> 획득 -> 경험치가 차면 레벨업 -> 3택1.
    // 레벨업 곡선은 "층당 2~3회, 전체 8~10회" 기준(GDD 4번)에 맞춘 임시값이며,
    // 정확한 수치는 M9 플레이테스트에서 확정한다(docs/DEVELOPMENT_PLAN.md).
    public class LevelSystem
    {
        private const float BaseXpToNextLevel = 50f;
        private const float XpGrowthPerLevel = 20f;

        public int Level { get; private set; } = 1;
        public float CurrentXp { get; private set; }
        public float XpToNextLevel { get; private set; } = BaseXpToNextLevel;

        public event Action OnLevelUp;

        // 레벨업이 그 호출 시점에 발생했으면 true를 반환한다.
        // 호출한 쪽(DayWaveManager)은 이 값으로 "3택1 UI를 띄우고 시간을 멈출지" 결정한다.
        public bool AddXp(float amount)
        {
            if (amount <= 0f) return false;

            CurrentXp += amount;
            if (CurrentXp < XpToNextLevel) return false;

            CurrentXp -= XpToNextLevel;
            Level++;
            XpToNextLevel = BaseXpToNextLevel + (Level - 1) * XpGrowthPerLevel;
            OnLevelUp?.Invoke();
            return true;
        }

        // GDD 7번: 평판을 모두 잃고 1층으로 회귀하면 레벨/경험치도 처음부터 다시 시작한다.
        public void ResetAll()
        {
            Level = 1;
            CurrentXp = 0f;
            XpToNextLevel = BaseXpToNextLevel;
        }
    }
}
