using RookieToCEO.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RookieToCEO.Gameplay.Skills
{
    // GDD 3번/13번 "3층 밤 보상: 퇴사 통보". R로 사용하는 궁극기 - 게이지가 가득 차야 쓸 수 있다.
    // 일반 업무는 공포(반대 방향 도주), 정예는 슬로우, CEO 최종 지시서(보스)는 3초 정지(GDD 13번).
    public class ResignationUltimate : MonoBehaviour
    {
        private const float MaxGauge = 100f;

        [SerializeField] private float fearDuration = 4f;
        [SerializeField] private float slowDuration = 4f;
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float bossPauseDuration = 3f; // GDD: CEO 웨이브 3초간 정지

        // M9: 배정되면 위 지속시간/배율을 덮어쓴다. bossPauseDuration은 GDD 고정값(3초)에 맞춰
        // BalanceData 쪽 값도 3초로 맞춰뒀다.
        [SerializeField] private BalanceData balanceData;

        public Gauge Gauge { get; } = new Gauge(MaxGauge);

        private void Awake()
        {
            if (balanceData == null) return;

            fearDuration = balanceData.ultimateFearDuration;
            slowDuration = balanceData.ultimateSlowDuration;
            slowMultiplier = balanceData.ultimateSlowMultiplier;
            bossPauseDuration = balanceData.ultimateBossPauseSeconds;
        }

        // 적을 처치하면 호출해 게이지를 충전한다 (M5에서 EnemyBase 사망 시 연결).
        public void AddGaugeOnKill(float amount)
        {
            Gauge.Add(amount);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        private void TryActivate()
        {
            if (!Gauge.TryConsume()) return;

            var registry = EnemyRegistry.Instance;
            registry.ApplyFearToAllNormal(fearDuration);
            registry.ApplySlowToAllElite(slowDuration, slowMultiplier);
            registry.PauseBoss(bossPauseDuration);
        }
    }
}
