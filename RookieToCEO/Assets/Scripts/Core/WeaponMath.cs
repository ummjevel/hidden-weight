using UnityEngine;

namespace RookieToCEO.Core
{
    // 무기 데미지/공격 간격/사거리 계산식을 한 곳에 모아둔다.
    // 스탯 배율(StatSystem)과 무기 기본값을 곱/나누는 단순한 계산이지만,
    // 여러 무기가 같은 공식을 쓰기 때문에 중복을 피하려고 분리했다.
    public static class WeaponMath
    {
        public static int EffectiveDamage(int baseDamage, float damageMultiplier)
        {
            return Mathf.RoundToInt(baseDamage * damageMultiplier);
        }

        // 공격속도 배율이 높을수록 공격 "간격"은 짧아져야 하므로 나눗셈으로 계산한다.
        public static float EffectiveAttackInterval(float baseInterval, float attackSpeedMultiplier)
        {
            return baseInterval / Mathf.Max(0.01f, attackSpeedMultiplier);
        }

        public static float EffectiveRange(float baseRange, float rangeMultiplier)
        {
            return baseRange * rangeMultiplier;
        }
    }
}
