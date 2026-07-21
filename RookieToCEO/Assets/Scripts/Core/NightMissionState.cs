namespace RookieToCEO.Core
{
    // GDD 10~12번(밤 탐방 구조/발각과 실패/무기 획득 과정)의 진행 상태.
    // 조사(E)와 탈출을 모두 마쳐야 무기를 실제로 보유하게 되고, 조사 없이 탈출하면 보상 없이
    // 성공 처리되며, 발각되거나 60초를 넘기면 실패로 끝난다.
    public enum NightMissionOutcome
    {
        InProgress,
        Success,             // 조사 + 탈출 모두 완료 -> 무기 획득
        SuccessWithoutWeapon, // 조사 없이 탈출 -> 보상 없음, 페널티도 없음
        FailedDetected,      // 경비/CCTV에게 발각
        FailedTimeout,       // 60초 안에 탈출 못함
    }

    public class NightMissionState
    {
        public const float TimeLimitSeconds = 60f; // GDD: 밤 탐방 시간 60초

        public float ElapsedSeconds { get; private set; }
        public bool HasInvestigated { get; private set; }
        public NightMissionOutcome Outcome { get; private set; } = NightMissionOutcome.InProgress;

        public bool IsFinished => Outcome != NightMissionOutcome.InProgress;

        // 발각/타임아웃으로 끝났는지 (GDD 11번 페널티 적용 대상).
        public bool IsFailure => Outcome == NightMissionOutcome.FailedDetected || Outcome == NightMissionOutcome.FailedTimeout;

        public void Tick(float deltaTime)
        {
            if (IsFinished) return;

            ElapsedSeconds += deltaTime;
            if (ElapsedSeconds >= TimeLimitSeconds)
            {
                Outcome = NightMissionOutcome.FailedTimeout;
            }
        }

        public void MarkDetected()
        {
            if (IsFinished) return;
            Outcome = NightMissionOutcome.FailedDetected;
        }

        public void MarkInvestigated()
        {
            if (IsFinished) return;
            HasInvestigated = true;
        }

        // 출구에 도달했을 때 호출한다.
        public void ReachExit()
        {
            if (IsFinished) return;
            Outcome = HasInvestigated ? NightMissionOutcome.Success : NightMissionOutcome.SuccessWithoutWeapon;
        }
    }
}
