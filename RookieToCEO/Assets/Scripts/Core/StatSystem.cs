using System;
using UnityEngine;

namespace RookieToCEO.Core
{
    // GDD 4번(낮 스탯 성장)의 6종 스탯. 레벨업 3택1을 고를 때마다 해당 스탯의 레벨이 1씩 오른다.
    public enum StatType
    {
        WorkPower,   // 업무처리력: 모든 무기 공격력 증가
        HandSpeed,   // 손속도: 기본 무기 공격속도 증가
        Awareness,   // 눈치: 이동속도 증가
        MentalCare,  // 멘탈 관리: 최대 HP 증가 및 즉시 회복
        WorkSense,   // 일머리: 공격 범위 증가
        Seniority,   // 짬: 액티브 스킬 쿨타임 감소
    }

    // 6종 스탯의 레벨과 배율 계산만 담당하는 순수 C# 클래스.
    // MonoBehaviour에 두지 않고 분리해야 씬을 만들지 않고도 EditMode 테스트로 계산식을 검증할 수 있다.
    [Serializable]
    public class StatSystem
    {
        // 레벨당 증가폭. docs/DEVELOPMENT_PLAN.md 밸런스 표의 TBD 자리에 대응한다.
        // "층당 2~3회, 전체 8~10회 레벨업" 기준(GDD 4번)에 맞춰 체감 가능한 값으로 임시 설정했고,
        // 실제 수치는 M9 플레이테스트에서 확정한다.
        private const float DamagePerLevel = 0.10f;
        private const float AttackSpeedPerLevel = 0.10f;
        private const float MoveSpeedPerLevel = 0.08f;
        private const int MaxHpPerLevel = 20;
        private const float RangePerLevel = 0.10f;
        private const float CooldownReductionPerLevel = 0.08f;

        private static readonly int StatTypeCount = Enum.GetValues(typeof(StatType)).Length;

        private readonly int[] _levels = new int[StatTypeCount];

        public int GetLevel(StatType type) => _levels[(int)type];

        public void LevelUp(StatType type)
        {
            _levels[(int)type]++;
        }

        // GDD 7번: 평판을 모두 잃고 1층으로 회귀하면 낮에 올린 스탯이 전부 초기화된다.
        public void ResetAll()
        {
            Array.Clear(_levels, 0, _levels.Length);
        }

        public float DamageMultiplier => 1f + GetLevel(StatType.WorkPower) * DamagePerLevel;
        public float AttackSpeedMultiplier => 1f + GetLevel(StatType.HandSpeed) * AttackSpeedPerLevel;
        public float MoveSpeedMultiplier => 1f + GetLevel(StatType.Awareness) * MoveSpeedPerLevel;
        public int BonusMaxHp => GetLevel(StatType.MentalCare) * MaxHpPerLevel;
        public float RangeMultiplier => 1f + GetLevel(StatType.WorkSense) * RangePerLevel;

        // 짬 스탯이 아무리 높아도 쿨타임이 0에 수렴하지 않도록 최소 배율(10%)을 보장한다.
        public float CooldownMultiplier => Mathf.Max(0.1f, 1f - GetLevel(StatType.Seniority) * CooldownReductionPerLevel);
    }
}
