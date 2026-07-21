using UnityEngine;

namespace RookieToCEO.Core
{
    // GDD 9번(상사의 눈치 시스템). 층마다 30초에 한 번 발동, 발동 2초 전 "상사가 보고 있습니다" 경고,
    // 시선이 화면 한쪽 끝에서 반대쪽 끝으로 천천히 이동하며, 시선에 걸리면 2초간 "일하는 척"
    // (이동은 가능하지만 자동 공격 정지) 상태가 된다.
    // 층별 차이(1층: 느린 이동 / 2층: 넓은 시야 / 3층: 두 번 이동)는 생성자 파라미터로 반영한다.
    public enum BossGlancePhase
    {
        Idle,     // 다음 발동 대기 중
        Warning,  // 발동 2초 전 경고
        Sweeping, // 시선이 이동 중
    }

    public class BossGlanceSystem
    {
        private const float TriggerIntervalSeconds = 30f;
        private const float WarningSeconds = 2f;
        private const float WorkingPretendSeconds = 2f;

        private readonly float _sweepSeconds;  // 한 번 훑는 데 걸리는 시간 (1층은 크게 = 느리게)
        private readonly float _gazeHalfWidth; // 시선 폭의 절반, 0~1 정규화 좌표 기준 (2층은 크게)
        private readonly int _sweepCount;      // 한 번 발동에 몇 번 훑는지 (3층은 2)

        private float _timer;
        private int _sweepsDone;
        private float _workingPretendTimer;

        public BossGlancePhase Phase { get; private set; } = BossGlancePhase.Idle;
        public float SweepProgress01 { get; private set; }
        public bool IsPretendingToWork => _workingPretendTimer > 0f;

        public BossGlanceSystem(float sweepSeconds, float gazeHalfWidth, int sweepCount)
        {
            _sweepSeconds = sweepSeconds;
            _gazeHalfWidth = gazeHalfWidth;
            _sweepCount = Mathf.Max(1, sweepCount);
            _timer = TriggerIntervalSeconds - WarningSeconds; // GDD: 발동 2초 전 경고 -> t=28에 경고 시작
        }

        // GDD 9번 "4층: CEO 웨이브 중 한 번 추가 발생" - 일반 30초 주기와 별개로 즉시 경고 단계를
        // 강제로 시작시킨다. 이미 Idle이 아니면(경고/이동 중이면) 아무 효과가 없다.
        public void ForceTrigger()
        {
            if (Phase != BossGlancePhase.Idle) return;

            Phase = BossGlancePhase.Warning;
            _timer = WarningSeconds;
        }

        // playerNormalizedX: 화면 폭 기준 0(왼쪽 끝)~1(오른쪽 끝)로 정규화한 플레이어 위치.
        // deltaTime이 한 단계의 남은 시간보다 커도(예: 테스트에서 큰 값으로 한 번에 흘려보내는 경우)
        // 단계를 순서대로 다 소비할 때까지 반복해서 정확하게 전이시킨다.
        public void Tick(float deltaTime, float playerNormalizedX)
        {
            if (_workingPretendTimer > 0f)
            {
                _workingPretendTimer -= deltaTime;
            }

            var remaining = deltaTime;
            while (remaining > 0f)
            {
                remaining = AdvancePhase(remaining, playerNormalizedX);
            }
        }

        // 현재 단계에서 remaining 시간을 소비한다. 단계가 끝나 다음 단계로 넘어가면 남은 시간을,
        // 다 소비했으면 0을 반환한다.
        private float AdvancePhase(float remaining, float playerNormalizedX)
        {
            switch (Phase)
            {
                case BossGlancePhase.Idle:
                    if (remaining < _timer)
                    {
                        _timer -= remaining;
                        return 0f;
                    }

                    remaining -= _timer;
                    Phase = BossGlancePhase.Warning;
                    _timer = WarningSeconds;
                    return remaining;

                case BossGlancePhase.Warning:
                    if (remaining < _timer)
                    {
                        _timer -= remaining;
                        return 0f;
                    }

                    remaining -= _timer;
                    Phase = BossGlancePhase.Sweeping;
                    _timer = _sweepSeconds;
                    _sweepsDone = 0;
                    SweepProgress01 = 0f;
                    return remaining;

                case BossGlancePhase.Sweeping:
                    if (remaining < _timer)
                    {
                        _timer -= remaining;
                        SweepProgress01 = 1f - Mathf.Clamp01(_timer / _sweepSeconds);
                        CheckCaught(playerNormalizedX);
                        return 0f;
                    }

                    remaining -= _timer;
                    SweepProgress01 = 1f;
                    CheckCaught(playerNormalizedX);
                    _sweepsDone++;

                    if (_sweepsDone >= _sweepCount)
                    {
                        Phase = BossGlancePhase.Idle;
                        _timer = TriggerIntervalSeconds - WarningSeconds;
                    }
                    else
                    {
                        _timer = _sweepSeconds; // 다음 스윕(3층: 두 번 이동)
                        SweepProgress01 = 0f;
                    }

                    return remaining;

                default:
                    return 0f;
            }
        }

        private void CheckCaught(float playerNormalizedX)
        {
            var distance = Mathf.Abs(playerNormalizedX - SweepProgress01);
            if (distance <= _gazeHalfWidth)
            {
                _workingPretendTimer = WorkingPretendSeconds;
            }
        }
    }
}
