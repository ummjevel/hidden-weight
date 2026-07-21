namespace RookieToCEO.Core
{
    // GDD 13번(CEO 최종 웨이브)의 3단계 시간대: 0~20초 업무 폭탄, 20~40초 전면 수정,
    // 40~60초 퇴근 취소. 스폰 자체는 M5의 WaveSpawnTable(floor=4)이 이미 이 경계를 알고 있으므로
    // 여기서는 "지금이 어느 단계인가"만 추적해 BossWaveManager가 단계별 연출(상사의 시선 강제
    // 발동, 빨간 구역 생성 등)을 트리거하는 기준으로 쓴다.
    public enum BossWavePhase
    {
        WorkBombardment, // 0~20초: 업무 폭탄
        FullRevision,    // 20~40초: 전면 수정
        CommuteCancelled, // 40~60초: 퇴근 취소 (CEO 최종 지시서 등장)
    }

    public class BossWaveState
    {
        public const float WaveDurationSeconds = 60f;
        public const float RevisionPhaseStartSeconds = 20f;
        public const float CommuteCancelledPhaseStartSeconds = 40f;

        public float ElapsedSeconds { get; private set; }
        public BossWavePhase CurrentPhase { get; private set; } = BossWavePhase.WorkBombardment;
        public bool IsTimeUp => ElapsedSeconds >= WaveDurationSeconds;

        public void Tick(float deltaTime)
        {
            if (IsTimeUp) return;

            ElapsedSeconds += deltaTime;

            if (ElapsedSeconds >= CommuteCancelledPhaseStartSeconds)
            {
                CurrentPhase = BossWavePhase.CommuteCancelled;
            }
            else if (ElapsedSeconds >= RevisionPhaseStartSeconds)
            {
                CurrentPhase = BossWavePhase.FullRevision;
            }
            else
            {
                CurrentPhase = BossWavePhase.WorkBombardment;
            }
        }
    }
}
