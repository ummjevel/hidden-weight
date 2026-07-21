using System.Collections.Generic;

namespace RookieToCEO.Core
{
    // 레벨업 시 "6종 스탯 중 3개를 뽑아 하나 선택"(GDD 4번)할 후보를 만든다.
    // 난수를 System.Random으로 주입받는 형태라, 시드를 고정하면 EditMode에서 결정론적으로
    // 테스트할 수 있다(실제 게임 플레이에서는 UnityEngine.Random 기반 System.Random을 넘기면 된다).
    public static class StatChoiceGenerator
    {
        private static readonly StatType[] AllTypes =
        {
            StatType.WorkPower, StatType.HandSpeed, StatType.Awareness,
            StatType.MentalCare, StatType.WorkSense, StatType.Seniority,
        };

        public static List<StatType> PickThree(System.Random random)
        {
            var pool = new List<StatType>(AllTypes);
            var result = new List<StatType>(3);

            for (var i = 0; i < 3 && pool.Count > 0; i++)
            {
                var index = random.Next(pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }
    }
}
