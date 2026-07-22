using UnityEngine;

namespace HanGame.Common
{
    /// <summary>
    /// 강화로 누적된 런타임 스탯 배수를 보관. 기획서 7장.
    /// StatUpgradeSystem이 값을 갱신하고, 무기·이동이 이 값을 읽는다.
    /// 회귀 시 RunState가 리셋되면 씬 재로드로 초기화된다.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        // 배수(1 = 기본). 감소형(쿨타임)은 0~1 배수.
        public float AttackPowerMul { get; private set; } = 1f;
        public float AttackSpeedMul { get; private set; } = 1f;
        public float MoveSpeedMul { get; private set; } = 1f;
        public float AttackRangeMul { get; private set; } = 1f;
        public float ActiveCooldownMul { get; private set; } = 1f;

        public void Reset()
        {
            AttackPowerMul = AttackSpeedMul = MoveSpeedMul = AttackRangeMul = ActiveCooldownMul = 1f;
        }

        public void ApplyAttackPower(float perStack, int stacks) => AttackPowerMul = 1f + perStack * stacks;
        public void ApplyAttackSpeed(float perStack, int stacks) => AttackSpeedMul = 1f + perStack * stacks;
        public void ApplyMoveSpeed(float perStack, int stacks) => MoveSpeedMul = 1f + perStack * stacks;
        public void ApplyAttackRange(float perStack, int stacks) => AttackRangeMul = 1f + perStack * stacks;
        public void ApplyActiveCooldown(float perStack, int stacks) => ActiveCooldownMul = Mathf.Max(0.1f, 1f - perStack * stacks);
    }
}
